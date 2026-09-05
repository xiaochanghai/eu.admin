using System.Data;
using System.Text.Json;
using EU.Core.IServices.Tasks;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：AgAgentTaskServices 职责实现

/// <summary>
/// 提供可恢复 Agent 任务的持久化与状态转换服务。
/// </summary>
public sealed class AgAgentTaskServices : BaseServices<AgAgentTask>, IAgAgentTaskServices
{
    private const int MaximumTake = 200;
    private const int MaximumCheckpointLength = 262_144;

    public AgAgentTaskServices(IBaseRepository<AgAgentTask> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<AgentTaskRecord> CreateAsync(CreateAgentTaskCommand command, CancellationToken cancellationToken = default)
    {
        ValidateCreate(command);
        cancellationToken.ThrowIfCancellationRequested();

        string tenantId = command.TenantId.Trim();
        string userId = command.UserId.Trim();
        string idempotencyKey = command.IdempotencyKey?.Trim() ?? string.Empty;
        string normalizedInput = command.Input?.Trim() ?? string.Empty;
        string sourceType = string.IsNullOrWhiteSpace(command.SourceType)
            ? "chat"
            : command.SourceType.Trim();
        ProtectedUnifiedPayload protectedInput = UnifiedEntryPayloadProtector.Protect(
            normalizedInput,
            AgentRuntimeService.MaximumInputCharacters * 4,
            AgentRuntimeService.MaximumInputCharacters * 4);
        if (!string.Equals(protectedInput.Content, normalizedInput, StringComparison.Ordinal))
        {
            throw Invalid("The task input contains protected content and cannot be persisted for deferred execution.");
        }

        if (idempotencyKey.Length > 0)
        {
            AgAgentTask? existing = await Db.Queryable<AgAgentTask>()
                .Where(value => value.TenantId == tenantId &&
                                value.UserId == userId &&
                                value.IdempotencyKey == idempotencyKey &&
                                !value.IsDeleted)
                .FirstAsync();
            if (existing is not null)
            {
                EnsureIdempotencyMatch(existing, command, sourceType, protectedInput.OriginalSha256);
                return Map(existing);
            }
        }

        AgAgentTask entity = new()
        {
            ID = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Title = command.Title.Trim(),
            Description = command.Description?.Trim() ?? string.Empty,
            Input = normalizedInput,
            InputSha256 = protectedInput.OriginalSha256,
            SourceType = sourceType,
            SourceId = command.SourceId?.Trim() ?? string.Empty,
            IdempotencyKey = idempotencyKey.Length == 0
                ? null
                : idempotencyKey,
            ConversationId = command.ConversationId,
            Status = (int)AgentTaskStatus.Pending,
            Priority = command.Priority,
            AttemptCount = 0,
            MaximumAttempts = command.MaximumAttempts,
            LogicalRevision = 0,
            AvailableAtUtc = command.AvailableAtUtc.UtcDateTime,
            IsDeleted = false,
            IsActive = true
        };
        DateTime createdAt = DateTime.UtcNow;
        await Db.Ado.BeginTranAsync();
        try
        {
            await Db.Insertable(entity).ExecuteCommandAsync();
            await AppendEventAsync(entity.ID, null, null, AgentTaskEventKinds.Created,
                AgentTaskStatus.Pending, string.Empty, createdAt,
                JsonSerializer.Serialize(new { sourceType, sourceId = entity.SourceId }));
            await Db.Ado.CommitTranAsync();
            return Map(entity);
        }
        catch when (idempotencyKey.Length > 0)
        {
            await Db.Ado.RollbackTranAsync();
            AgAgentTask? concurrent = await Db.Queryable<AgAgentTask>()
                .Where(value => value.TenantId == tenantId &&
                                value.UserId == userId &&
                                value.IdempotencyKey == idempotencyKey &&
                                !value.IsDeleted)
                .FirstAsync();
            if (concurrent is not null)
            {
                EnsureIdempotencyMatch(concurrent, command, sourceType, protectedInput.OriginalSha256);
                return Map(concurrent);
            }

            throw;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<AgentTaskRecord>> ListAsync(AgentTaskQuery query, CancellationToken cancellationToken = default)
    {
        Required(query.TenantId, nameof(query.TenantId));
        Required(query.UserId, nameof(query.UserId));
        if (query.Take is < 1 or > MaximumTake)
        {
            throw Invalid("The task page size must be between 1 and 200.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<AgAgentTask> values = await Db.Queryable<AgAgentTask>()
            .Where(value => value.TenantId == query.TenantId && value.UserId == query.UserId && !value.IsDeleted)
            .WhereIF(query.Status.HasValue, value => value.Status == (int)query.Status!.Value)
            .OrderBy(value => value.CreatedTime, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(query.Take)
            .ToListAsync();
        return values.Select(Map).ToArray();
    }

    public async Task<AgentTaskRecord?> GetAsync(Guid id, string tenantId, string? userId, CancellationToken cancellationToken = default)
    {
        Required(tenantId, nameof(tenantId));
        cancellationToken.ThrowIfCancellationRequested();
        AgAgentTask? value = await Db.Queryable<AgAgentTask>()
            .Where(item => item.ID == id && item.TenantId == tenantId && !item.IsDeleted)
            .WhereIF(!string.IsNullOrWhiteSpace(userId), item => item.UserId == userId)
            .FirstAsync();
        return value is null ? null : Map(value);
    }

    public async Task<IReadOnlyList<AgentTaskAttemptRecord>> ListAttemptsAsync(
        Guid taskId,
        string tenantId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        AgentTaskRecord? task = await GetAsync(taskId, tenantId, userId, cancellationToken);
        if (task is null)
        {
            throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        }

        List<AgAgentTaskAttempt> values = await Db.Queryable<AgAgentTaskAttempt>()
            .Where(value => value.TaskId == taskId && !value.IsDeleted)
            .OrderBy(value => value.AttemptNumber)
            .ToListAsync();
        return values.Select(MapAttempt).ToArray();
    }

    public async Task<IReadOnlyList<AgentTaskEventRecord>> ListEventsAsync(
        Guid taskId, string tenantId, string? userId, int take = 200,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 500) throw Invalid("The task event page size must be between 1 and 500.");
        AgentTaskRecord? task = await GetAsync(taskId, tenantId, userId, cancellationToken);
        if (task is null)
        {
            throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        }

        List<AgAgentTaskEvent> values = await Db.Queryable<AgAgentTaskEvent>()
            .Where(value => value.TaskId == taskId && !value.IsDeleted)
            .OrderBy(value => value.OccurredAtUtc, OrderByType.Desc)
            .OrderBy(value => value.CreatedTime, OrderByType.Desc)
            .Take(take)
            .ToListAsync();
        return values
            .OrderBy(value => value.OccurredAtUtc)
            .ThenBy(value => value.CreatedTime)
            .Select(MapEvent)
            .ToArray();
    }

    public async Task<AgentTaskRecord?> TryClaimNextAsync(ClaimAgentTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (!command.AcrossTenants)
        {
            Required(command.TenantId, nameof(command.TenantId));
        }
        Required(command.WorkerId, nameof(command.WorkerId));
        if (command.LeaseDuration < TimeSpan.FromSeconds(10) || command.LeaseDuration > TimeSpan.FromHours(1))
        {
            throw Invalid("The task lease duration must be between 10 seconds and 1 hour.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = command.ClaimedAtUtc.UtcDateTime;
        List<AgAgentTask> candidates = await Db.Queryable<AgAgentTask>()
            .Where(value => !value.IsDeleted &&
                ((value.Status == (int)AgentTaskStatus.Pending && value.AvailableAtUtc <= now &&
                  (value.AttemptCount < value.MaximumAttempts || value.CheckpointKind == "user-input-received")) ||
                 (value.Status == (int)AgentTaskStatus.Running && value.LeaseExpiresAtUtc <= now)))
            .WhereIF(!command.AcrossTenants, value => value.TenantId == command.TenantId)
            .WhereIF(!string.IsNullOrWhiteSpace(command.SourceType), value => value.SourceType == command.SourceType)
            .OrderBy(value => value.Priority, OrderByType.Desc)
            .OrderBy(value => value.AvailableAtUtc)
            .OrderBy(value => value.CreatedTime)
            .Take(10)
            .ToListAsync();

        foreach (AgAgentTask candidate in candidates)
        {
            int revision = checked((int)(candidate.LogicalRevision ?? 0));
            int currentAttemptNumber = candidate.AttemptCount ?? 0;
            bool exhaustedExpiredLease = candidate.Status == (int)AgentTaskStatus.Running &&
                currentAttemptNumber >= (candidate.MaximumAttempts ?? 1);
            if (exhaustedExpiredLease)
            {
                await Db.Ado.BeginTranAsync(IsolationLevel.ReadCommitted);
                try
                {
                    const string leaseExpiredMessage = "The worker lease expired after the maximum attempt count was reached.";
                    int failed = await Db.Updateable<AgAgentTask>()
                        .SetColumns(value => new AgAgentTask
                        {
                            Status = (int)AgentTaskStatus.Failed,
                            FinishedAtUtc = now,
                            LeaseOwner = string.Empty,
                            LeaseExpiresAtUtc = null,
                            LastErrorCode = AgentTaskErrorCodes.LeaseInvalid,
                            LastErrorMessage = leaseExpiredMessage,
                            LogicalRevision = value.LogicalRevision + 1
                        })
                        .Where(value => value.ID == candidate.ID && value.TenantId == candidate.TenantId &&
                                        value.Status == (int)AgentTaskStatus.Running &&
                                        value.LeaseExpiresAtUtc <= now &&
                                        value.AttemptCount >= value.MaximumAttempts &&
                                        value.LogicalRevision == revision && !value.IsDeleted)
                        .ExecuteCommandAsync();
                    if (failed != 1)
                    {
                        await Db.Ado.RollbackTranAsync();
                        continue;
                    }

                    await FinishAttemptAsync(candidate.ID, currentAttemptNumber, candidate.CurrentRunId,
                        AgentTaskAttemptStatus.Failed, now, AgentTaskErrorCodes.LeaseInvalid, leaseExpiredMessage);
                    await AppendEventAsync(candidate.ID, currentAttemptNumber, candidate.CurrentRunId,
                        AgentTaskEventKinds.Failed, AgentTaskStatus.Failed,
                        candidate.LeaseOwner ?? string.Empty, now,
                        JsonSerializer.Serialize(new { errorCode = AgentTaskErrorCodes.LeaseInvalid }));
                    await Db.Ado.CommitTranAsync();
                }
                catch
                {
                    await Db.Ado.RollbackTranAsync();
                    throw;
                }

                continue;
            }

            int attemptNumber = currentAttemptNumber + 1;
            DateTime leaseExpires = now.Add(command.LeaseDuration);
            await Db.Ado.BeginTranAsync(IsolationLevel.ReadCommitted);
            try
            {
                int updated = await Db.Updateable<AgAgentTask>()
                    .SetColumns(value => new AgAgentTask
                    {
                        Status = (int)AgentTaskStatus.Running,
                        AttemptCount = attemptNumber,
                        LogicalRevision = value.LogicalRevision + 1,
                        StartedAtUtc = value.StartedAtUtc ?? now,
                        FinishedAtUtc = null,
                        LeaseOwner = command.WorkerId,
                        LeaseExpiresAtUtc = leaseExpires,
                        LastErrorCode = string.Empty,
                        LastErrorMessage = string.Empty
                    })
                    .Where(value => value.ID == candidate.ID && value.TenantId == candidate.TenantId &&
                                    value.LogicalRevision == revision && !value.IsDeleted &&
                                    ((value.Status == (int)AgentTaskStatus.Pending && value.AvailableAtUtc <= now &&
                                      (value.AttemptCount < value.MaximumAttempts || value.CheckpointKind == "user-input-received")) ||
                                     (value.Status == (int)AgentTaskStatus.Running && value.LeaseExpiresAtUtc <= now)))
                    .ExecuteCommandAsync();
                if (updated != 1)
                {
                    await Db.Ado.RollbackTranAsync();
                    continue;
                }

                await Db.Updateable<AgAgentTaskAttempt>()
                    .SetColumns(_ => new AgAgentTaskAttempt
                    {
                        RunId = candidate.CurrentRunId,
                        Status = (int)AgentTaskAttemptStatus.Failed,
                        FinishedAtUtc = now,
                        ErrorCode = AgentTaskErrorCodes.LeaseInvalid,
                        ErrorMessage = "The previous worker lease expired."
                    })
                    .Where(value => value.TaskId == candidate.ID &&
                                    value.Status == (int)AgentTaskAttemptStatus.Running &&
                                    !value.IsDeleted)
                    .ExecuteCommandAsync();
                await Db.Insertable(new AgAgentTaskAttempt
                {
                    ID = Guid.NewGuid(),
                    TaskId = candidate.ID,
                    AttemptNumber = attemptNumber,
                    RunId = candidate.CurrentRunId,
                    Status = (int)AgentTaskAttemptStatus.Running,
                    WorkerId = command.WorkerId,
                    StartedAtUtc = now,
                    IsDeleted = false,
                    IsActive = true
                }).ExecuteCommandAsync();
                await Db.Ado.CommitTranAsync();
                return await GetAsync(candidate.ID, candidate.TenantId ?? string.Empty, null, cancellationToken);
            }
            catch
            {
                await Db.Ado.RollbackTranAsync();
                throw;
            }
        }

        return null;
    }

    public async Task<AgentTaskRecord> RenewLeaseAsync(RenewAgentTaskLeaseCommand command, CancellationToken cancellationToken = default)
    {
        Required(command.TenantId, nameof(command.TenantId));
        Required(command.WorkerId, nameof(command.WorkerId));
        ValidateLeaseDuration(command.LeaseDuration);
        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = command.RenewedAtUtc.UtcDateTime;
        int updated = await Db.Updateable<AgAgentTask>()
            .SetColumns(value => new AgAgentTask
            {
                LeaseExpiresAtUtc = now.Add(command.LeaseDuration),
                LogicalRevision = value.LogicalRevision + 1
            })
            .Where(value => value.ID == command.TaskId && value.TenantId == command.TenantId &&
                            value.Status == (int)AgentTaskStatus.Running &&
                            value.LeaseOwner == command.WorkerId && value.LeaseExpiresAtUtc > now &&
                            value.LogicalRevision == command.ExpectedLogicalRevision && !value.IsDeleted)
            .ExecuteCommandAsync();
        return await RequireUpdatedAsync(command.TaskId, command.TenantId, updated, cancellationToken);
    }

    public async Task<AgentTaskRecord> SaveCheckpointAsync(SaveAgentTaskCheckpointCommand command, CancellationToken cancellationToken = default)
    {
        Required(command.CheckpointKind, nameof(command.CheckpointKind));
        ValidateCheckpoint(command.CheckpointJson);
        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = command.SavedAtUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    CurrentRunId = command.RunId,
                    ConversationId = command.ConversationId,
                    CheckpointKind = command.CheckpointKind,
                    CheckpointJson = command.CheckpointJson,
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == command.TaskId && value.TenantId == command.TenantId &&
                                value.Status == (int)AgentTaskStatus.Running &&
                                value.LeaseOwner == command.WorkerId && value.LeaseExpiresAtUtc > now &&
                                value.LogicalRevision == command.ExpectedLogicalRevision && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1) throw Conflict();

            if (command.RunId.HasValue)
            {
                int attemptUpdated = await Db.Updateable<AgAgentTaskAttempt>()
                    .SetColumns(value => new AgAgentTaskAttempt { RunId = command.RunId })
                    .Where(value => value.TaskId == command.TaskId &&
                                    value.Status == (int)AgentTaskAttemptStatus.Running && !value.IsDeleted)
                    .ExecuteCommandAsync();
                if (attemptUpdated != 1) throw Conflict();
            }

            await AppendEventAsync(command.TaskId, null, command.RunId,
                AgentTaskEventKinds.CheckpointSaved, AgentTaskStatus.Running, command.WorkerId, now,
                JsonSerializer.Serialize(new { checkpointKind = command.CheckpointKind }));
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(command.TaskId, command.TenantId, null, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<AgentTaskRecord> WaitAsync(WaitAgentTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Status is not AgentTaskStatus.WaitingForApproval and not AgentTaskStatus.WaitingForUser)
        {
            throw Invalid("The requested task waiting state is invalid.");
        }

        ValidateCheckpoint(command.CheckpointJson);
        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = command.PausedAtUtc.UtcDateTime;
        AgentTaskRecord current = await GetAsync(command.TaskId, command.TenantId, null, cancellationToken)
            ?? throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    Status = (int)command.Status,
                    CurrentRunId = command.RunId,
                    ConversationId = command.ConversationId,
                    CheckpointKind = command.CheckpointKind,
                    CheckpointJson = command.CheckpointJson,
                    LeaseOwner = string.Empty,
                    LeaseExpiresAtUtc = null,
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == command.TaskId && value.TenantId == command.TenantId &&
                                value.Status == (int)AgentTaskStatus.Running &&
                                value.LeaseOwner == command.WorkerId && value.LeaseExpiresAtUtc > now &&
                                value.LogicalRevision == command.ExpectedLogicalRevision && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                throw Conflict();
            }

            await FinishAttemptAsync(command.TaskId, current.AttemptCount, command.RunId,
                AgentTaskAttemptStatus.Paused, now, string.Empty, string.Empty);
            await AppendEventAsync(command.TaskId, current.AttemptCount, command.RunId,
                command.Status == AgentTaskStatus.WaitingForApproval
                    ? AgentTaskEventKinds.WaitingForApproval
                    : AgentTaskEventKinds.WaitingForUser,
                command.Status, command.WorkerId, now,
                JsonSerializer.Serialize(new { checkpointKind = command.CheckpointKind }));
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(command.TaskId, command.TenantId, null, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<AgentTaskRecord> CompleteAsync(CompleteAgentTaskCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = command.FinishedAtUtc.UtcDateTime;
        AgentTaskRecord current = await GetAsync(command.TaskId, command.TenantId, null, cancellationToken)
            ?? throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    CurrentRunId = command.RunId,
                    Status = (int)AgentTaskStatus.Completed,
                    FinishedAtUtc = now,
                    LeaseOwner = string.Empty,
                    LeaseExpiresAtUtc = null,
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == command.TaskId && value.TenantId == command.TenantId &&
                                value.Status == (int)AgentTaskStatus.Running &&
                                value.LeaseOwner == command.WorkerId && value.LeaseExpiresAtUtc > now &&
                                value.LogicalRevision == command.ExpectedLogicalRevision && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                throw Conflict();
            }

            await FinishAttemptAsync(command.TaskId, current.AttemptCount, command.RunId,
                AgentTaskAttemptStatus.Completed, now, string.Empty, string.Empty);
            await AppendEventAsync(command.TaskId, current.AttemptCount, command.RunId,
                AgentTaskEventKinds.Completed, AgentTaskStatus.Completed, command.WorkerId, now, "{}");
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(command.TaskId, command.TenantId, null, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<AgentTaskRecord> FailAsync(FailAgentTaskCommand command, CancellationToken cancellationToken = default)
    {
        Required(command.ErrorCode, nameof(command.ErrorCode));
        if (command.RetryDelay < TimeSpan.Zero || command.RetryDelay > TimeSpan.FromDays(1))
        {
            throw Invalid("The task retry delay must be between zero and one day.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        AgentTaskRecord current = await GetAsync(command.TaskId, command.TenantId, null, cancellationToken)
            ?? throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        bool terminal = current.AttemptCount >= current.MaximumAttempts;
        DateTime now = command.FailedAtUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    Status = terminal ? (int)AgentTaskStatus.Failed : (int)AgentTaskStatus.Pending,
                    CurrentRunId = terminal ? current.CurrentRunId : null,
                    AvailableAtUtc = terminal ? value.AvailableAtUtc : now.Add(command.RetryDelay),
                    FinishedAtUtc = terminal ? now : null,
                    LeaseOwner = string.Empty,
                    LeaseExpiresAtUtc = null,
                    LastErrorCode = command.ErrorCode,
                    LastErrorMessage = ProtectErrorMessage(command.ErrorMessage),
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == command.TaskId && value.TenantId == command.TenantId &&
                                value.Status == (int)AgentTaskStatus.Running &&
                                value.LeaseOwner == command.WorkerId && value.LeaseExpiresAtUtc > now &&
                                value.LogicalRevision == command.ExpectedLogicalRevision && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                throw Conflict();
            }

            await FinishAttemptAsync(command.TaskId, current.AttemptCount, current.CurrentRunId,
                AgentTaskAttemptStatus.Failed, now, command.ErrorCode, command.ErrorMessage);
            await AppendEventAsync(command.TaskId, current.AttemptCount, current.CurrentRunId,
                terminal ? AgentTaskEventKinds.Failed : AgentTaskEventKinds.RetryScheduled,
                terminal ? AgentTaskStatus.Failed : AgentTaskStatus.Pending,
                command.WorkerId, now, JsonSerializer.Serialize(new { errorCode = command.ErrorCode }));
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(command.TaskId, command.TenantId, null, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<AgentTaskRecord> CancelAsync(
        Guid id,
        string tenantId,
        string userId,
        DateTimeOffset cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        Required(tenantId, nameof(tenantId));
        Required(userId, nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = cancelledAtUtc.UtcDateTime;
        AgentTaskRecord current = await GetAsync(id, tenantId, userId, cancellationToken)
            ?? throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        if (current.Status == AgentTaskStatus.Cancelled)
        {
            return current;
        }

        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    Status = (int)AgentTaskStatus.Cancelled,
                    FinishedAtUtc = now,
                    LeaseOwner = string.Empty,
                    LeaseExpiresAtUtc = null,
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == id && value.TenantId == tenantId && value.UserId == userId &&
                                value.Status != (int)AgentTaskStatus.Completed &&
                                value.Status != (int)AgentTaskStatus.Failed &&
                                value.Status != (int)AgentTaskStatus.Cancelled && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                throw Conflict();
            }

            await FinishAttemptAsync(id, current.AttemptCount, current.CurrentRunId,
                AgentTaskAttemptStatus.Cancelled, now, string.Empty, string.Empty);
            await AppendEventAsync(id, current.AttemptCount == 0 ? null : current.AttemptCount,
                current.CurrentRunId, AgentTaskEventKinds.Cancelled, AgentTaskStatus.Cancelled,
                string.Empty, now, "{}");
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(id, tenantId, userId, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<AgentTaskRecord> ResumeWithUserInputAsync(ResumeAgentTaskWithUserInputCommand command, CancellationToken cancellationToken = default)
    {
        Required(command.TenantId, nameof(command.TenantId));
        Required(command.UserId, nameof(command.UserId));
        Required(command.Input, nameof(command.Input));
        string normalizedInput = command.Input.Trim();
        if (normalizedInput.Length > AgentRuntimeService.MaximumInputCharacters)
        {
            throw Invalid($"The task input exceeds {AgentRuntimeService.MaximumInputCharacters} characters.");
        }

        ProtectedUnifiedPayload protectedInput = UnifiedEntryPayloadProtector.Protect(
            normalizedInput, AgentRuntimeService.MaximumInputCharacters * 4,
            AgentRuntimeService.MaximumInputCharacters * 4);
        if (!string.Equals(protectedInput.Content, normalizedInput, StringComparison.Ordinal))
        {
            throw Invalid("The task input contains protected content and cannot be persisted for deferred execution.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = command.ResumedAtUtc.UtcDateTime;
        AgentTaskRecord current = await GetAsync(command.TaskId, command.TenantId, command.UserId, cancellationToken)
            ?? throw new AgentTaskException(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    Input = normalizedInput,
                    InputSha256 = protectedInput.OriginalSha256,
                    Status = (int)AgentTaskStatus.Pending,
                    CurrentRunId = null,
                    AvailableAtUtc = now,
                    FinishedAtUtc = null,
                    CheckpointKind = "user-input-received",
                    CheckpointJson = JsonSerializer.Serialize(new
                    {
                        previousRunId = current.CurrentRunId,
                        inputSha256 = protectedInput.OriginalSha256
                    }),
                    LastErrorCode = string.Empty,
                    LastErrorMessage = string.Empty,
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == command.TaskId && value.TenantId == command.TenantId &&
                                value.UserId == command.UserId &&
                                value.Status == (int)AgentTaskStatus.WaitingForUser &&
                                value.LogicalRevision == command.ExpectedLogicalRevision && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1) throw Conflict();

            await AppendEventAsync(command.TaskId, current.AttemptCount, current.CurrentRunId,
                AgentTaskEventKinds.ResumedByUser, AgentTaskStatus.Pending, string.Empty, now,
                JsonSerializer.Serialize(new { previousRunId = current.CurrentRunId }));
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(command.TaskId, command.TenantId, command.UserId, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<AgentTaskRecord?> SynchronizeRunAsync(SynchronizeAgentTaskRunCommand command, CancellationToken cancellationToken = default)
    {
        Required(command.TenantId, nameof(command.TenantId));
        Required(command.UserId, nameof(command.UserId));
        if (command.RunId == Guid.Empty || command.Status is not (
            AgentTaskStatus.Completed or AgentTaskStatus.Failed or AgentTaskStatus.Cancelled))
        {
            throw Invalid("The task run synchronization result is invalid.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        AgAgentTask? entity = await Db.Queryable<AgAgentTask>()
            .Where(value => value.CurrentRunId == command.RunId &&
                            value.TenantId == command.TenantId &&
                            value.UserId == command.UserId && !value.IsDeleted)
            .FirstAsync();
        if (entity is null)
        {
            return null;
        }

        AgentTaskStatus currentStatus = (AgentTaskStatus)(entity.Status ?? 0);
        if (currentStatus is AgentTaskStatus.Completed or AgentTaskStatus.Failed or AgentTaskStatus.Cancelled)
        {
            return Map(entity);
        }

        if (currentStatus != AgentTaskStatus.WaitingForApproval)
        {
            throw Conflict();
        }

        DateTime finished = command.FinishedAtUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgAgentTask>()
                .SetColumns(value => new AgAgentTask
                {
                    Status = (int)command.Status,
                    FinishedAtUtc = finished,
                    CheckpointKind = "approval-resolved",
                    CheckpointJson = JsonSerializer.Serialize(new
                    {
                        runId = command.RunId,
                        status = command.Status.ToString(),
                        errorCode = command.ErrorCode
                    }),
                    LastErrorCode = command.ErrorCode,
                    LastErrorMessage = string.Empty,
                    LogicalRevision = value.LogicalRevision + 1
                })
                .Where(value => value.ID == entity.ID &&
                                value.Status == (int)AgentTaskStatus.WaitingForApproval &&
                                value.LogicalRevision == entity.LogicalRevision && !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1) throw Conflict();

            await AppendEventAsync(entity.ID, entity.AttemptCount, command.RunId,
                AgentTaskEventKinds.RunSynchronized, command.Status, string.Empty, finished,
                JsonSerializer.Serialize(new { errorCode = command.ErrorCode }));
            await Db.Ado.CommitTranAsync();
            return (await GetAsync(entity.ID, command.TenantId, null, cancellationToken))!;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private Task<int> AppendEventAsync(
        Guid taskId, int? attemptNumber, Guid? runId, string kind,
        AgentTaskStatus status, string workerId, DateTime occurredAtUtc, string payloadJson) =>
        Db.Insertable(new AgAgentTaskEvent
        {
            ID = Guid.NewGuid(),
            TaskId = taskId,
            AttemptNumber = attemptNumber,
            RunId = runId,
            Kind = kind,
            Status = (int)status,
            WorkerId = workerId,
            OccurredAtUtc = occurredAtUtc,
            PayloadJson = payloadJson,
            IsDeleted = false,
            IsActive = true
        }).ExecuteCommandAsync();

    private async Task<AgentTaskRecord> RequireUpdatedAsync(Guid id, string tenantId, int updated, CancellationToken cancellationToken)
    {
        if (updated != 1)
        {
            throw Conflict();
        }

        return (await GetAsync(id, tenantId, null, cancellationToken))!;
    }

    private Task<int> FinishAttemptAsync(
        Guid taskId,
        int attemptNumber,
        Guid? runId,
        AgentTaskAttemptStatus status,
        DateTime finishedAtUtc,
        string errorCode,
        string? errorMessage) =>
        Db.Updateable<AgAgentTaskAttempt>()
            .SetColumns(_ => new AgAgentTaskAttempt
            {
                RunId = runId,
                Status = (int)status,
                FinishedAtUtc = finishedAtUtc,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage ?? string.Empty
            })
            .Where(value => value.TaskId == taskId && value.AttemptNumber == attemptNumber &&
                            value.Status == (int)AgentTaskAttemptStatus.Running && !value.IsDeleted)
            .ExecuteCommandAsync();

    private static void ValidateCreate(CreateAgentTaskCommand command)
    {
        Required(command.TenantId, nameof(command.TenantId));
        Required(command.UserId, nameof(command.UserId));
        Required(command.Title, nameof(command.Title));
        Required(command.Input, nameof(command.Input));
        if (!string.IsNullOrWhiteSpace(command.SourceType) &&
            !string.Equals(command.SourceType.Trim(), "chat", StringComparison.Ordinal))
        {
            throw Invalid("The requested task source type does not have a registered executor.");
        }
        if (command.Input.Trim().Length > AgentRuntimeService.MaximumInputCharacters)
        {
            throw Invalid($"The task input exceeds {AgentRuntimeService.MaximumInputCharacters} characters.");
        }
        if (command.Title.Trim().Length > 256 || command.IdempotencyKey?.Trim().Length > 128 ||
            command.SourceType?.Trim().Length > 64 || command.SourceId?.Trim().Length > 256)
        {
            throw Invalid("One or more task fields exceed their maximum length.");
        }

        if (command.MaximumAttempts is < 1 or > 20 || command.Priority is < -100 or > 100)
        {
            throw Invalid("The task retry or priority setting is invalid.");
        }
    }

    private static void EnsureIdempotencyMatch(AgAgentTask existing, CreateAgentTaskCommand command, string sourceType, string inputSha256)
    {
        if (!string.Equals(existing.UserId, command.UserId.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existing.Title, command.Title.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existing.Description ?? string.Empty, command.Description?.Trim() ?? string.Empty, StringComparison.Ordinal) ||
            !string.Equals(existing.SourceType, sourceType, StringComparison.Ordinal) ||
            !string.Equals(existing.SourceId ?? string.Empty, command.SourceId?.Trim() ?? string.Empty, StringComparison.Ordinal) ||
            !string.Equals(existing.InputSha256, inputSha256, StringComparison.Ordinal) ||
            existing.Priority != command.Priority ||
            existing.MaximumAttempts != command.MaximumAttempts ||
            (command.ConversationId.HasValue && existing.ConversationId != command.ConversationId))
        {
            throw new AgentTaskException(
                AgentTaskErrorCodes.IdempotencyKeyReused,
                "The Agent task idempotency key was reused with different task content.");
        }
    }

    private static void ValidateCheckpoint(string checkpointJson)
    {
        if (string.IsNullOrWhiteSpace(checkpointJson) || checkpointJson.Length > MaximumCheckpointLength)
        {
            throw Invalid("The task checkpoint is empty or too large.");
        }

        try
        {
            using JsonDocument _ = JsonDocument.Parse(checkpointJson);
        }
        catch (JsonException)
        {
            throw Invalid("The task checkpoint must be valid JSON.");
        }
    }

    private static string ProtectErrorMessage(string? value)
    {
        string bounded = (value ?? string.Empty).Trim();
        if (bounded.Length > 4_096)
        {
            bounded = bounded[..4_096];
        }

        return UnifiedEntryPayloadProtector.Protect(
            bounded,
            16_384,
            16_384).Content;
    }

    private static void ValidateLeaseDuration(TimeSpan leaseDuration)
    {
        if (leaseDuration < TimeSpan.FromSeconds(10) || leaseDuration > TimeSpan.FromHours(1))
        {
            throw Invalid("The task lease duration must be between 10 seconds and 1 hour.");
        }
    }

    private static void Required(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid($"The task field '{field}' is required.");
        }
    }

    private static AgentTaskException Invalid(string message) => new(AgentTaskErrorCodes.Invalid, message);
    private static AgentTaskException Conflict() => new(AgentTaskErrorCodes.Conflict, "The Agent task state or lease has changed.");
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static AgentTaskRecord Map(AgAgentTask value) => new(
        value.ID,
        value.TenantId ?? string.Empty,
        value.UserId ?? string.Empty,
        value.Title ?? string.Empty,
        value.Description ?? string.Empty,
        value.Input ?? string.Empty,
        value.InputSha256 ?? string.Empty,
        value.SourceType ?? string.Empty,
        value.SourceId ?? string.Empty,
        value.IdempotencyKey ?? string.Empty,
        value.ConversationId,
        value.CurrentRunId,
        (AgentTaskStatus)(value.Status ?? 0),
        value.Priority ?? 0,
        value.AttemptCount ?? 0,
        value.MaximumAttempts ?? 1,
        value.LogicalRevision ?? 0,
        ToOffset(value.AvailableAtUtc ?? DateTime.UtcNow),
        value.StartedAtUtc.HasValue ? ToOffset(value.StartedAtUtc.Value) : null,
        value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
        value.LeaseOwner ?? string.Empty,
        value.LeaseExpiresAtUtc.HasValue ? ToOffset(value.LeaseExpiresAtUtc.Value) : null,
        value.CheckpointKind ?? string.Empty,
        value.CheckpointJson ?? string.Empty,
        value.LastErrorCode ?? string.Empty,
        value.LastErrorMessage ?? string.Empty);

    private static AgentTaskAttemptRecord MapAttempt(AgAgentTaskAttempt value) => new(
        value.ID,
        value.TaskId ?? Guid.Empty,
        value.AttemptNumber ?? 0,
        value.RunId,
        (AgentTaskAttemptStatus)(value.Status ?? 0),
        value.WorkerId ?? string.Empty,
        ToOffset(value.StartedAtUtc ?? DateTime.UtcNow),
        value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
        value.ErrorCode ?? string.Empty,
        value.ErrorMessage ?? string.Empty);

    private static AgentTaskEventRecord MapEvent(AgAgentTaskEvent value) => new(
        value.ID,
        value.TaskId ?? Guid.Empty,
        value.AttemptNumber,
        value.RunId,
        value.Kind ?? string.Empty,
        (AgentTaskStatus)(value.Status ?? 0),
        value.WorkerId ?? string.Empty,
        ToOffset(value.OccurredAtUtc ?? DateTime.UtcNow),
        value.PayloadJson ?? string.Empty);
}

#endregion

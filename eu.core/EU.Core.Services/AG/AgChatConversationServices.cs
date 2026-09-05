using System.Text;
using System.Text.Json;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgChatConversationServices 职责实现

/// <summary>
/// 提供 Agent 聊天会话的持久化服务。
/// </summary>
public sealed class AgChatConversationServices :
    BaseServices<AgChatConversation>,
    IAgChatConversationServices,
    IUnifiedEntryRepository,
    IUnifiedEntryRecovery
{
    #region 构造（AgChatConversationServices）
    /// <summary>
    /// 构造（AgChatConversationServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgChatConversationServices(IBaseRepository<AgChatConversation> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 获取（GetConversationAsync）
    /// <summary>
    /// 获取（GetConversationAsync）
    /// </summary>
    /// <param name="id">会话标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的未删除会话；不存在时为 null。</returns>
    public async Task<ConversationRecord?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgChatConversation? value = await Db.Queryable<AgChatConversation>()
            .Where(item => item.ID == id && !item.IsDeleted)
            .FirstAsync();
        return value is null ? null : MapConversation(value);
    }
    #endregion

    #region 查询列表（ListConversationsAsync）
    /// <summary>
    /// 查询列表（ListConversationsAsync）
    /// </summary>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按更新时间倒序、标识升序排列的未删除会话，最多 100 条。</returns>
    public async Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgChatConversation> values = await Db.Queryable<AgChatConversation>()
            .Where(value => !value.IsDeleted)
            .OrderBy(value => value.UpdatedAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
        return values.Select(MapConversation).ToArray();
    }
    #endregion

    #region 查询列表（ListMessagesAsync）
    /// <summary>
    /// 查询列表（ListMessagesAsync）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>会话最近的消息，按消息序号升序返回。</returns>
    public async Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesAsync(
        Guid conversationId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ReadMessagesAsync(conversationId, take);
    }
    #endregion

    #region 获取（GetRunAsync）
    /// <summary>
    /// 获取（GetRunAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的未删除统一入口运行；不存在时为 null。</returns>
    public async Task<UnifiedEntryRunRecord?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgUnifiedEntryRun? value = await Db.Queryable<AgUnifiedEntryRun>()
            .Where(item => item.ID == runId && !item.IsDeleted)
            .FirstAsync();
        return value is null ? null : MapEntryRun(value);
    }
    #endregion

    #region 查询列表（ListRunsAsync）
    /// <summary>
    /// 查询列表（ListRunsAsync）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定会话的运行列表，按开始时间倒序、标识升序排列，最多 100 条。</returns>
    public async Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsAsync(Guid conversationId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgUnifiedEntryRun> values = await Db.Queryable<AgUnifiedEntryRun>()
            .Where(value => value.ConversationId == conversationId && !value.IsDeleted)
            .OrderBy(value => value.StartedAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
        return values.Select(MapEntryRun).ToArray();
    }
    #endregion

    #region 获取（GetDetailsAsync）
    /// <summary>
    /// 获取（GetDetailsAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含 Agent、编排和工具调用的运行详情；入口运行不存在时为 null。</returns>
    public async Task<UnifiedRunDetails?> GetDetailsAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync();
        try
        {
            AgUnifiedEntryRun? entry = await Db.Queryable<AgUnifiedEntryRun>()
                .Where(value => value.ID == runId && !value.IsDeleted)
                .FirstAsync();
            if (entry is null)
            {
                await Db.Ado.CommitTranAsync();
                return null;
            }

            UnifiedRunDetails result = await ReadDetailsAsync(entry);
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 查询列表（ListEventsAsync）
    /// <summary>
    /// 查询列表（ListEventsAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定运行的未删除事件，按事件序号升序排列。</returns>
    public async Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ReadEventsAsync(runId);
    }
    #endregion

    #region 获取（GetConversationForOwnerAsync）
    /// <summary>
    /// 获取（GetConversationForOwnerAsync）
    /// </summary>
    /// <param name="id">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的会话；不存在或所属身份不匹配时为 null。</returns>
    public async Task<ConversationRecord?> GetConversationForOwnerAsync(Guid id, string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgChatConversation? value = await Db.Queryable<AgChatConversation>()
            .Where(item => item.ID == id && item.TenantId == tenantId &&
                           item.UserId == userId && !item.IsDeleted)
            .FirstAsync();
        return value is null ? null : MapConversation(value);
    }
    #endregion

    #region 查询列表（ListConversationsForOwnerAsync）
    /// <summary>
    /// 查询列表（ListConversationsForOwnerAsync）
    /// </summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和用户的会话列表，按更新时间倒序、标识升序排列，最多 100 条。</returns>
    public async Task<IReadOnlyList<ConversationRecord>> ListConversationsForOwnerAsync(
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgChatConversation> values = await Db.Queryable<AgChatConversation>()
            .Where(value => value.TenantId == tenantId && value.UserId == userId &&
                            !value.IsDeleted)
            .OrderBy(value => value.UpdatedAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
        return values.Select(MapConversation).ToArray();
    }
    #endregion

    #region 查询列表（ListMessagesForOwnerAsync）
    /// <summary>
    /// 查询列表（ListMessagesForOwnerAsync）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>所属身份匹配的会话最近消息，按序号升序排列；会话不存在或不属于该用户时为空集合。</returns>
    public async Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Db.Queryable<AgChatConversation>()
                .Where(value => value.ID == conversationId && value.TenantId == tenantId &&
                                value.UserId == userId && !value.IsDeleted)
                .AnyAsync())
        {
            return [];
        }

        return await ReadMessagesAsync(conversationId, take);
    }
    #endregion

    #region 获取（GetRunForOwnerAsync）
    /// <summary>
    /// 获取（GetRunForOwnerAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的入口运行；不存在或所属身份不匹配时为 null。</returns>
    public async Task<UnifiedEntryRunRecord?> GetRunForOwnerAsync(Guid runId, string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgUnifiedEntryRun? value = await Db.Queryable<AgUnifiedEntryRun>()
            .Where(item => item.ID == runId && item.TenantId == tenantId &&
                           item.UserId == userId && !item.IsDeleted)
            .FirstAsync();
        return value is null ? null : MapEntryRun(value);
    }
    #endregion

    #region 查询列表（ListRunsForOwnerAsync）
    /// <summary>
    /// 查询列表（ListRunsForOwnerAsync）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定会话中属于该租户和用户的运行列表，按开始时间倒序、标识升序排列，最多 100 条。</returns>
    public async Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgUnifiedEntryRun> values = await Db.Queryable<AgUnifiedEntryRun>()
            .Where(value => value.ConversationId == conversationId &&
                            value.TenantId == tenantId && value.UserId == userId &&
                            !value.IsDeleted)
            .OrderBy(value => value.StartedAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
        return values.Select(MapEntryRun).ToArray();
    }
    #endregion

    #region 获取（GetDetailsForOwnerAsync）
    /// <summary>
    /// 获取（GetDetailsForOwnerAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的运行详情；不存在或所属身份不匹配时为 null。</returns>
    public async Task<UnifiedRunDetails?> GetDetailsForOwnerAsync(Guid runId, string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync();
        try
        {
            AgUnifiedEntryRun? entry = await Db.Queryable<AgUnifiedEntryRun>()
                .Where(value => value.ID == runId && value.TenantId == tenantId &&
                                value.UserId == userId && !value.IsDeleted)
                .FirstAsync();
            UnifiedRunDetails? details = entry is null ? null : await ReadDetailsAsync(entry);
            await Db.Ado.CommitTranAsync();
            return details;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 查询列表（ListEventsForOwnerAsync）
    /// <summary>
    /// 查询列表（ListEventsForOwnerAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>所属身份匹配的运行事件，按序号升序排列；运行不存在或不属于该用户时为空集合。</returns>
    public async Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Db.Queryable<AgUnifiedEntryRun>()
                .Where(value => value.ID == runId && value.TenantId == tenantId &&
                                value.UserId == userId && !value.IsDeleted)
                .AnyAsync())
        {
            return [];
        }

        return await ReadEventsAsync(runId);
    }
    #endregion

    #region 获取（GetAggregateForOwnerAsync）
    /// <summary>
    /// 获取（GetAggregateForOwnerAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含会话、消息、运行详情、事件及持久化版本的聚合快照；运行或会话不存在、或所属身份不匹配时为 null。</returns>
    public async Task<UnifiedEntryAggregate?> GetAggregateForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync();
        try
        {
            AgUnifiedEntryRun? entry = await Db.Queryable<AgUnifiedEntryRun>()
                .Where(value => value.ID == runId && value.TenantId == tenantId &&
                                value.UserId == userId && !value.IsDeleted)
                .FirstAsync();
            if (entry is null)
            {
                await Db.Ado.CommitTranAsync();
                return null;
            }

            AgChatConversation? conversation = await Db.Queryable<AgChatConversation>()
                .Where(value => value.ID == entry.ConversationId &&
                                value.TenantId == tenantId && value.UserId == userId &&
                                !value.IsDeleted)
                .FirstAsync();
            if (conversation is null)
            {
                await Db.Ado.CommitTranAsync();
                return null;
            }

            List<AgChatMessage> messages = await Db.Queryable<AgChatMessage>()
                .Where(value => value.ConversationId == conversation.ID && !value.IsDeleted)
                .OrderBy(value => value.Ordinal)
                .ToListAsync();
            UnifiedRunDetails details = await ReadDetailsAsync(entry);
            IReadOnlyList<UnifiedRunEventRecord> events = await ReadEventsAsync(runId);
            long revision = Required(entry.PersistenceRevision, "PersistenceRevision");
            await Db.Ado.CommitTranAsync();
            return new UnifiedEntryAggregate(
                MapConversation(conversation),
                messages.Select(MapMessage).ToArray(),
                details,
                events,
                revision);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 脱敏（RedactExpiredBusinessQueryResultsAsync）
    /// <summary>
    /// 脱敏（RedactExpiredBusinessQueryResultsAsync）
    /// </summary>
    /// <param name="cutoffUtc">筛选截止时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>本次脱敏的过期业务查询消息、关联工具结果和事件数量，以及清理截止时间。</returns>
    public async Task<BusinessQueryCleanupResult> RedactExpiredBusinessQueryResultsAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime cutoff = cutoffUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            List<AgChatMessage> expired = await Db.Queryable<AgChatMessage>()
                .Where(value => value.Kind == nameof(ConversationMessageKind.BusinessQueryResult) &&
                                value.BusinessQueryId.HasValue &&
                                value.BusinessPresentationJson != string.Empty &&
                                value.CreatedAtUtc < cutoff && !value.IsDeleted)
                .OrderBy(value => value.ID)
                .ToListAsync();
            int tools = 0;
            int events = 0;
            foreach (AgChatMessage value in expired)
            {
                Guid queryId = value.BusinessQueryId!.Value;
                string receipt = Required(value.BusinessReceiptJson, "BusinessReceiptJson");
                string integrity = Required(value.BusinessIntegritySha256, "BusinessIntegritySha256");
                string content = BusinessQueryResultRedaction.CreateContent(queryId, receipt, integrity);
                string payload = BusinessQueryResultRedaction.RedactedPayload(queryId, integrity);
                await Db.Updateable<AgChatMessage>()
                    .SetColumns(_ => new AgChatMessage
                    {
                        Content = content,
                        ContentUtf8Bytes = Encoding.UTF8.GetByteCount(content),
                        BusinessPresentationJson = string.Empty
                    })
                    .Where(item => item.ID == value.ID &&
                                   item.BusinessPresentationJson != string.Empty && !item.IsDeleted)
                    .ExecuteCommandAsync();
                string queryText = queryId.ToString("D").ToLowerInvariant();
                tools += await Db.Updateable<AgUnifiedToolCall>()
                    .SetColumns(_ => new AgUnifiedToolCall { ResultContent = payload })
                    .Where(item => item.ResultContent.ToLower().Contains(queryText) && !item.IsDeleted)
                    .ExecuteCommandAsync();
                events += await Db.Updateable<AgUnifiedRunEvent>()
                    .SetColumns(_ => new AgUnifiedRunEvent { PayloadJson = payload })
                    .Where(item => item.PayloadJson.ToLower().Contains(queryText) && !item.IsDeleted)
                    .ExecuteCommandAsync();
            }

            await Db.Ado.CommitTranAsync();
            return new BusinessQueryCleanupResult(expired.Count, tools, events, cutoffUtc);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 恢复（RecoverInterruptedAsync）
    /// <summary>
    /// 恢复（RecoverInterruptedAsync）
    /// </summary>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>本次将 Pending 或 Running 入口运行恢复为 Failed 的数量。</returns>
    public async Task<int> RecoverInterruptedAsync(DateTimeOffset recoveredAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime recovered = recoveredAtUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            List<AgUnifiedEntryRun> interrupted = await Db.Queryable<AgUnifiedEntryRun>()
                .Where(value => (value.Status == nameof(UnifiedRunStatus.Pending) ||
                                 value.Status == nameof(UnifiedRunStatus.Running)) &&
                                !value.IsDeleted)
                .OrderBy(value => value.StartedAtUtc)
                .OrderBy(value => value.ID)
                .ToListAsync();
            foreach (AgUnifiedEntryRun run in interrupted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTime started = Required(run.StartedAtUtc, "StartedAtUtc");
                long durationTicks = Math.Max(0, (recovered - started).Ticks);
                await RecoverChildrenAsync<AgUnifiedAgentRun>(run.ID, recovered);
                await RecoverChildrenAsync<AgUnifiedOrchestrationLink>(run.ID, recovered);
                await RecoverChildrenAsync<AgUnifiedToolCall>(run.ID, recovered);
                int updated = await Db.Updateable<AgUnifiedEntryRun>()
                    .SetColumns(value => new AgUnifiedEntryRun
                    {
                        Status = nameof(UnifiedRunStatus.Failed),
                        FinishedAtUtc = recovered,
                        DurationTicks = durationTicks,
                        ErrorCode = UnifiedEntryErrorCodes.HostInterrupted,
                        PersistenceRevision = value.PersistenceRevision + 1,
                        StateSha256 = string.Empty
                    })
                    .Where(value => value.ID == run.ID &&
                                    (value.Status == nameof(UnifiedRunStatus.Pending) ||
                                     value.Status == nameof(UnifiedRunStatus.Running)) &&
                                    !value.IsDeleted)
                    .ExecuteCommandAsync();
                if (updated != 1)
                {
                    throw ConcurrentWriteRejected();
                }

                await Db.Updateable<AgChatConversation>()
                    .SetColumns(_ => new AgChatConversation { UpdatedAtUtc = recovered })
                    .Where(value => value.ID == run.ConversationId && !value.IsDeleted)
                    .ExecuteCommandAsync();
                AgUnifiedRunEvent? latest = await Db.Queryable<AgUnifiedRunEvent>()
                    .Where(value => value.EntryRunId == run.ID && !value.IsDeleted)
                    .OrderBy(value => value.Sequence, OrderByType.Desc)
                    .FirstAsync();
                string payloadJson = JsonSerializer.Serialize(new
                {
                    errorCode = UnifiedEntryErrorCodes.HostInterrupted,
                    detail = "The Host restarted before this run reached a terminal state."
                });
                ProtectedUnifiedPayload payload =
                    UnifiedEntryPayloadProtector.ProtectInternal(payloadJson);
                await Db.Insertable(new AgUnifiedRunEvent
                {
                    ID = Guid.NewGuid(),
                    EntryRunId = run.ID,
                    Sequence = checked((latest?.Sequence ?? 0) + 1),
                    CorrelationId = run.CorrelationId,
                    Kind = "failed",
                    OccurredAtUtc = recovered,
                    ParentRunId = null,
                    Depth = 0,
                    PayloadJson = payload.Content,
                    PayloadSha256 = payload.OriginalSha256,
                    IsDeleted = false,
                    IsActive = true
                }).ExecuteCommandAsync();
            }

            await Db.Ado.CommitTranAsync();
            return interrupted.Count;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 保存（SaveAsync）
    /// <summary>
    /// 保存（SaveAsync）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行聚合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保存后的聚合副本及持久化版本；相同内容的重放可复用已有版本，过期版本冲突时抛出异常。</returns>
    public async Task<UnifiedEntryAggregate> SaveAsync(UnifiedEntryAggregate value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        UnifiedEntryAggregate snapshot = UnifiedEntryContractCloner.Clone(value);
        Validate(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        string fingerprint = UnifiedEntryAggregateFingerprint.ComputeSha256(snapshot);
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgUnifiedEntryRun? durable = await Db.Queryable<AgUnifiedEntryRun>()
                .Where(item => item.ID == snapshot.Details.EntryRun.Id && !item.IsDeleted)
                .FirstAsync();
            AgUnifiedRunEvent? latest = durable is null
                ? null
                : await Db.Queryable<AgUnifiedRunEvent>()
                    .Where(item => item.EntryRunId == durable.ID && !item.IsDeleted)
                    .OrderBy(item => item.Sequence, OrderByType.Desc)
                    .FirstAsync();
            if (durable is not null && durable.PersistenceRevision != snapshot.PersistenceRevision)
            {
                bool reconciled = snapshot.PersistenceRevision < long.MaxValue &&
                                  durable.PersistenceRevision == snapshot.PersistenceRevision + 1 &&
                                  StringComparer.Ordinal.Equals(durable.StateSha256, fingerprint);
                if (reconciled)
                {
                    await Db.Ado.CommitTranAsync();
                    return snapshot.WithPersistenceRevision(durable.PersistenceRevision!.Value);
                }

                throw ConcurrentWriteRejected();
            }

            if (durable is null && snapshot.PersistenceRevision != 0)
            {
                throw ConcurrentWriteRejected();
            }

            IReadOnlyList<UnifiedRunEventRecord> eventsToAppend = SelectEventsToAppend(
                snapshot.Events,
                latest?.Sequence ?? 0,
                latest?.ID,
                latest?.Kind,
                latest?.PayloadSha256);
            long nextRevision = checked(snapshot.PersistenceRevision + 1);
            await UpsertConversationAsync(snapshot.Conversation);
            await PersistMessagesAsync(snapshot.Conversation.Id, snapshot.Messages);
            await UpsertEntryRunAsync(
                snapshot.Details.EntryRun,
                durable is not null,
                snapshot.PersistenceRevision,
                nextRevision,
                fingerprint);
            await Db.Deleteable<AgUnifiedToolCall>()
                .Where(item => item.EntryRunId == snapshot.Details.EntryRun.Id)
                .ExecuteCommandAsync();
            await Db.Deleteable<AgUnifiedOrchestrationLink>()
                .Where(item => item.EntryRunId == snapshot.Details.EntryRun.Id)
                .ExecuteCommandAsync();
            await Db.Deleteable<AgUnifiedAgentRun>()
                .Where(item => item.EntryRunId == snapshot.Details.EntryRun.Id)
                .ExecuteCommandAsync();
            if (snapshot.Details.AgentRuns.Count > 0)
            {
                await Db.Insertable(snapshot.Details.AgentRuns
                    .Select((item, ordinal) => MapAgentRun(item, ordinal)).ToArray())
                    .ExecuteCommandAsync();
            }
            if (snapshot.Details.Orchestrations.Count > 0)
            {
                await Db.Insertable(snapshot.Details.Orchestrations
                    .Select((item, ordinal) => MapOrchestration(item, ordinal)).ToArray())
                    .ExecuteCommandAsync();
            }
            if (snapshot.Details.ToolCalls.Count > 0)
            {
                await Db.Insertable(snapshot.Details.ToolCalls
                    .Select((item, ordinal) => MapToolCall(item, ordinal)).ToArray())
                    .ExecuteCommandAsync();
            }
            if (eventsToAppend.Count > 0)
            {
                await Db.Insertable(eventsToAppend.Select(MapEvent).ToArray())
                    .ExecuteCommandAsync();
            }

            await Db.Ado.CommitTranAsync();
            return snapshot.WithPersistenceRevision(nextRevision);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 读取（ReadMessagesAsync）
    /// <summary>
    /// 读取（ReadMessagesAsync）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <returns>会话最近的消息，按消息序号升序返回，数量限定在读取上限内。</returns>
    private async Task<IReadOnlyList<ConversationMessageRecord>> ReadMessagesAsync(Guid conversationId, int take)
    {
        List<AgChatMessage> values = await Db.Queryable<AgChatMessage>()
            .Where(value => value.ConversationId == conversationId && !value.IsDeleted)
            .OrderBy(value => value.Ordinal, OrderByType.Desc)
            .Take(Math.Clamp(take, 1, UnifiedEntryReadLimits.MaximumMessageTake))
            .ToListAsync();
        return values.OrderBy(value => value.Ordinal).Select(MapMessage).ToArray();
    }
    #endregion

    #region 读取（ReadDetailsAsync）
    /// <summary>
    /// 读取（ReadDetailsAsync）
    /// </summary>
    /// <param name="entry">当前处理的入口记录或条目。</param>
    /// <returns>从入口运行及关联的 Agent、编排、工具调用实体组装的运行详情。</returns>
    private async Task<UnifiedRunDetails> ReadDetailsAsync(AgUnifiedEntryRun entry)
    {
        List<AgUnifiedAgentRun> agents = await Db.Queryable<AgUnifiedAgentRun>()
            .Where(value => value.EntryRunId == entry.ID && !value.IsDeleted)
            .OrderBy(value => value.Ordinal)
            .ToListAsync();
        List<AgUnifiedOrchestrationLink> orchestrations = await Db
            .Queryable<AgUnifiedOrchestrationLink>()
            .Where(value => value.EntryRunId == entry.ID && !value.IsDeleted)
            .OrderBy(value => value.Ordinal)
            .ToListAsync();
        List<AgUnifiedToolCall> tools = await Db.Queryable<AgUnifiedToolCall>()
            .Where(value => value.EntryRunId == entry.ID && !value.IsDeleted)
            .OrderBy(value => value.Ordinal)
            .ToListAsync();
        return new UnifiedRunDetails(
            MapEntryRun(entry),
            agents.Select(MapAgentRun).ToArray(),
            orchestrations.Select(MapOrchestration).ToArray(),
            tools.Select(MapToolCall).ToArray());
    }
    #endregion

    #region 读取（ReadEventsAsync）
    /// <summary>
    /// 读取（ReadEventsAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <returns>指定运行的未删除事件，按事件序号升序排列。</returns>
    private async Task<IReadOnlyList<UnifiedRunEventRecord>> ReadEventsAsync(Guid runId)
    {
        List<AgUnifiedRunEvent> values = await Db.Queryable<AgUnifiedRunEvent>()
            .Where(value => value.EntryRunId == runId && !value.IsDeleted)
            .OrderBy(value => value.Sequence)
            .ToListAsync();
        return values.Select(MapEvent).ToArray();
    }
    #endregion

    #region 恢复（RecoverChildrenAsync）
    /// <summary>
    /// 恢复（RecoverChildrenAsync）
    /// </summary>
    /// <typeparam name="TEntity">待处理数据的泛型类型。</typeparam>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="recovered">恢复后的数据。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task RecoverChildrenAsync<TEntity>(Guid runId, DateTime recovered)
        where TEntity : BasePoco, new()
    {
        if (typeof(TEntity) == typeof(AgUnifiedAgentRun))
        {
            List<AgUnifiedAgentRun> values = await Db.Queryable<AgUnifiedAgentRun>()
                .Where(value => value.EntryRunId == runId &&
                                (value.Status == nameof(UnifiedRunStatus.Pending) ||
                                 value.Status == nameof(UnifiedRunStatus.Running)) && !value.IsDeleted)
                .ToListAsync();
            foreach (AgUnifiedAgentRun value in values)
            {
                await Db.Updateable<AgUnifiedAgentRun>()
                    .SetColumns(_ => new AgUnifiedAgentRun
                    {
                        Status = nameof(UnifiedRunStatus.Failed), FinishedAtUtc = recovered,
                        DurationTicks = Math.Max(0, (recovered - value.StartedAtUtc!.Value).Ticks),
                        ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
                    }).Where(item => item.ID == value.ID && !item.IsDeleted).ExecuteCommandAsync();
            }
            return;
        }
        if (typeof(TEntity) == typeof(AgUnifiedOrchestrationLink))
        {
            List<AgUnifiedOrchestrationLink> values = await Db.Queryable<AgUnifiedOrchestrationLink>()
                .Where(value => value.EntryRunId == runId &&
                                (value.Status == nameof(UnifiedRunStatus.Pending) || value.Status == nameof(UnifiedRunStatus.Running)) && !value.IsDeleted)
                .ToListAsync();
            foreach (AgUnifiedOrchestrationLink value in values)
            {
                await Db.Updateable<AgUnifiedOrchestrationLink>()
                    .SetColumns(_ => new AgUnifiedOrchestrationLink
                    {
                        Status = nameof(UnifiedRunStatus.Failed), FinishedAtUtc = recovered,
                        DurationTicks = Math.Max(0, (recovered - value.StartedAtUtc!.Value).Ticks),
                        ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
                    }).Where(item => item.ID == value.ID && !item.IsDeleted).ExecuteCommandAsync();
            }
            return;
        }

        List<AgUnifiedToolCall> tools = await Db.Queryable<AgUnifiedToolCall>()
            .Where(value => value.EntryRunId == runId &&
                            (value.Status == nameof(UnifiedRunStatus.Pending) || value.Status == nameof(UnifiedRunStatus.Running)) && !value.IsDeleted)
            .ToListAsync();
        foreach (AgUnifiedToolCall value in tools)
        {
            await Db.Updateable<AgUnifiedToolCall>()
                .SetColumns(_ => new AgUnifiedToolCall
                {
                    Status = nameof(UnifiedRunStatus.Failed), FinishedAtUtc = recovered,
                    DurationTicks = Math.Max(0, (recovered - value.StartedAtUtc!.Value).Ticks),
                    ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
                }).Where(item => item.ID == value.ID && !item.IsDeleted).ExecuteCommandAsync();
        }
    }
    #endregion

    #region 处理（UpsertConversationAsync）
    /// <summary>
    /// 处理（UpsertConversationAsync）
    /// </summary>
    /// <param name="value">本次操作使用的会话记录。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task UpsertConversationAsync(ConversationRecord value)
    {
        AgChatConversation? existing = await Db.Queryable<AgChatConversation>()
            .Where(item => item.ID == value.Id && !item.IsDeleted)
            .FirstAsync();
        if (existing is null)
        {
            await Db.Insertable(MapConversation(value)).ExecuteCommandAsync();
            return;
        }
        if (existing.CreatedAtUtc is not DateTime existingCreatedAt ||
            !SamePersistedInstant(existingCreatedAt, value.CreatedAtUtc.UtcDateTime) ||
            !StringComparer.Ordinal.Equals(existing.TenantId, value.TenantId) ||
            !StringComparer.Ordinal.Equals(existing.UserId, value.UserId))
        {
            throw new InvalidOperationException(
                "The conversation identity conflicts with persisted data.");
        }
        if (value.UpdatedAtUtc.UtcDateTime >= existing.UpdatedAtUtc)
        {
            await Db.Updateable<AgChatConversation>()
                .SetColumns(_ => new AgChatConversation
                {
                    Title = value.Title,
                    UpdatedAtUtc = value.UpdatedAtUtc.UtcDateTime
                })
                .Where(item => item.ID == value.Id && item.CreatedAtUtc == existingCreatedAt &&
                               item.TenantId == value.TenantId && item.UserId == value.UserId && !item.IsDeleted)
                .ExecuteCommandAsync();
        }
    }
    #endregion

    #region 处理（PersistMessagesAsync）
    /// <summary>
    /// 处理（PersistMessagesAsync）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="values">按会话消息顺序提交的完整消息集合，用于校验旧消息并追加新消息。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task PersistMessagesAsync(Guid conversationId, IReadOnlyList<ConversationMessageRecord> values)
    {
        List<AgChatMessage> rows = await Db.Queryable<AgChatMessage>()
            .Where(item => item.ConversationId == conversationId && !item.IsDeleted)
            .OrderBy(item => item.Ordinal)
            .ToListAsync();
        Dictionary<Guid, (ConversationMessageRecord Value, long Ordinal)> persisted = rows
            .ToDictionary(item => item.ID, item => (MapMessage(item), Required(item.Ordinal, "Ordinal")));
        long nextOrdinal = rows.Count == 0 ? 0 : checked(rows.Max(item => item.Ordinal!.Value) + 1);
        long previousOrdinal = -1;
        foreach (ConversationMessageRecord value in values)
        {
            if (persisted.TryGetValue(value.Id, out var existing))
            {
                if (!SamePersistedMessage(existing.Value, value) || existing.Ordinal <= previousOrdinal)
                {
                    throw new InvalidOperationException(
                        $"The conversation message identity or supplied ordering conflicts with persisted data. " +
                        $"Conflicting fields: {DescribeMessageConflict(existing.Value, value, existing.Ordinal, previousOrdinal)}.");
                }
                previousOrdinal = existing.Ordinal;
                continue;
            }
            long ordinal = nextOrdinal++;
            if (ordinal <= previousOrdinal)
            {
                throw new InvalidOperationException(
                    "The supplied conversation message order cannot be appended safely.");
            }
            await Db.Insertable(MapMessage(value, ordinal)).ExecuteCommandAsync();
            previousOrdinal = ordinal;
        }
    }
    #endregion

    #region 处理（DescribeMessageConflict）
    /// <summary>
    /// 处理（DescribeMessageConflict）
    /// </summary>
    /// <param name="persisted">已持久化的数据。</param>
    /// <param name="supplied">调用方提供的数据。</param>
    /// <param name="persistedOrdinal">已持久化消息的排序序号。</param>
    /// <param name="previousOrdinal">前一条消息的排序序号，用于检查消息顺序。</param>
    /// <returns>以逗号分隔的冲突字段名；未识别出具体差异时为 unknown。</returns>
    private static string DescribeMessageConflict(
        ConversationMessageRecord persisted,
        ConversationMessageRecord supplied,
        long persistedOrdinal,
        long previousOrdinal)
    {
        var fields = new List<string>();
        if (persisted.Id != supplied.Id) fields.Add(nameof(ConversationMessageRecord.Id));
        if (persisted.ConversationId != supplied.ConversationId) fields.Add(nameof(ConversationMessageRecord.ConversationId));
        if (persisted.Role != supplied.Role) fields.Add(nameof(ConversationMessageRecord.Role));
        if (!StringComparer.Ordinal.Equals(persisted.Content, supplied.Content)) fields.Add(nameof(ConversationMessageRecord.Content));
        if (!StringComparer.Ordinal.Equals(persisted.ContentSha256, supplied.ContentSha256)) fields.Add(nameof(ConversationMessageRecord.ContentSha256));
        if (persisted.ContentUtf8Bytes != supplied.ContentUtf8Bytes) fields.Add(nameof(ConversationMessageRecord.ContentUtf8Bytes));
        if (!SamePersistedInstant(persisted.CreatedAtUtc.UtcDateTime, supplied.CreatedAtUtc.UtcDateTime)) fields.Add(nameof(ConversationMessageRecord.CreatedAtUtc));
        if (persisted.Kind != supplied.Kind) fields.Add(nameof(ConversationMessageRecord.Kind));
        if (persisted.BusinessQueryId != supplied.BusinessQueryId) fields.Add(nameof(ConversationMessageRecord.BusinessQueryId));
        if (!StringComparer.Ordinal.Equals(persisted.BusinessQueryReceiptJson, supplied.BusinessQueryReceiptJson)) fields.Add(nameof(ConversationMessageRecord.BusinessQueryReceiptJson));
        if (!StringComparer.Ordinal.Equals(persisted.BusinessQueryPresentationJson, supplied.BusinessQueryPresentationJson)) fields.Add(nameof(ConversationMessageRecord.BusinessQueryPresentationJson));
        if (!StringComparer.Ordinal.Equals(persisted.BusinessQueryIntegritySha256, supplied.BusinessQueryIntegritySha256)) fields.Add(nameof(ConversationMessageRecord.BusinessQueryIntegritySha256));
        if (persistedOrdinal <= previousOrdinal) fields.Add("Ordinal");
        return fields.Count == 0 ? "unknown" : string.Join(", ", fields);
    }
    #endregion

    #region 核对消息内容和业务查询证据是否一致（SamePersistedMessage）
    /// <summary>
    /// 核对消息内容和业务查询证据是否一致（SamePersistedMessage）。
    /// </summary>
    /// <param name="persisted">数据库中已保存的消息。</param>
    /// <param name="supplied">调用方提供的待核对消息。</param>
    /// <returns>消息标识、会话、角色、内容及摘要、字节数、类型和业务查询证据一致，且创建时间处于允许误差内时返回 true，否则返回 false。</returns>
    private static bool SamePersistedMessage(ConversationMessageRecord persisted, ConversationMessageRecord supplied) =>
        persisted.Id == supplied.Id &&
        persisted.ConversationId == supplied.ConversationId &&
        persisted.Role == supplied.Role &&
        StringComparer.Ordinal.Equals(persisted.Content, supplied.Content) &&
        StringComparer.Ordinal.Equals(persisted.ContentSha256, supplied.ContentSha256) &&
        persisted.ContentUtf8Bytes == supplied.ContentUtf8Bytes &&
        SamePersistedInstant(persisted.CreatedAtUtc.UtcDateTime, supplied.CreatedAtUtc.UtcDateTime) &&
        persisted.Kind == supplied.Kind &&
        persisted.BusinessQueryId == supplied.BusinessQueryId &&
        StringComparer.Ordinal.Equals(persisted.BusinessQueryReceiptJson, supplied.BusinessQueryReceiptJson) &&
        StringComparer.Ordinal.Equals(persisted.BusinessQueryPresentationJson, supplied.BusinessQueryPresentationJson) &&
        StringComparer.Ordinal.Equals(persisted.BusinessQueryIntegritySha256, supplied.BusinessQueryIntegritySha256);
    #endregion

    #region 按持久化时间精度比较时刻（SamePersistedInstant）
    /// <summary>
    /// 按持久化时间精度比较时刻（SamePersistedInstant）。
    /// </summary>
    /// <param name="left">第一个 UTC 时间值。</param>
    /// <param name="right">第二个 UTC 时间值。</param>
    /// <returns>两时间值相差不超过 4 毫秒时返回 true，否则返回 false；用于容忍数据库时间参数舍入误差。</returns>
    private static bool SamePersistedInstant(DateTime left, DateTime right) =>
        // SqlSugar's SQL Server DateTime parameter binding can round a value by
        // up to one legacy DATETIME increment even when the column is DATETIME2(7).
        Math.Abs((left - right).Ticks) <= 4 * TimeSpan.TicksPerMillisecond;
    #endregion

    #region 处理（UpsertEntryRunAsync）
    /// <summary>
    /// 处理（UpsertEntryRunAsync）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行记录。</param>
    /// <param name="rowExists">目标记录是否已存在。</param>
    /// <param name="expectedRevision">并发更新要求匹配的修订号。</param>
    /// <param name="nextRevision">更新后的修订号。</param>
    /// <param name="fingerprint">用于一致性比较的指纹。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task UpsertEntryRunAsync(UnifiedEntryRunRecord value, bool rowExists, long expectedRevision, long nextRevision, string fingerprint)
    {
        if (!rowExists)
        {
            await Db.Insertable(MapEntryRun(value, nextRevision, fingerprint)).ExecuteCommandAsync();
            return;
        }
        AgUnifiedEntryRun replacement = MapEntryRun(value, nextRevision, fingerprint);
        int updated = await Db.Updateable<AgUnifiedEntryRun>()
            .SetColumns(_ => new AgUnifiedEntryRun
            {
                ConversationId = replacement.ConversationId,
                CorrelationId = replacement.CorrelationId,
                MainAgentVersionId = replacement.MainAgentVersionId,
                Status = replacement.Status,
                StartedAtUtc = replacement.StartedAtUtc,
                FinishedAtUtc = replacement.FinishedAtUtc,
                DurationTicks = replacement.DurationTicks,
                InputText = replacement.InputText,
                InputSha256 = replacement.InputSha256,
                OutputText = replacement.OutputText,
                OutputSha256 = replacement.OutputSha256,
                ErrorCode = replacement.ErrorCode,
                TenantId = replacement.TenantId,
                UserId = replacement.UserId,
                PersistenceRevision = nextRevision,
                StateSha256 = fingerprint
            })
            .Where(item => item.ID == value.Id && item.PersistenceRevision == expectedRevision &&
                           item.TenantId == value.TenantId && item.UserId == value.UserId &&
                           !item.IsDeleted)
            .ExecuteCommandAsync();
        if (updated != 1)
        {
            throw ConcurrentWriteRejected();
        }
    }
    #endregion

    #region 处理（SelectEventsToAppend）
    /// <summary>
    /// 处理（SelectEventsToAppend）
    /// </summary>
    /// <param name="events">事件集合。</param>
    /// <param name="lastEventSequence">最近事件序号。</param>
    /// <param name="lastEventId">最近事件标识。</param>
    /// <param name="lastEventKind">最近事件类型。</param>
    /// <param name="lastEventPayloadSha256">最近事件载荷的 SHA-256 摘要。</param>
    /// <returns>校验已持久化事件边界后仍需追加的事件；边界不一致时抛出并发写入异常。</returns>
    internal static IReadOnlyList<UnifiedRunEventRecord> SelectEventsToAppend(
        IReadOnlyList<UnifiedRunEventRecord> events,
        long lastEventSequence,
        Guid? lastEventId,
        string? lastEventKind,
        string? lastEventPayloadSha256)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (lastEventSequence == 0)
        {
            if (lastEventId is not null || lastEventKind is not null ||
                lastEventPayloadSha256 is not null)
            {
                throw ConcurrentWriteRejected();
            }
            return events.ToArray();
        }
        if (lastEventSequence < 0 || lastEventSequence > events.Count ||
            lastEventId is not Guid persistedEventId)
        {
            throw ConcurrentWriteRejected();
        }
        UnifiedRunEventRecord expected = events[checked((int)lastEventSequence - 1)];
        if (expected.Sequence != lastEventSequence || expected.Id != persistedEventId ||
            !StringComparer.Ordinal.Equals(expected.Kind, lastEventKind) ||
            !StringComparer.Ordinal.Equals(expected.PayloadSha256, lastEventPayloadSha256))
        {
            throw ConcurrentWriteRejected();
        }
        return events.Skip(checked((int)lastEventSequence)).ToArray();
    }
    #endregion

    #region 校验（Validate）
    /// <summary>
    /// 校验（Validate）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行聚合。</param>
    private static void Validate(UnifiedEntryAggregate value)
    {
        Guid runId = value.Details.EntryRun.Id;
        Guid conversationId = value.Conversation.Id;
        Guid correlationId = value.Details.EntryRun.CorrelationId;
        bool invalid = conversationId == Guid.Empty || runId == Guid.Empty ||
            correlationId == Guid.Empty || value.Details.EntryRun.ConversationId != conversationId ||
            HasDuplicateIds(value.Messages.Select(item => item.Id)) ||
            value.Messages.Any(item => item.Id == Guid.Empty || item.ConversationId != conversationId || item.ContentUtf8Bytes < 0) ||
            HasDuplicateIds(value.Details.AgentRuns.Select(item => item.Id)) ||
            value.Details.AgentRuns.Any(item => item.Id == Guid.Empty || item.EntryRunId != runId || item.Depth < 0) ||
            HasDuplicateIds(value.Details.Orchestrations.Select(item => item.Id)) ||
            value.Details.Orchestrations.Any(item => item.Id == Guid.Empty || item.EntryRunId != runId || item.Depth < 0) ||
            HasDuplicateIds(value.Details.ToolCalls.Select(item => item.Id)) ||
            value.Details.ToolCalls.Any(item => item.Id == Guid.Empty || item.EntryRunId != runId || item.Depth < 0) ||
            HasDuplicateIds(value.Events.Select(item => item.Id)) ||
            value.Events.Any(item => item.Id == Guid.Empty || item.EntryRunId != runId ||
                                     item.CorrelationId != correlationId || item.Depth < 0) ||
            !value.Events.Select(item => item.Sequence)
                .SequenceEqual(Enumerable.Range(1, value.Events.Count).Select(index => (long)index));
        if (invalid)
        {
            throw new ArgumentException(
                "The unified entry aggregate contains invalid or mismatched identities.", nameof(value));
        }
        int terminalEventCount = value.Events.Count(item =>
            item.Kind is "completed" or "failed" or "cancelled");
        if (terminalEventCount > 1 || IsTerminal(value.Details.EntryRun.Status) && terminalEventCount != 1 ||
            !IsTerminal(value.Details.EntryRun.Status) && terminalEventCount != 0)
        {
            throw new ArgumentException(
                "The unified entry aggregate contains an invalid terminal event history.", nameof(value));
        }
    }
    #endregion

    #region 检查标识集合是否有重复项（HasDuplicateIds）
    /// <summary>
    /// 检查标识集合是否有重复项（HasDuplicateIds）。
    /// </summary>
    /// <param name="ids">待枚举并检查重复项的 GUID 集合。</param>
    /// <returns>集合中存在重复 GUID 时返回 true，否则返回 false；空集合返回 false。</returns>
    private static bool HasDuplicateIds(IEnumerable<Guid> ids)
    {
        Guid[] values = ids.ToArray();
        return values.Distinct().Count() != values.Length;
    }
    #endregion

    #region 判断会话运行是否结束或被阻断（IsTerminal）
    /// <summary>
    /// 判断会话运行是否结束或被阻断（IsTerminal）。
    /// </summary>
    /// <param name="status">待检查的会话运行状态。</param>
    /// <returns>状态为 Completed、Failed、Cancelled 或 Blocked 时返回 true，否则返回 false。</returns>
    private static bool IsTerminal(UnifiedRunStatus status) => status is
        UnifiedRunStatus.Completed or UnifiedRunStatus.Failed or UnifiedRunStatus.Cancelled or UnifiedRunStatus.Blocked;
    #endregion

    #region 处理（ConcurrentWriteRejected）
    /// <summary>
    /// 处理（ConcurrentWriteRejected）
    /// </summary>
    /// <returns>表示统一入口聚合持久化版本过期的异常。</returns>
    private static InvalidOperationException ConcurrentWriteRejected() =>
        new("The unified entry aggregate revision is stale.");
    #endregion

    #region 映射（MapConversation）
    /// <summary>
    /// 映射（MapConversation）
    /// </summary>
    /// <param name="value">本次操作使用的会话实体。</param>
    /// <returns>从会话实体还原的会话记录及所属身份。</returns>
    private static ConversationRecord MapConversation(AgChatConversation value) => new(
        value.ID, Required(value.Title, "Title"), ToOffset(Required(value.CreatedAtUtc, "CreatedAtUtc")),
        ToOffset(Required(value.UpdatedAtUtc, "UpdatedAtUtc")))
    { TenantId = Required(value.TenantId, "TenantId"), UserId = Required(value.UserId, "UserId") };
    #endregion

    #region 映射（MapConversation）
    /// <summary>
    /// 映射（MapConversation）
    /// </summary>
    /// <param name="value">本次操作使用的会话记录。</param>
    /// <returns>由会话记录构造的持久化实体。</returns>
    private static AgChatConversation MapConversation(ConversationRecord value) => new()
    {
        ID = value.Id, Title = value.Title, CreatedAtUtc = value.CreatedAtUtc.UtcDateTime,
        UpdatedAtUtc = value.UpdatedAtUtc.UtcDateTime, TenantId = value.TenantId,
        UserId = value.UserId, IsDeleted = false, IsActive = true
    };
    #endregion

    #region 映射（MapMessage）
    /// <summary>
    /// 映射（MapMessage）
    /// </summary>
    /// <param name="value">本次操作使用的消息实体。</param>
    /// <returns>从消息实体还原的会话消息，包含业务查询凭据和展示信息。</returns>
    private static ConversationMessageRecord MapMessage(AgChatMessage value) => new(
        value.ID, Required(value.ConversationId, "ConversationId"),
        ParseEnum<ConversationMessageRole>(Required(value.Role, "Role")),
        Required(value.Content, "Content"), Required(value.ContentSha256, "ContentSha256"),
        checked((int)Required(value.ContentUtf8Bytes, "ContentUtf8Bytes")),
        ToOffset(Required(value.CreatedAtUtc, "CreatedAtUtc")))
    {
        Kind = ParseEnum<ConversationMessageKind>(Required(value.Kind, "Kind")),
        BusinessQueryId = value.BusinessQueryId,
        BusinessQueryReceiptJson = Required(value.BusinessReceiptJson, "BusinessReceiptJson"),
        BusinessQueryPresentationJson = Required(value.BusinessPresentationJson, "BusinessPresentationJson"),
        BusinessQueryIntegritySha256 = Required(value.BusinessIntegritySha256, "BusinessIntegritySha256")
    };
    #endregion

    #region 映射（MapMessage）
    /// <summary>
    /// 映射（MapMessage）
    /// </summary>
    /// <param name="value">本次操作使用的会话消息记录。</param>
    /// <param name="ordinal">记录在所属聚合中的排序序号。</param>
    /// <returns>带有消息序号及业务查询信息的会话消息持久化实体。</returns>
    private static AgChatMessage MapMessage(ConversationMessageRecord value, long ordinal) => new()
    {
        ID = value.Id, ConversationId = value.ConversationId, Ordinal = ordinal,
        Role = value.Role.ToString(), Content = value.Content, ContentSha256 = value.ContentSha256,
        ContentUtf8Bytes = value.ContentUtf8Bytes, CreatedAtUtc = value.CreatedAtUtc.UtcDateTime,
        Kind = value.Kind.ToString(), BusinessQueryId = value.BusinessQueryId,
        BusinessReceiptJson = value.BusinessQueryReceiptJson,
        BusinessPresentationJson = value.BusinessQueryPresentationJson,
        BusinessIntegritySha256 = value.BusinessQueryIntegritySha256,
        IsDeleted = false, IsActive = true
    };
    #endregion

    #region 映射（MapEntryRun）
    /// <summary>
    /// 映射（MapEntryRun）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行实体。</param>
    /// <returns>从入口运行实体还原的运行记录及所属身份。</returns>
    private static UnifiedEntryRunRecord MapEntryRun(AgUnifiedEntryRun value) => new(
        value.ID, Required(value.ConversationId, "ConversationId"), Required(value.CorrelationId, "CorrelationId"),
        Required(value.MainAgentVersionId, "MainAgentVersionId"), ParseEnum<UnifiedRunStatus>(Required(value.Status, "Status")),
        ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")), ToNullableOffset(value.FinishedAtUtc),
        value.DurationTicks.HasValue ? TimeSpan.FromTicks(value.DurationTicks.Value) : null,
        Required(value.InputText, "InputText"), Required(value.InputSha256, "InputSha256"),
        Required(value.OutputText, "OutputText"), Required(value.OutputSha256, "OutputSha256"),
        Required(value.ErrorCode, "ErrorCode"))
    { TenantId = Required(value.TenantId, "TenantId"), UserId = Required(value.UserId, "UserId") };
    #endregion

    #region 映射（MapEntryRun）
    /// <summary>
    /// 映射（MapEntryRun）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行记录。</param>
    /// <param name="revision">修订号。</param>
    /// <param name="fingerprint">用于一致性比较的指纹。</param>
    /// <returns>带有持久化版本和状态指纹的入口运行实体。</returns>
    private static AgUnifiedEntryRun MapEntryRun(UnifiedEntryRunRecord value, long revision, string fingerprint) => new()
    {
        ID = value.Id, ConversationId = value.ConversationId, CorrelationId = value.CorrelationId,
        MainAgentVersionId = value.MainAgentVersionId, Status = value.Status.ToString(),
        StartedAtUtc = value.StartedAtUtc.UtcDateTime, FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        DurationTicks = value.Duration?.Ticks, InputText = value.Input, InputSha256 = value.InputSha256,
        OutputText = value.Output, OutputSha256 = value.OutputSha256, ErrorCode = value.ErrorCode,
        PersistenceRevision = revision, StateSha256 = fingerprint, TenantId = value.TenantId,
        UserId = value.UserId, IsDeleted = false, IsActive = true
    };
    #endregion

    #region 映射（MapAgentRun）
    /// <summary>
    /// 映射（MapAgentRun）
    /// </summary>
    /// <param name="value">本次操作使用的Agent 子运行实体。</param>
    /// <returns>从 Agent 运行实体还原的统一入口子运行记录。</returns>
    private static UnifiedAgentRunRecord MapAgentRun(AgUnifiedAgentRun value) => new(
        value.ID, Required(value.EntryRunId, "EntryRunId"), value.ParentRunId,
        ParseEnum<UnifiedAgentRunKind>(Required(value.Kind, "Kind")), Required(value.AgentId, "AgentId"),
        Required(value.AgentVersionId, "AgentVersionId"), Required(value.Depth, "Depth"),
        ParseEnum<UnifiedRunStatus>(Required(value.Status, "Status")), ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")),
        ToNullableOffset(value.FinishedAtUtc), ToDuration(value.DurationTicks), Required(value.InputText, "InputText"),
        Required(value.InputSha256, "InputSha256"), Required(value.OutputText, "OutputText"),
        Required(value.OutputSha256, "OutputSha256"), Required(value.ErrorCode, "ErrorCode"));
    #endregion

    #region 映射（MapAgentRun）
    /// <summary>
    /// 映射（MapAgentRun）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口 Agent 子运行记录。</param>
    /// <param name="ordinal">记录在所属聚合中的排序序号。</param>
    /// <returns>带有聚合内排序序号的 Agent 运行实体。</returns>
    private static AgUnifiedAgentRun MapAgentRun(UnifiedAgentRunRecord value, int ordinal) => new()
    {
        ID = value.Id, EntryRunId = value.EntryRunId, Ordinal = ordinal, ParentRunId = value.ParentRunId,
        Kind = value.Kind.ToString(), AgentId = value.AgentId, AgentVersionId = value.AgentVersionId,
        Depth = value.Depth, Status = value.Status.ToString(), StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime, DurationTicks = value.Duration?.Ticks,
        InputText = value.Input, InputSha256 = value.InputSha256, OutputText = value.Output,
        OutputSha256 = value.OutputSha256, ErrorCode = value.ErrorCode, IsDeleted = false, IsActive = true
    };
    #endregion

    #region 映射（MapOrchestration）
    /// <summary>
    /// 映射（MapOrchestration）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行关联实体。</param>
    /// <returns>从编排关联实体还原的统一入口编排运行关联记录。</returns>
    private static UnifiedOrchestrationRunLink MapOrchestration(AgUnifiedOrchestrationLink value) => new(
        value.ID, Required(value.EntryRunId, "EntryRunId"), Required(value.ParentRunId, "ParentRunId"),
        Required(value.OrchestrationRunId, "OrchestrationRunId"), Required(value.OrchestrationVersionId, "OrchestrationVersionId"),
        Required(value.Depth, "Depth"), ParseEnum<UnifiedRunStatus>(Required(value.Status, "Status")),
        ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")), ToNullableOffset(value.FinishedAtUtc),
        ToDuration(value.DurationTicks), Required(value.InputText, "InputText"), Required(value.InputSha256, "InputSha256"),
        Required(value.OutputText, "OutputText"), Required(value.OutputSha256, "OutputSha256"), Required(value.ErrorCode, "ErrorCode"));
    #endregion

    #region 映射（MapOrchestration）
    /// <summary>
    /// 映射（MapOrchestration）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行关联记录。</param>
    /// <param name="ordinal">记录在所属聚合中的排序序号。</param>
    /// <returns>带有聚合内排序序号的编排运行关联实体。</returns>
    private static AgUnifiedOrchestrationLink MapOrchestration(UnifiedOrchestrationRunLink value, int ordinal) => new()
    {
        ID = value.Id, EntryRunId = value.EntryRunId, Ordinal = ordinal, ParentRunId = value.ParentRunId,
        OrchestrationRunId = value.OrchestrationRunId, OrchestrationVersionId = value.OrchestrationVersionId,
        Depth = value.Depth, Status = value.Status.ToString(), StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime, DurationTicks = value.Duration?.Ticks,
        InputText = value.Input, InputSha256 = value.InputSha256, OutputText = value.Output,
        OutputSha256 = value.OutputSha256, ErrorCode = value.ErrorCode, IsDeleted = false, IsActive = true
    };
    #endregion

    #region 映射（MapToolCall）
    /// <summary>
    /// 映射（MapToolCall）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口工具调用实体。</param>
    /// <returns>从工具调用实体还原的统一入口工具调用记录。</returns>
    private static UnifiedToolCallRecord MapToolCall(AgUnifiedToolCall value) => new(
        value.ID, Required(value.EntryRunId, "EntryRunId"), Required(value.ParentRunId, "ParentRunId"),
        Required(value.ToolVersionId, "ToolVersionId"), Required(value.Depth, "Depth"),
        ParseEnum<UnifiedRunStatus>(Required(value.Status, "Status")), ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")),
        ToNullableOffset(value.FinishedAtUtc), ToDuration(value.DurationTicks), Required(value.ArgumentsJson, "ArgumentsJson"),
        Required(value.ArgumentsSha256, "ArgumentsSha256"), Required(value.ResultContent, "ResultContent"),
        Required(value.ResultSha256, "ResultSha256"), Required(value.ErrorCode, "ErrorCode"));
    #endregion

    #region 映射（MapToolCall）
    /// <summary>
    /// 映射（MapToolCall）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口工具调用记录。</param>
    /// <param name="ordinal">工具调用在审计记录中的排序序号。</param>
    /// <returns>带有聚合内排序序号的工具调用实体。</returns>
    private static AgUnifiedToolCall MapToolCall(UnifiedToolCallRecord value, int ordinal) => new()
    {
        ID = value.Id, EntryRunId = value.EntryRunId, Ordinal = ordinal, ParentRunId = value.ParentRunId,
        ToolVersionId = value.ToolVersionId, Depth = value.Depth, Status = value.Status.ToString(),
        StartedAtUtc = value.StartedAtUtc.UtcDateTime, FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        DurationTicks = value.Duration?.Ticks, ArgumentsJson = value.ArgumentsJson,
        ArgumentsSha256 = value.ArgumentsSha256, ResultContent = value.ResultContent,
        ResultSha256 = value.ResultSha256, ErrorCode = value.ErrorCode, IsDeleted = false, IsActive = true
    };
    #endregion

    #region 映射（MapEvent）
    /// <summary>
    /// 映射（MapEvent）
    /// </summary>
    /// <param name="value">本次操作使用的运行事件实体。</param>
    /// <returns>从事件实体还原的统一运行事件记录。</returns>
    private static UnifiedRunEventRecord MapEvent(AgUnifiedRunEvent value) => new(
        value.ID, Required(value.EntryRunId, "EntryRunId"), Required(value.Sequence, "Sequence"),
        Required(value.CorrelationId, "CorrelationId"), Required(value.Kind, "Kind"),
        ToOffset(Required(value.OccurredAtUtc, "OccurredAtUtc")), value.ParentRunId,
        Required(value.Depth, "Depth"), Required(value.PayloadJson, "PayloadJson"),
        Required(value.PayloadSha256, "PayloadSha256"));
    #endregion

    #region 映射（MapEvent）
    /// <summary>
    /// 映射（MapEvent）
    /// </summary>
    /// <param name="value">本次操作使用的统一运行事件。</param>
    /// <returns>保留事件序号及载荷摘要的运行事件持久化实体。</returns>
    private static AgUnifiedRunEvent MapEvent(UnifiedRunEventRecord value) => new()
    {
        ID = value.Id, EntryRunId = value.EntryRunId, Sequence = value.Sequence,
        CorrelationId = value.CorrelationId, Kind = value.Kind, OccurredAtUtc = value.OccurredAtUtc.UtcDateTime,
        ParentRunId = value.ParentRunId, Depth = value.Depth, PayloadJson = value.PayloadJson,
        PayloadSha256 = value.PayloadSha256, IsDeleted = false, IsActive = true
    };
    #endregion

    #region 转换（ToOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。</returns>
    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    #endregion
    #region 转换（ToNullableOffset）
    /// <summary>
    /// 转换（ToNullableOffset）
    /// </summary>
    /// <param name="value">数据库中按 UTC 存储的可空时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间；输入为 null 时返回 null。</returns>
    private static DateTimeOffset? ToNullableOffset(DateTime? value) =>
        value.HasValue ? ToOffset(value.Value) : null;
    #endregion
    #region 转换（ToDuration）
    /// <summary>
    /// 转换（ToDuration）
    /// </summary>
    /// <param name="value">以 tick 为单位的可空持续时间。</param>
    /// <returns>由计时周期数构造的持续时间；输入为 null 时返回 null。</returns>
    private static TimeSpan? ToDuration(long? value) => value.HasValue ? TimeSpan.FromTicks(value.Value) : null;
    #endregion
    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Unified entry field '{field}' is missing.");
    #endregion
    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Unified entry field '{field}' is missing.");
    #endregion
    #region 解析（ParseEnum）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseEnum）。
    /// </summary>
    /// <typeparam name="T">目标枚举类型。</typeparam>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析的枚举值；无效输入抛出异常。</returns>
    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse(value, false, out T parsed) ? parsed :
            throw new InvalidDataException($"The database value '{value}' is not a valid {typeof(T).Name}.");
    #endregion
}

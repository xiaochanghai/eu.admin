using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：AgOrchestrationRunServices 职责实现

/// <summary>
/// 提供编排运行记录的持久化服务。
/// </summary>
public sealed class AgOrchestrationRunServices :
    BaseServices<AgOrchestrationRun>,
    IAgOrchestrationRunServices,
    IOrchestrationRunRepository
{
    public AgOrchestrationRunServices(IBaseRepository<AgOrchestrationRun> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgOrchestrationRun? existing = await Db.Queryable<AgOrchestrationRun>()
                .Where(candidate => candidate.ID == value.Id && !candidate.IsDeleted)
                .FirstAsync();
            if (existing is null)
            {
                await Db.Insertable(MapRunEntity(value)).ExecuteCommandAsync();
                await InsertNodesAsync(value, cancellationToken);
            }
            else if (ParseRunStatus(existing.Status) == OrchestrationRunStatus.Running)
            {
                AgOrchestrationRun entity = MapRunEntity(value);
                await Db.Updateable(entity)
                    .UpdateColumns(candidate => new
                    {
                        candidate.OrchestrationId,
                        candidate.OrchestrationVersionId,
                        candidate.OrchestrationCode,
                        candidate.Status,
                        candidate.StartedAtUtc,
                        candidate.FinishedAtUtc,
                        candidate.InputSha256,
                        candidate.ErrorCode
                    })
                    .Where(candidate => candidate.ID == value.Id && !candidate.IsDeleted)
                    .ExecuteCommandAsync();
                await ReplaceNodesAsync(value, cancellationToken);
            }

            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgOrchestrationRun? run = await Db.Queryable<AgOrchestrationRun>()
                .Where(value => value.ID == id && !value.IsDeleted)
                .FirstAsync();
            OrchestrationRunRecord? result = run is null
                ? null
                : (await LoadRunsAsync([run], cancellationToken))[0];
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(Guid orchestrationId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgOrchestrationRun> runs = await Db.Queryable<AgOrchestrationRun>()
                .Where(value => value.OrchestrationId == orchestrationId && !value.IsDeleted)
                .OrderBy(value => value.StartedAtUtc, OrderByType.Desc)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();
            IReadOnlyList<OrchestrationRunRecord> result = await LoadRunsAsync(
                runs,
                cancellationToken);
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task SaveDetailsAsync(OrchestrationRunDetails value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            await WriteDetailsAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<OrchestrationRunDetails?> GetDetailsAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgOrchestrationRunDetail? detail = await Db.Queryable<AgOrchestrationRunDetail>()
                .Where(value => value.RunId == runId && !value.IsDeleted)
                .FirstAsync();
            OrchestrationRunDetails? result = detail is null
                ? null
                : await LoadDetailsAsync(detail, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TrySaveRunningDetailsAsync(OrchestrationRunDetails value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgOrchestrationRun? run = await Db.Queryable<AgOrchestrationRun>()
                .Where(candidate => candidate.ID == value.RunId && !candidate.IsDeleted)
                .FirstAsync();
            if (run is null || ParseRunStatus(run.Status) != OrchestrationRunStatus.Running)
            {
                await Db.Ado.CommitTranAsync();
                return false;
            }

            await WriteDetailsAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken = default)
    {
        ValidateTerminalStatuses(runStatus, nodeStatus);
        return TransitionRunningAsync(
            runId,
            runStatus,
            nodeStatus,
            transitionPolicy,
            finishedAtUtc,
            errorCode,
            detailsIfMissing,
            cancellationToken);
    }

    public Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        string errorCode,
        CancellationToken cancellationToken = default) =>
        TransitionRunningAsync(
            runId,
            OrchestrationRunStatus.Failed,
            OrchestrationNodeRunStatus.Failed,
            OrchestrationTerminalTransitionPolicy.TerminalizePending,
            recoveredAtUtc,
            errorCode,
            detailsIfMissing: null,
            cancellationToken);

    private async Task<OrchestrationRunTransitionResult> TransitionRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgOrchestrationRun? entity = await Db.Queryable<AgOrchestrationRun>()
                .Where(value => value.ID == runId && !value.IsDeleted)
                .FirstAsync();
            if (entity is null)
            {
                await Db.Ado.CommitTranAsync();
                return new OrchestrationRunTransitionResult(null, false);
            }

            OrchestrationRunRecord current = (await LoadRunsAsync([entity], cancellationToken))[0];
            if (current.Status != OrchestrationRunStatus.Running)
            {
                await Db.Ado.CommitTranAsync();
                return new OrchestrationRunTransitionResult(current, false);
            }
            if (detailsIfMissing is not null && detailsIfMissing.RunId != runId)
            {
                throw new InvalidOperationException(
                    "Fallback orchestration details do not belong to the transitioned run.");
            }

            if (detailsIfMissing is not null && !await Db.Queryable<AgOrchestrationRunDetail>()
                .Where(value => value.RunId == runId && !value.IsDeleted)
                .AnyAsync())
            {
                await WriteDetailsAsync(detailsIfMissing, CancellationToken.None);
            }

            DateTime finished = finishedAtUtc.UtcDateTime;
            string nodeStatusText = nodeStatus.ToString();
            bool terminalizePending = transitionPolicy ==
                OrchestrationTerminalTransitionPolicy.TerminalizePending;
            await Db.Updateable<AgOrchestrationNodeAttempt>()
                .SetColumns(_ => new AgOrchestrationNodeAttempt
                {
                    Status = nodeStatusText,
                    FinishedAtUtc = finished,
                    ErrorCode = errorCode
                })
                .Where(value =>
                    value.RunId == runId &&
                    !value.IsDeleted &&
                    (value.Status == OrchestrationNodeRunStatus.Running.ToString() ||
                     (terminalizePending &&
                      value.Status == OrchestrationNodeRunStatus.Pending.ToString())))
                .ExecuteCommandAsync();
            await Db.Updateable<AgOrchestrationToolCall>()
                .SetColumns(_ => new AgOrchestrationToolCall
                {
                    Status = AgentRunEventKind.ToolFailed.ToString(),
                    FinishedAtUtc = finished,
                    ErrorCode = errorCode
                })
                .Where(value =>
                    value.RunId == runId &&
                    value.Status == AgentRunEventKind.ToolStarted.ToString() &&
                    !value.IsDeleted)
                .ExecuteCommandAsync();
            await Db.Updateable<AgOrchestrationRunNode>()
                .SetColumns(_ => new AgOrchestrationRunNode
                {
                    Status = nodeStatusText,
                    FinishedAtUtc = finished,
                    ErrorCode = errorCode
                })
                .Where(value =>
                    value.RunId == runId &&
                    !value.IsDeleted &&
                    (value.Status == OrchestrationNodeRunStatus.Running.ToString() ||
                     (terminalizePending &&
                      value.Status == OrchestrationNodeRunStatus.Pending.ToString())))
                .ExecuteCommandAsync();

            int updated = await Db.Updateable<AgOrchestrationRun>()
                .SetColumns(_ => new AgOrchestrationRun
                {
                    Status = runStatus.ToString(),
                    FinishedAtUtc = finished,
                    ErrorCode = errorCode
                })
                .Where(value =>
                    value.ID == runId &&
                    value.Status == OrchestrationRunStatus.Running.ToString() &&
                    !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                throw new InvalidOperationException(
                    $"Orchestration run '{runId}' changed during terminal transition.");
            }

            OrchestrationRunRecord terminal = TerminalizeRun(
                current,
                runStatus,
                nodeStatus,
                transitionPolicy,
                finishedAtUtc,
                errorCode);
            await Db.Ado.CommitTranAsync();
            return new OrchestrationRunTransitionResult(terminal, true);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task<IReadOnlyList<OrchestrationRunRecord>> LoadRunsAsync(IReadOnlyList<AgOrchestrationRun> runs, CancellationToken cancellationToken)
    {
        if (runs.Count == 0)
        {
            return [];
        }

        Guid[] runIds = runs.Select(value => value.ID).ToArray();
        List<AgOrchestrationRunNode> nodes = await Db.Queryable<AgOrchestrationRunNode>()
            .Where(value =>
                value.RunId.HasValue &&
                runIds.Contains(value.RunId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.RunId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyDictionary<Guid, AgOrchestrationRunNode[]> nodesByRun = nodes
            .GroupBy(value => Required(value.RunId, "Node.RunId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        return OrchestrationContractCloner.ReadOnly(runs.Select(run => MapRun(
            run,
            nodesByRun.GetValueOrDefault(run.ID) ?? [])));
    }

    private async Task<OrchestrationRunDetails> LoadDetailsAsync(AgOrchestrationRunDetail detail, CancellationToken cancellationToken)
    {
        Guid runId = Required(detail.RunId, "Detail.RunId");
        List<AgOrchestrationNodeAttempt> attempts = await Db
            .Queryable<AgOrchestrationNodeAttempt>()
            .Where(value => value.RunId == runId && !value.IsDeleted)
            .OrderBy(value => value.Sequence)
            .OrderBy(value => value.ID)
            .ToListAsync();
        List<AgOrchestrationToolCall> tools = await Db.Queryable<AgOrchestrationToolCall>()
            .Where(value => value.RunId == runId && !value.IsDeleted)
            .OrderBy(value => value.NodeId)
            .OrderBy(value => value.Attempt)
            .OrderBy(value => value.Sequence)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<(string NodeId, int Attempt), AgOrchestrationToolCall[]> toolsByAttempt =
            tools.GroupBy(value => (
                    Required(value.NodeId, "ToolCall.NodeId"),
                    Required(value.Attempt, "ToolCall.Attempt")))
                .ToDictionary(group => group.Key, group => group.ToArray());
        var result = new OrchestrationRunDetails(
            runId,
            Required(detail.OrchestrationId, "Detail.OrchestrationId"),
            Required(detail.InputText, "Detail.InputText"),
            Required(detail.OutputText, "Detail.OutputText"),
            OrchestrationContractCloner.ReadOnly(attempts.Select(value =>
            {
                string nodeId = Required(value.NodeId, "Attempt.NodeId");
                int attempt = Required(value.Attempt, "Attempt.Attempt");
                return MapAttempt(
                    value,
                    toolsByAttempt.GetValueOrDefault((nodeId, attempt)) ?? []);
            })));
        return OrchestrationContractCloner.Clone(result);
    }

    private async Task WriteDetailsAsync(OrchestrationRunDetails value, CancellationToken cancellationToken)
    {
        AgOrchestrationRunDetail? existing = await Db.Queryable<AgOrchestrationRunDetail>()
            .Where(candidate => candidate.RunId == value.RunId && !candidate.IsDeleted)
            .FirstAsync();
        if (existing is null)
        {
            await Db.Insertable(new AgOrchestrationRunDetail
            {
                ID = Guid.NewGuid(),
                RunId = value.RunId,
                OrchestrationId = value.OrchestrationId,
                InputText = value.Input,
                OutputText = value.Output,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
        }
        else
        {
            await Db.Updateable<AgOrchestrationRunDetail>()
                .SetColumns(_ => new AgOrchestrationRunDetail
                {
                    OrchestrationId = value.OrchestrationId,
                    InputText = value.Input,
                    OutputText = value.Output
                })
                .Where(candidate => candidate.ID == existing.ID && !candidate.IsDeleted)
                .ExecuteCommandAsync();
        }

        await Db.Deleteable<AgOrchestrationToolCall>()
            .Where(candidate => candidate.RunId == value.RunId)
            .ExecuteCommandAsync();
        await Db.Deleteable<AgOrchestrationNodeAttempt>()
            .Where(candidate => candidate.RunId == value.RunId)
            .ExecuteCommandAsync();

        for (int sequence = 0; sequence < value.Attempts.Count; sequence++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OrchestrationNodeAttemptRecord attempt = value.Attempts[sequence];
            await Db.Insertable(MapAttemptEntity(value.RunId, sequence, attempt))
                .ExecuteCommandAsync();
            if (attempt.ToolCalls.Count > 0)
            {
                await Db.Insertable(attempt.ToolCalls.Select((tool, toolSequence) =>
                    MapToolEntity(value.RunId, attempt.NodeId, attempt.Attempt, toolSequence, tool))
                    .ToList()).ExecuteCommandAsync();
            }
        }
    }

    private async Task ReplaceNodesAsync(OrchestrationRunRecord value, CancellationToken cancellationToken)
    {
        await Db.Deleteable<AgOrchestrationRunNode>()
            .Where(candidate => candidate.RunId == value.Id)
            .ExecuteCommandAsync();
        await InsertNodesAsync(value, cancellationToken);
    }

    private async Task InsertNodesAsync(OrchestrationRunRecord value, CancellationToken cancellationToken)
    {
        if (value.Nodes.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Insertable(value.Nodes.Select((node, ordinal) =>
            MapNodeEntity(value.Id, ordinal, node)).ToList()).ExecuteCommandAsync();
    }

    private static OrchestrationRunRecord MapRun(AgOrchestrationRun value, IReadOnlyList<AgOrchestrationRunNode> nodes) =>
        new(
            value.ID,
            Required(value.OrchestrationId, "OrchestrationId"),
            Required(value.OrchestrationVersionId, "OrchestrationVersionId"),
            Required(value.OrchestrationCode, "OrchestrationCode"),
            ParseRunStatus(value.Status),
            ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")),
            value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
            Required(value.InputSha256, "InputSha256"),
            Required(value.ErrorCode, "ErrorCode"),
            OrchestrationContractCloner.ReadOnly(nodes
                .OrderBy(node => Required(node.Ordinal, "Node.Ordinal"))
                .ThenBy(node => node.ID)
                .Select(MapNode)));

    private static OrchestrationNodeRunRecord MapNode(AgOrchestrationRunNode value) =>
        new(
            Required(value.NodeId, "Node.NodeId"),
            Required(value.NodeName, "Node.NodeName"),
            Required(value.AgentId, "Node.AgentId"),
            Required(value.AgentVersionId, "Node.AgentVersionId"),
            ParseNodeStatus(value.Status),
            Required(value.Attempts, "Node.Attempts"),
            value.StartedAtUtc.HasValue ? ToOffset(value.StartedAtUtc.Value) : null,
            value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
            Required(value.OutputCharacters, "Node.OutputCharacters"),
            Required(value.InputSha256, "Node.InputSha256"),
            Required(value.ErrorCode, "Node.ErrorCode"));

    private static OrchestrationNodeAttemptRecord MapAttempt(AgOrchestrationNodeAttempt value, IReadOnlyList<AgOrchestrationToolCall> tools) =>
        new(
            Required(value.NodeId, "Attempt.NodeId"),
            Required(value.Attempt, "Attempt.Attempt"),
            Required(value.AgentRunId, "Attempt.AgentRunId"),
            Required(value.InputText, "Attempt.InputText"),
            Required(value.InputSha256, "Attempt.InputSha256"),
            Required(value.OutputText, "Attempt.OutputText"),
            Required(value.OutputSha256, "Attempt.OutputSha256"),
            ParseNodeStatus(value.Status),
            ToOffset(Required(value.StartedAtUtc, "Attempt.StartedAtUtc")),
            value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
            Required(value.ErrorCode, "Attempt.ErrorCode"),
            OrchestrationContractCloner.ReadOnly(tools
                .OrderBy(tool => Required(tool.Sequence, "ToolCall.Sequence"))
                .ThenBy(tool => tool.ID)
                .Select(MapTool)));

    private static OrchestrationToolCallRecord MapTool(AgOrchestrationToolCall value) =>
        new(
            Required(value.ToolCallId, "ToolCall.ToolCallId"),
            Required(value.AgentRunId, "ToolCall.AgentRunId"),
            Required(value.ToolVersionId, "ToolCall.ToolVersionId"),
            Required(value.ToolName, "ToolCall.ToolName"),
            Enum.Parse<AgentRunEventKind>(Required(value.Status, "ToolCall.Status"), false),
            Required(value.ArgumentsJson, "ToolCall.ArgumentsJson"),
            Required(value.ResultContent, "ToolCall.ResultContent"),
            Required(value.ResultSha256, "ToolCall.ResultSha256"),
            checked((int)Required(value.ResultCharacters, "ToolCall.ResultCharacters")),
            ToOffset(Required(value.StartedAtUtc, "ToolCall.StartedAtUtc")),
            value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
            Required(value.ErrorCode, "ToolCall.ErrorCode"));

    private static AgOrchestrationRun MapRunEntity(OrchestrationRunRecord value) => new()
    {
        ID = value.Id,
        OrchestrationId = value.OrchestrationId,
        OrchestrationVersionId = value.OrchestrationVersionId,
        OrchestrationCode = value.OrchestrationCode,
        Status = value.Status.ToString(),
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        InputSha256 = value.InputSha256,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static AgOrchestrationRunNode MapNodeEntity(Guid runId, int ordinal, OrchestrationNodeRunRecord value) => new()
    {
        ID = Guid.NewGuid(),
        RunId = runId,
        Ordinal = ordinal,
        NodeId = value.NodeId,
        NodeName = value.NodeName,
        AgentId = value.AgentId,
        AgentVersionId = value.AgentVersionId,
        Status = value.Status.ToString(),
        Attempts = value.Attempts,
        StartedAtUtc = value.StartedAtUtc?.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        OutputCharacters = value.OutputCharacters,
        InputSha256 = value.InputSha256,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static AgOrchestrationNodeAttempt MapAttemptEntity(Guid runId, int sequence, OrchestrationNodeAttemptRecord value) => new()
    {
        ID = Guid.NewGuid(),
        RunId = runId,
        NodeId = value.NodeId,
        Attempt = value.Attempt,
        Sequence = sequence,
        AgentRunId = value.AgentRunId,
        InputText = value.Input,
        InputSha256 = value.InputSha256,
        OutputText = value.Output,
        OutputSha256 = value.OutputSha256,
        Status = value.Status.ToString(),
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static AgOrchestrationToolCall MapToolEntity(Guid runId, string nodeId, int attempt, int sequence, OrchestrationToolCallRecord value) => new()
    {
        ID = Guid.NewGuid(),
        ToolCallId = value.ToolCallId,
        RunId = runId,
        NodeId = nodeId,
        Attempt = attempt,
        Sequence = sequence,
        AgentRunId = value.AgentRunId,
        ToolVersionId = value.ToolVersionId,
        ToolName = value.ToolName,
        Status = value.Status.ToString(),
        ArgumentsJson = value.ArgumentsJson,
        ResultContent = value.ResultContent,
        ResultSha256 = value.ResultSha256,
        ResultCharacters = value.ResultCharacters,
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static OrchestrationRunRecord TerminalizeRun(
        OrchestrationRunRecord value,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode) =>
        value with
        {
            Status = runStatus,
            FinishedAtUtc = finishedAtUtc,
            ErrorCode = errorCode,
            Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                ShouldTerminalize(node.Status, transitionPolicy)
                    ? node with
                    {
                        Status = nodeStatus,
                        FinishedAtUtc = finishedAtUtc,
                        ErrorCode = errorCode
                    }
                    : node))
        };

    private static void ValidateTerminalStatuses(OrchestrationRunStatus runStatus, OrchestrationNodeRunStatus nodeStatus)
    {
        if (runStatus == OrchestrationRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runStatus),
                "A terminal run status is required.");
        }
        if (nodeStatus is OrchestrationNodeRunStatus.Pending or OrchestrationNodeRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeStatus),
                "A terminal node status is required.");
        }
    }

    private static bool ShouldTerminalize(OrchestrationNodeRunStatus status, OrchestrationTerminalTransitionPolicy transitionPolicy) =>
        status == OrchestrationNodeRunStatus.Running ||
        (status == OrchestrationNodeRunStatus.Pending &&
         transitionPolicy == OrchestrationTerminalTransitionPolicy.TerminalizePending);

    private static OrchestrationRunStatus ParseRunStatus(string? value) =>
        Enum.Parse<OrchestrationRunStatus>(Required(value, "Status"), false);

    private static OrchestrationNodeRunStatus ParseNodeStatus(string? value) =>
        Enum.Parse<OrchestrationNodeRunStatus>(Required(value, "Node.Status"), false);

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Orchestration run field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Orchestration run field '{field}' is missing.");
}

#endregion

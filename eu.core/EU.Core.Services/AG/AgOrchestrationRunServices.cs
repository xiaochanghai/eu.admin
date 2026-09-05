using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgOrchestrationRunServices 职责实现

/// <summary>
/// 提供编排运行记录的持久化服务。
/// </summary>
public sealed class AgOrchestrationRunServices :
    BaseServices<AgOrchestrationRun>,
    IAgOrchestrationRunServices,
    IOrchestrationRunRepository
{
    #region 构造（AgOrchestrationRunServices）
    /// <summary>
    /// 构造（AgOrchestrationRunServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgOrchestrationRunServices(IBaseRepository<AgOrchestrationRun> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 保存（SaveAsync）
    /// <summary>
    /// 保存（SaveAsync）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 读取编排运行及节点记录（GetAsync）
    /// <summary>
    /// 读取编排运行及节点记录（GetAsync）。
    /// </summary>
    /// <param name="id">编排运行标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的编排运行及其节点记录；不存在时为 null。</returns>
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
    #endregion

    #region 按时间倒序列出编排运行（ListAsync）
    /// <summary>
    /// 按时间倒序列出编排运行（ListAsync）。
    /// </summary>
    /// <param name="orchestrationId">编排定义标识。</param>
    /// <param name="take">期望返回的记录数，持久化实现将其限制在 1 至 100 之间。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回指定编排下未删除的运行记录，按开始时间倒序排列；无记录时为空集合。</returns>
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
    #endregion

    #region 保存（SaveDetailsAsync）
    /// <summary>
    /// 保存（SaveDetailsAsync）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行及节点尝试详情。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 读取编排运行详情（GetDetailsAsync）
    /// <summary>
    /// 读取编排运行详情（GetDetailsAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的运行详情及尝试、工具调用记录；详情不存在时为 null。</returns>
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
    #endregion

    #region 仅为运行中的编排保存详情（TrySaveRunningDetailsAsync）
    /// <summary>
    /// 仅为运行中的编排保存详情（TrySaveRunningDetailsAsync）。
    /// </summary>
    /// <param name="value">待保存的编排运行详情，RunId 用于定位运行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>存在未删除且状态为 Running 的运行记录并成功写入详情时返回 true；记录不存在或不再运行时返回 false。</returns>
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
    #endregion

    #region 原子地终结编排运行及相关执行记录（TryFinalizeRunningAsync）
    /// <summary>
    /// 原子地终结编排运行及相关执行记录（TryFinalizeRunningAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="runStatus">拟保存的编排终态，只允许受支持的结束状态。</param>
    /// <param name="nodeStatus">需要终结的节点及尝试记录使用的终态。</param>
    /// <param name="transitionPolicy">决定是否同时终结 Pending 节点和尝试。</param>
    /// <param name="finishedAtUtc">完成时间（UTC）。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="detailsIfMissing">运行详情缺失时补写的详情，非 null 时 RunId 必须与目标运行一致。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回转换后的运行记录和 Transitioned 标记；实际完成转换时标记为 true，原记录已非 Running 时返回原记录及 false，记录不存在时 Run 为 null 且标记为 false。</returns>
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
    #endregion

    #region 将中断的编排运行终结为失败（RecoverInterruptedAsync）
    /// <summary>
    /// 将中断的编排运行终结为失败（RecoverInterruptedAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回失败转换后的运行记录及 Transitioned 标记；原记录已非 Running 时不转换，记录不存在时 Run 为 null；不重新执行编排。</returns>
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
    #endregion

    #region 在事务中终结编排及未完成执行记录（TransitionRunningAsync）
    /// <summary>
    /// 在事务中终结编排及未完成执行记录（TransitionRunningAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="runStatus">运行状态。</param>
    /// <param name="nodeStatus">编排节点状态。</param>
    /// <param name="transitionPolicy">控制是否将 Pending 节点和尝试与 Running 项一同终结的策略。</param>
    /// <param name="finishedAtUtc">完成时间（UTC）。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="detailsIfMissing">仅在详情缺失时补写的备用详情，其 RunId 必须匹配目标运行。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回终结后的运行记录和 true；原运行已非 Running 时返回原记录和 false，不存在时返回 null 记录和 false；事务或并发写入异常通过异常报告。</returns>
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
    #endregion

    #region 加载（LoadRunsAsync）
    /// <summary>
    /// 加载（LoadRunsAsync）
    /// </summary>
    /// <param name="runs">运行记录集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保持输入顺序并补齐有序节点状态的编排运行集合。</returns>
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
    #endregion

    #region 加载（LoadDetailsAsync）
    /// <summary>
    /// 加载（LoadDetailsAsync）
    /// </summary>
    /// <param name="detail">明细数据。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含节点尝试和各次工具调用的编排运行详情副本。</returns>
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
    #endregion

    #region 写入（WriteDetailsAsync）
    /// <summary>
    /// 写入（WriteDetailsAsync）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行及节点尝试详情。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 处理（ReplaceNodesAsync）
    /// <summary>
    /// 处理（ReplaceNodesAsync）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ReplaceNodesAsync(OrchestrationRunRecord value, CancellationToken cancellationToken)
    {
        await Db.Deleteable<AgOrchestrationRunNode>()
            .Where(candidate => candidate.RunId == value.Id)
            .ExecuteCommandAsync();
        await InsertNodesAsync(value, cancellationToken);
    }
    #endregion

    #region 新增（InsertNodesAsync）
    /// <summary>
    /// 新增（InsertNodesAsync）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 映射（MapRun）
    /// <summary>
    /// 映射（MapRun）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行实体。</param>
    /// <param name="nodes">编排节点集合。</param>
    /// <returns>包含按序排列的节点状态的编排运行记录。</returns>
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
    #endregion

    #region 映射（MapNode）
    /// <summary>
    /// 映射（MapNode）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行节点实体。</param>
    /// <returns>包含执行次数、输入摘要、输出长度及错误信息的节点运行记录。</returns>
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
    #endregion

    #region 映射（MapAttempt）
    /// <summary>
    /// 映射（MapAttempt）
    /// </summary>
    /// <param name="value">本次操作使用的编排节点尝试实体。</param>
    /// <param name="tools">工具集合。</param>
    /// <returns>包含输入输出及有序工具调用明细的节点尝试记录。</returns>
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
    #endregion

    #region 映射（MapTool）
    /// <summary>
    /// 映射（MapTool）
    /// </summary>
    /// <param name="value">本次操作使用的编排工具调用实体。</param>
    /// <returns>包含工具参数、结果、摘要及执行状态的编排工具调用记录。</returns>
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
    #endregion

    #region 映射（MapRunEntity）
    /// <summary>
    /// 映射（MapRunEntity）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行记录。</param>
    /// <returns>由编排运行记录构造的主表持久化实体。</returns>
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
    #endregion

    #region 映射（MapNodeEntity）
    /// <summary>
    /// 映射（MapNodeEntity）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="ordinal">节点在所属版本或运行中的排序序号。</param>
    /// <param name="value">本次操作使用的编排节点运行记录。</param>
    /// <returns>带有所属运行及节点序号的节点运行实体。</returns>
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
    #endregion

    #region 映射（MapAttemptEntity）
    /// <summary>
    /// 映射（MapAttemptEntity）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="sequence">事件或记录序号。</param>
    /// <param name="value">本次操作使用的编排节点尝试记录。</param>
    /// <returns>带有所属运行、节点、尝试次数及序号的节点尝试实体。</returns>
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
    #endregion

    #region 映射（MapToolEntity）
    /// <summary>
    /// 映射（MapToolEntity）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="nodeId">编排节点标识。</param>
    /// <param name="attempt">任务执行尝试。</param>
    /// <param name="sequence">事件或记录序号。</param>
    /// <param name="value">本次操作使用的编排工具调用记录。</param>
    /// <returns>带有所属运行、节点尝试及调用序号的工具调用实体。</returns>
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
    #endregion

    #region 处理（TerminalizeRun）
    /// <summary>
    /// 处理（TerminalizeRun）
    /// </summary>
    /// <param name="value">本次操作使用的编排运行记录。</param>
    /// <param name="runStatus">运行状态。</param>
    /// <param name="nodeStatus">编排节点状态。</param>
    /// <param name="transitionPolicy">运行状态转换策略。</param>
    /// <param name="finishedAtUtc">完成时间（UTC）。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>设置指定运行终态并按转换策略更新节点终态的记录副本；不执行持久化。</returns>
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
    #endregion

    #region 校验（ValidateTerminalStatuses）
    /// <summary>
    /// 校验（ValidateTerminalStatuses）
    /// </summary>
    /// <param name="runStatus">运行状态。</param>
    /// <param name="nodeStatus">编排节点状态。</param>
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
    #endregion

    #region 判断编排节点是否需要转为终态（ShouldTerminalize）
    /// <summary>
    /// 判断编排节点是否需要转为终态（ShouldTerminalize）。
    /// </summary>
    /// <param name="status">节点当前运行状态。</param>
    /// <param name="transitionPolicy">是否同时终结尚未执行节点的收尾策略。</param>
    /// <returns>节点为 Running，或节点为 Pending 且策略为 TerminalizePending 时返回 true，否则返回 false。</returns>
    private static bool ShouldTerminalize(OrchestrationNodeRunStatus status, OrchestrationTerminalTransitionPolicy transitionPolicy) =>
        status == OrchestrationNodeRunStatus.Running ||
        (status == OrchestrationNodeRunStatus.Pending &&
         transitionPolicy == OrchestrationTerminalTransitionPolicy.TerminalizePending);
    #endregion

    #region 解析（ParseRunStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseRunStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析的枚举值；无效输入抛出异常。</returns>
    private static OrchestrationRunStatus ParseRunStatus(string? value) =>
        Enum.Parse<OrchestrationRunStatus>(Required(value, "Status"), false);
    #endregion

    #region 解析（ParseNodeStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseNodeStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析的枚举值；无效输入抛出异常。</returns>
    private static OrchestrationNodeRunStatus ParseNodeStatus(string? value) =>
        Enum.Parse<OrchestrationNodeRunStatus>(Required(value, "Node.Status"), false);
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

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Orchestration run field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Orchestration run field '{field}' is missing.");
    #endregion
}

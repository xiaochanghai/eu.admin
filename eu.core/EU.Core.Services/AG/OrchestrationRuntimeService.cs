using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Model;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

#nullable enable

namespace EU.Core.Services;

// 文件职责：OrchestrationRuntimeService 职责实现

/// <summary>
/// 负责执行已发布的 Agent 编排。
/// </summary>
/// <param name="orchestrations">用于读取和持久化编排定义的仓储。</param>
/// <param name="runs">用于读取和持久化编排运行记录的仓储。</param>
/// <param name="agents">用于查询 Agent 定义及已发布版本的目录。</param>
/// <param name="agentRuntime">用于准备和启动 Agent 运行的服务。</param>
/// <param name="payloadLimits">编排执行载荷的大小限制；为 null 时使用默认限制。</param>
public sealed class OrchestrationRuntimeService(
    IOrchestrationRepository orchestrations,
    IOrchestrationRunRepository runs,
    IAgentDefinitionCatalog agents,
    IAgentRuntimeService agentRuntime,
    ExecutionPayloadLimits? payloadLimits = null) : BaseServices
{
    /// <summary>单次运行输入允许的最大字符数。</summary>
    public const int MaximumInputCharacters = 32_768;
    private const string TerminalPersistenceFailureDataKey =
        "OrchestrationTerminalPersistenceFailure";
    private readonly ExecutionPayloadLimits _payloadLimits =
        payloadLimits ?? new ExecutionPayloadLimits();
    private readonly ConcurrentDictionary<Guid, ActiveExecution> _active = [];
    private readonly ConcurrentDictionary<Guid, byte> _retired = [];
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _runGates = [];
    private readonly ConcurrentDictionary<Guid, string> _outputs = [];
    private readonly ConcurrentQueue<Guid> _outputOrder = [];

    #region 启动（StartAsync）
    /// <summary>
    /// 启动（StartAsync）
    /// </summary>
    /// <param name="orchestrationId">编排定义标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<OrchestrationRunRecord>> StartAsync(Guid orchestrationId, string? input, CancellationToken cancellationToken = default)
    {
        string normalized = input?.Trim() ?? "";
        if (normalized.Length is 0 or > MaximumInputCharacters)
            return Failure(
                OrchestrationErrorCodes.RunInputInvalid,
                $"Input must contain from 1 through {MaximumInputCharacters} characters.");
        OrchestrationDefinition? definition =
            await orchestrations.GetByIdAsync(orchestrationId, cancellationToken);
        if (definition is null)
            return Failure(
                OrchestrationErrorCodes.NotFound, "The orchestration was not found.");
        if (definition.Status != OrchestrationStatus.Enabled)
            return Failure(
                OrchestrationErrorCodes.Disabled, "The orchestration is disabled.");
        OrchestrationVersionSnapshot? snapshot =
            definition.PublishedVersions.LastOrDefault()?.Snapshot;
        if (snapshot is null)
            return Failure(
                OrchestrationErrorCodes.VersionMissing, "The orchestration has no published version.");

        return await StartSnapshotAsync(definition, snapshot, normalized, cancellationToken);
    }
    #endregion

    #region 启动（StartVersionAsync）
    /// <summary>
    /// 启动（StartVersionAsync）
    /// </summary>
    /// <param name="orchestrationId">编排定义标识。</param>
    /// <param name="orchestrationVersionId">编排版本标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<OrchestrationRunRecord>> StartVersionAsync(
        Guid orchestrationId,
        Guid orchestrationVersionId,
        string input,
        CancellationToken cancellationToken = default)
        => await StartVersionAsync(
            orchestrationId,
            orchestrationVersionId,
            input,
            executionOptions: null,
            cancellationToken);
    #endregion

    #region 启动（StartVersionAsync）
    /// <summary>
    /// 启动（StartVersionAsync）
    /// </summary>
    /// <param name="orchestrationId">编排定义标识。</param>
    /// <param name="orchestrationVersionId">编排版本标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="executionOptions">当前运行使用的执行选项。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<OrchestrationRunRecord>> StartVersionAsync(
        Guid orchestrationId,
        Guid orchestrationVersionId,
        string input,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken = default)
    {
        string normalized = input?.Trim() ?? "";
        if (normalized.Length is 0 or > MaximumInputCharacters)
            return Failure(
                OrchestrationErrorCodes.RunInputInvalid,
                $"Input must contain from 1 through {MaximumInputCharacters} characters.");
        OrchestrationDefinition? definition =
            await orchestrations.GetByIdAsync(orchestrationId, cancellationToken);
        if (definition is null)
            return Failure(
                OrchestrationErrorCodes.NotFound, "The orchestration was not found.");
        if (definition.Status != OrchestrationStatus.Enabled)
            return Failure(
                OrchestrationErrorCodes.Disabled, "The orchestration is disabled.");
        OrchestrationVersionSnapshot? snapshot = definition.PublishedVersions
            .FirstOrDefault(version => version.Id == orchestrationVersionId)
            ?.Snapshot;
        if (snapshot is null)
            return Failure(
                OrchestrationErrorCodes.VersionMissing,
                "The requested orchestration version is not published by this orchestration.");

        return await StartSnapshotAsync(
            definition,
            snapshot,
            normalized,
            executionOptions,
            cancellationToken);
    }
    #endregion

    #region 启动（StartSnapshotAsync）
    /// <summary>
    /// 启动（StartSnapshotAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="normalized">规范化后的值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    private async Task<ServiceResult<OrchestrationRunRecord>> StartSnapshotAsync(
        OrchestrationDefinition definition,
        OrchestrationVersionSnapshot snapshot,
        string normalized,
        CancellationToken cancellationToken) =>
        await StartSnapshotAsync(
            definition,
            snapshot,
            normalized,
            executionOptions: null,
            cancellationToken);
    #endregion

    #region 启动（StartSnapshotAsync）
    /// <summary>
    /// 启动（StartSnapshotAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="normalized">规范化后的值。</param>
    /// <param name="executionOptions">当前运行使用的执行选项。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    private async Task<ServiceResult<OrchestrationRunRecord>> StartSnapshotAsync(
        OrchestrationDefinition definition,
        OrchestrationVersionSnapshot snapshot,
        string normalized,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken)
    {
        foreach (OrchestrationAgentBinding binding in snapshot.Agents)
        {
            AgentDefinition? agent = await agents.GetDefinitionAsync(binding.AgentId, cancellationToken);
            if (agent?.RuntimeStatus != AgentRuntimeStatus.Enabled ||
                agent.PublishedVersions.LastOrDefault()?.Id != binding.AgentVersionId)
                return Failure(
                    OrchestrationErrorCodes.AgentUnavailable,
                    $"Bound Agent version '{binding.AgentVersionId}' is no longer current and enabled.");
        }

        var record = new OrchestrationRunRecord(
            Guid.NewGuid(), definition.Id, snapshot.VersionId, definition.Code,
            OrchestrationRunStatus.Running, DateTimeOffset.UtcNow, null,
            Hash(normalized), "", OrchestrationContractCloner.ReadOnly(snapshot.Nodes.Select(node =>
            {
                OrchestrationAgentBinding binding = snapshot.Agents.Single(value => value.AgentId == node.AgentId);
                return new OrchestrationNodeRunRecord(
                    node.Id, node.Name, node.AgentId, binding.AgentVersionId,
                    OrchestrationNodeRunStatus.Pending, 0, null, null, 0, "", "");
            })));
        var source = new CancellationTokenSource();
        var registered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task execution = ExecuteAfterRegistrationAsync(
            record,
            snapshot,
            normalized,
            executionOptions,
            source,
            registered.Task);
        var active = new ActiveExecution(source, execution);
        if (!_active.TryAdd(record.Id, active))
        {
            registered.TrySetResult(false);
            source.Dispose();
            throw new InvalidOperationException("Run identifier collision.");
        }

        bool runRecordPersisted = false;
        var initialDetails = new OrchestrationRunDetails(
            record.Id,
            record.OrchestrationId,
            RedactContent(normalized),
            "",
            OrchestrationContractCloner.ReadOnly(
                Array.Empty<OrchestrationNodeAttemptRecord>()));
        try
        {
            await runs.SaveAsync(record, cancellationToken);
            runRecordPersisted = true;
            bool detailsPersisted = await runs.TrySaveRunningDetailsAsync(
                initialDetails,
                cancellationToken);
            if (!detailsPersisted)
            {
                throw new InvalidOperationException(
                    "The orchestration run became terminal before its details were initialized.");
            }
        }
        catch (Exception exception)
        {
            source.Dispose();
            if (runRecordPersisted)
            {
                bool cancelled =
                    exception is OperationCanceledException &&
                    cancellationToken.IsCancellationRequested;
                await ReconcileInitializationFailureAsync(
                    record.Id,
                    initialDetails,
                    cancelled
                        ? OrchestrationRunStatus.Cancelled
                        : OrchestrationRunStatus.Failed,
                    cancelled
                        ? OrchestrationNodeRunStatus.Cancelled
                        : OrchestrationNodeRunStatus.Failed,
                    cancelled
                        ? "ORCHESTRATION_RUN_CANCELLED"
                        : "ORCHESTRATION_RUN_FAILED",
                    exception);
            }

            _active.TryRemove(record.Id, out _);
            registered.TrySetResult(false);
            await execution;
            throw;
        }

        registered.TrySetResult(true);
        return Success(record);
    }
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<OrchestrationRunRecord> Failure(string code, string message) =>
        ServiceResult<OrchestrationRunRecord>.Failure(
            OrchestrationServiceStatusCodes.FromErrorCode(code),
            message);
    #endregion

    #region 核对并同步（ReconcileInitializationFailureAsync）
    /// <summary>
    /// 核对并同步（ReconcileInitializationFailureAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="detailsIfMissing">明细缺失时使用的默认数据。</param>
    /// <param name="runStatus">运行状态。</param>
    /// <param name="nodeStatus">编排节点状态。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="originalException">最初导致失败的异常。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ReconcileInitializationFailureAsync(
        Guid runId,
        OrchestrationRunDetails detailsIfMissing,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        string errorCode,
        Exception originalException)
    {
        try
        {
            OrchestrationRunRecord? authoritative =
                await runs.GetAsync(runId, CancellationToken.None)
                    .ConfigureAwait(false);
            if (authoritative is null
                || authoritative.Status != OrchestrationRunStatus.Running)
            {
                return;
            }

            await runs.TryFinalizeRunningAsync(
                runId,
                runStatus,
                nodeStatus,
                OrchestrationTerminalTransitionPolicy.TerminalizePending,
                DateTimeOffset.UtcNow,
                errorCode,
                detailsIfMissing,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception terminalException)
        {
            try
            {
                OrchestrationRunRecord? authoritative =
                    await runs.GetAsync(runId, CancellationToken.None)
                        .ConfigureAwait(false);
                if (authoritative?.Status == OrchestrationRunStatus.Running)
                {
                    originalException.Data[TerminalPersistenceFailureDataKey] =
                        terminalException;
                }
            }
            catch (Exception reconciliationException)
            {
                originalException.Data[TerminalPersistenceFailureDataKey] =
                    new AggregateException(
                        terminalException,
                        reconciliationException);
            }
        }
    }
    #endregion

    #region 读取运行并检查宿主中断（GetAsync）
    /// <summary>
    /// 读取运行并检查宿主中断（GetAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回经中断恢复检查后的运行记录；不存在时为 null；该查询可能将无活动执行的 Running 记录终结为失败。</returns>
    public async Task<OrchestrationRunRecord?> GetAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await RecoverIfInterruptedAsync(await runs.GetAsync(runId, cancellationToken), cancellationToken);
    #endregion

    #region 读取编排运行详情（GetDetailsAsync）
    /// <summary>
    /// 读取编排运行详情（GetDetailsAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的运行详情及尝试、工具调用记录；详情不存在时为 null。</returns>
    public Task<OrchestrationRunDetails?> GetDetailsAsync(Guid runId, CancellationToken cancellationToken = default) =>
        runs.GetDetailsAsync(runId, cancellationToken);
    #endregion

    #region 列出编排运行并检查中断记录（ListAsync）
    /// <summary>
    /// 列出编排运行并检查中断记录（ListAsync）。
    /// </summary>
    /// <param name="orchestrationId">编排定义标识。</param>
    /// <param name="take">期望返回的记录数，持久化实现将其限制在 1 至 100 之间。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回查询并逐项完成中断恢复检查的运行列表；检查可能写入失败终态，无匹配项时为空集合。</returns>
    public async Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(Guid orchestrationId, int take, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrchestrationRunRecord> values =
            await runs.ListAsync(orchestrationId, take, cancellationToken);
        var recovered = new List<OrchestrationRunRecord>(values.Count);
        foreach (OrchestrationRunRecord value in values)
            recovered.Add((await RecoverIfInterruptedAsync(value, cancellationToken))!);
        return OrchestrationContractCloner.ReadOnly(recovered);
    }
    #endregion

    #region 请求取消编排运行（CancelAsync）
    /// <summary>
    /// 请求取消编排运行（CancelAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>找到活动执行并发出取消请求，或持久化运行记录存在时返回 true；两者都不存在时返回 false；不保证运行已经结束。</returns>
    public async Task<bool> CancelAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (!_active.TryGetValue(runId, out ActiveExecution? active))
            return await runs.GetAsync(runId, cancellationToken) is not null;
        try
        {
            await active.Cancellation.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Execution reached its terminal record between lookup and cancellation.
        }
        return true;
    }
    #endregion

    #region 强制终结编排并移出活动执行集合（ForceCancelAsync）
    /// <summary>
    /// 强制终结编排并移出活动执行集合（ForceCancelAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消等待运行互斥锁的令牌；取得锁后的持久化操作不使用该令牌。</param>
    /// <returns>返回尝试取消后的运行记录；已非 Running 的记录保留原状态，记录不存在时返回 null；活动执行会被移出并尝试取消，不等待其任务结束。</returns>
    public async Task<OrchestrationRunRecord?> ForceCancelAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        ActiveExecution? retiredExecution = null;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? value =
                await runs.GetAsync(
                    runId,
                    CancellationToken.None).ConfigureAwait(false);
            if (value is null)
            {
                _active.TryRemove(runId, out retiredExecution);
                return null;
            }

            OrchestrationRunRecord terminal = value;
            if (value.Status == OrchestrationRunStatus.Running)
            {
                DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
                OrchestrationRunTransitionResult result =
                    await runs.TryFinalizeRunningAsync(
                        runId,
                        OrchestrationRunStatus.Cancelled,
                        OrchestrationNodeRunStatus.Cancelled,
                        OrchestrationTerminalTransitionPolicy.TerminalizePending,
                        finishedAt,
                        "ORCHESTRATION_RUN_CANCELLED",
                        detailsIfMissing: null,
                        CancellationToken.None).ConfigureAwait(false);
                terminal = result.Run ?? value;
                if (result.Transitioned)
                {
                    _retired[runId] = 0;
                }
            }

            _active.TryRemove(runId, out retiredExecution);
            return terminal;
        }
        finally
        {
            gate.Release();
            if (retiredExecution is not null)
            {
                CancelRetiredExecutionBestEffort(retiredExecution);
                ObserveRetiredExecution(runId, retiredExecution.Execution);
            }
        }
    }
    #endregion

    #region 等待活动执行并读取编排运行结果（WaitForTerminalAsync）
    /// <summary>
    /// 等待活动执行并读取编排运行结果（WaitForTerminalAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>存在活动执行时先等待其任务，再返回经中断恢复检查的运行记录；无记录时为 null，等待任务的异常继续向上传播。</returns>
    public async Task<OrchestrationRunRecord?> WaitForTerminalAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (_active.TryGetValue(runId, out ActiveExecution? active))
            await active.Execution.WaitAsync(cancellationToken);
        return await GetAsync(runId, cancellationToken);
    }
    #endregion

    #region 获取（GetEphemeralOutput）
    /// <summary>
    /// 获取（GetEphemeralOutput）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <returns>当前进程暂存的运行输出；未找到时为 null。</returns>
    public string? GetEphemeralOutput(Guid runId) =>
        _outputs.TryGetValue(runId, out string? value) ? value : null;
    #endregion

    #region 执行（ExecuteAfterRegistrationAsync）
    /// <summary>
    /// 执行（ExecuteAfterRegistrationAsync）
    /// </summary>
    /// <param name="record">业务记录。</param>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="initialInput">初始执行输入。</param>
    /// <param name="executionOptions">当前运行使用的执行选项。</param>
    /// <param name="source">源数据。</param>
    /// <param name="registered">已登记的数据。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ExecuteAfterRegistrationAsync(
        OrchestrationRunRecord record,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        AgentRunExecutionOptions? executionOptions,
        CancellationTokenSource source,
        Task<bool> registered)
    {
        if (!await registered) return;
        await ExecuteGuardedAsync(
            record,
            snapshot,
            initialInput,
            executionOptions,
            source);
    }
    #endregion

    #region 执行（ExecuteGuardedAsync）
    /// <summary>
    /// 执行（ExecuteGuardedAsync）
    /// </summary>
    /// <param name="record">业务记录。</param>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="initialInput">初始执行输入。</param>
    /// <param name="executionOptions">当前运行使用的执行选项。</param>
    /// <param name="source">源数据。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ExecuteGuardedAsync(
        OrchestrationRunRecord record,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        AgentRunExecutionOptions? executionOptions,
        CancellationTokenSource source)
    {
        try
        {
            await ExecuteAsync(
                record,
                snapshot,
                initialInput,
                executionOptions,
                source.Token);
        }
        catch (OrchestrationOwnershipLostException)
        {
            // Another runtime made a durable terminal state authoritative.
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            await FinishAsync(record.Id, OrchestrationRunStatus.Cancelled, "ORCHESTRATION_RUN_CANCELLED");
        }
        catch (AgentRuntimeException exception)
        {
            await FinishAsync(record.Id, OrchestrationRunStatus.Failed, exception.ErrorCode);
        }
        catch
        {
            await FinishAsync(record.Id, OrchestrationRunStatus.Failed, "ORCHESTRATION_RUN_FAILED");
        }
        finally
        {
            _active.TryRemove(record.Id, out _);
            source.Dispose();
            _retired.TryRemove(record.Id, out _);
            _runGates.TryRemove(record.Id, out _);
        }
    }
    #endregion

    #region 执行（ExecuteAsync）
    /// <summary>
    /// 执行（ExecuteAsync）
    /// </summary>
    /// <param name="initial">初始数据。</param>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="initialInput">初始执行输入。</param>
    /// <param name="executionOptions">当前运行使用的执行选项。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ExecuteAsync(
        OrchestrationRunRecord initial,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken)
    {
        string nodeId = snapshot.StartNodeId;
        string previousOutput = "";
        bool previousSucceeded = true;
        int visited = 0;
        while (nodeId.Length > 0 && visited++ < snapshot.Nodes.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OrchestrationNode node = snapshot.Nodes.Single(value => value.Id == nodeId);
            string nodeInput = BuildInput(node, initialInput, previousOutput);
            Guid agentVersionId = initial.Nodes
                .Single(value => value.NodeId == node.Id)
                .AgentVersionId;
            (bool succeeded, string output, string errorCode) result =
                await ExecuteNodeAsync(
                    initial.Id,
                    node,
                    agentVersionId,
                    nodeInput,
                    executionOptions,
                    cancellationToken);
            previousSucceeded = result.succeeded;
            previousOutput = result.output;
            OrchestrationEdge? edge = snapshot.Edges
                .Where(value => value.FromNodeId == nodeId)
                .OrderBy(value => value.Order)
                .FirstOrDefault(value => Matches(value, previousSucceeded, previousOutput));
            if (edge is null)
            {
                if (previousSucceeded)
                {
                    EnsureWithinLimit(
                        previousOutput, _payloadLimits.FinalOutputCharacters, "final output");
                    EnsureOwnership(
                        initial.Id,
                        await UpdateDetailsOutputAsync(
                            initial.Id,
                            RedactContent(previousOutput)));
                }
                bool finalized = await FinishAsync(
                    initial.Id,
                    previousSucceeded ? OrchestrationRunStatus.Completed : OrchestrationRunStatus.Failed,
                    previousSucceeded ? "" : result.errorCode);
                EnsureOwnership(initial.Id, finalized);
                if (previousSucceeded)
                {
                    CacheOutput(initial.Id, previousOutput);
                }
                return;
            }
            nodeId = edge.ToNodeId;
        }
        await FinishAsync(initial.Id, OrchestrationRunStatus.Failed, "ORCHESTRATION_STEP_LIMIT");
    }
    #endregion

    #region 执行编排节点并记录重试过程（ExecuteNodeAsync）
    /// <summary>
    /// 执行编排节点并记录重试过程（ExecuteNodeAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="node">包含目标 Agent、超时时间和最大重试次数的编排节点。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="executionOptions">可选的内部工具、MCP 守卫、执行身份和审批处理覆盖项。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回执行结果元组：succeeded 表示是否成功，成功时 output 为未脱敏输出且 errorCode 为空；重试耗尽时 output 为空、errorCode 为最后保存的错误码。</returns>
    private async Task<(bool succeeded, string output, string errorCode)> ExecuteNodeAsync(
        Guid runId,
        OrchestrationNode node,
        Guid agentVersionId,
        string input,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken)
    {
        string lastError = "";
        for (int attempt = 1; attempt <= node.MaximumRetries + 1; attempt++)
        {
            DateTimeOffset started = DateTimeOffset.UtcNow;
            string persistedInput = RedactContent(input);
            EnsureWithinLimit(
                persistedInput, _payloadLimits.NodeInputCharacters, "node input");
            var attemptRecord = new OrchestrationNodeAttemptRecord(
                node.Id,
                attempt,
                Guid.Empty,
                persistedInput,
                Hash(input),
                "",
                "",
                OrchestrationNodeRunStatus.Running,
                started,
                null,
                "",
                OrchestrationContractCloner.ReadOnly(Array.Empty<OrchestrationToolCallRecord>()));
            EnsureOwnership(
                runId,
                await UpsertAttemptAsync(runId, attemptRecord, cancellationToken));
            EnsureOwnership(
                runId,
                await UpdateNodeAsync(runId, node.Id, current => current with
            {
                Status = OrchestrationNodeRunStatus.Running,
                Attempts = attempt,
                StartedAtUtc = current.StartedAtUtc ?? started,
                InputSha256 = Hash(input),
                ErrorCode = ""
            }));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(node.TimeoutSeconds));
            try
            {
                AgentRunPreparationResult prepared =
                    await agentRuntime.PrepareVersionAsync(
                        node.AgentId, agentVersionId, input, timeout.Token);
                if (!prepared.Succeeded)
                {
                    lastError = prepared.Error!.Code;
                    attemptRecord = attemptRecord with
                    {
                        Status = OrchestrationNodeRunStatus.Failed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        ErrorCode = lastError
                    };
                    EnsureOwnership(
                        runId,
                        await UpsertAttemptAsync(
                            runId,
                            attemptRecord,
                            cancellationToken));
                    continue;
                }
                AgentRunContext executionContext = prepared.Context! with
                {
                    InternalTools = executionOptions?.InternalTools?.ToArray()
                        ?? prepared.Context.InternalTools,
                    McpCallGuard = executionOptions?.McpCallGuard
                        ?? prepared.Context.McpCallGuard,
                    McpResultGuard = executionOptions?.McpResultGuard
                        ?? prepared.Context.McpResultGuard,
                    ExecutionIdentity = executionOptions?.ExecutionIdentity
                        ?? prepared.Context.ExecutionIdentity,
                    ToolApprovalBinding = executionOptions?.ToolApprovalBinding
                        ?? prepared.Context.ToolApprovalBinding,
                    ToolApprovalHandler = executionOptions?.ToolApprovalHandler
                        ?? prepared.Context.ToolApprovalHandler
                };
                attemptRecord = attemptRecord with
                {
                    AgentRunId = executionContext.RunId
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        cancellationToken));
                var output = new StringBuilder();
                var toolCalls = new List<OrchestrationToolCallRecord>();
                bool failed = false;
                await foreach (AgentRunEvent value in agentRuntime.StreamAsync(
                    executionContext,
                    timeout.Token))
                {
                    if (value.Kind == AgentRunEventKind.Delta)
                    {
                        string candidate = output.ToString() + value.Text;
                        EnsureWithinLimit(
                            candidate, _payloadLimits.NodeOutputCharacters, "node output");
                        output.Append(value.Text);
                    }
                    if (value.Kind == AgentRunEventKind.ToolStarted)
                    {
                        string arguments = RedactContent(value.ArgumentsJson);
                        EnsureWithinLimit(
                            arguments, _payloadLimits.ToolArgumentsCharacters, "tool arguments");
                        toolCalls.Add(new OrchestrationToolCallRecord(
                            value.ToolCallId ?? Guid.NewGuid(),
                            executionContext.RunId,
                            value.ToolVersionId ?? Guid.Empty,
                            value.ToolName,
                            value.Kind,
                            arguments,
                            "",
                            "",
                            0,
                            value.OccurredAtUtc,
                            null,
                            ""));
                    }
                    if (value.Kind is AgentRunEventKind.ToolSucceeded or
                        AgentRunEventKind.ToolBlocked or
                        AgentRunEventKind.ToolFailed)
                    {
                        string resultContent = RedactContent(value.Text);
                        EnsureWithinLimit(
                            resultContent, _payloadLimits.ToolResultCharacters, "tool result");
                        int index = value.ToolCallId is Guid callId
                            ? toolCalls.FindIndex(call => call.ToolCallId == callId)
                            : -1;
                        OrchestrationToolCallRecord current = index >= 0
                            ? toolCalls[index]
                            : new OrchestrationToolCallRecord(
                                value.ToolCallId ?? Guid.NewGuid(),
                                executionContext.RunId,
                                value.ToolVersionId ?? Guid.Empty,
                                value.ToolName,
                                AgentRunEventKind.ToolStarted,
                                "",
                                "",
                                "",
                                0,
                                value.OccurredAtUtc,
                                null,
                                "");
                        OrchestrationToolCallRecord terminal = current with
                        {
                            Status = value.Kind,
                            ResultContent = resultContent,
                            ResultSha256 = Hash(value.Text),
                            ResultCharacters = value.Text.Length,
                            FinishedAtUtc = value.OccurredAtUtc,
                            ErrorCode = value.ErrorCode
                        };
                        if (index >= 0) toolCalls[index] = terminal;
                        else toolCalls.Add(terminal);
                    }
                    if (value.Kind is AgentRunEventKind.Failed or AgentRunEventKind.Cancelled)
                    {
                        failed = true;
                        lastError = value.ErrorCode;
                    }
                    attemptRecord = attemptRecord with
                    {
                        Output = RedactContent(output.ToString()),
                        OutputSha256 = output.Length == 0 ? "" : Hash(output.ToString()),
                        ToolCalls = OrchestrationContractCloner.ReadOnly(toolCalls)
                    };
                    EnsureOwnership(
                        runId,
                        await UpsertAttemptAsync(
                            runId,
                            attemptRecord,
                            timeout.Token));
                }
                if (!failed)
                {
                    attemptRecord = attemptRecord with
                    {
                        Status = OrchestrationNodeRunStatus.Completed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        Output = RedactContent(output.ToString()),
                        OutputSha256 = Hash(output.ToString()),
                        ToolCalls = OrchestrationContractCloner.ReadOnly(toolCalls)
                    };
                    EnsureOwnership(
                        runId,
                        await UpsertAttemptAsync(
                            runId,
                            attemptRecord,
                            cancellationToken));
                    EnsureOwnership(
                        runId,
                        await UpdateNodeAsync(runId, node.Id, current => current with
                    {
                        Status = OrchestrationNodeRunStatus.Completed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        OutputCharacters = output.Length
                    }));
                    return (true, output.ToString(), "");
                }
                attemptRecord = attemptRecord with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = lastError.Length == 0 ? "ORCHESTRATION_NODE_FAILED" : lastError,
                    Output = RedactContent(output.ToString()),
                    OutputSha256 = output.Length == 0 ? "" : Hash(output.ToString()),
                    ToolCalls = OrchestrationContractCloner.ReadOnly(toolCalls)
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        cancellationToken));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = "ORCHESTRATION_NODE_TIMEOUT";
                attemptRecord = attemptRecord with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = lastError
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        CancellationToken.None));
            }
            catch (AgentRuntimeException exception)
                when (exception.ErrorCode == OrchestrationErrorCodes.PayloadLimitExceeded)
            {
                attemptRecord = attemptRecord with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = exception.ErrorCode
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        CancellationToken.None));
                EnsureOwnership(
                    runId,
                    await UpdateNodeAsync(runId, node.Id, current => current with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = exception.ErrorCode
                }));
                throw;
            }
            if (attempt <= node.MaximumRetries) continue;
        }
        EnsureOwnership(
            runId,
            await UpdateNodeAsync(runId, node.Id, current => current with
        {
            Status = OrchestrationNodeRunStatus.Failed,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            ErrorCode = lastError.Length == 0 ? "ORCHESTRATION_NODE_FAILED" : lastError
        }));
        return (false, "", lastError);
    }
    #endregion

    #region 更新运行中编排的匹配节点（UpdateNodeAsync）
    /// <summary>
    /// 更新运行中编排的匹配节点（UpdateNodeAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="nodeId">编排节点标识。</param>
    /// <param name="update">应用到匹配节点的转换函数；未匹配的节点保持原样。</param>
    /// <returns>运行未退出活动管理且更新后读取到的状态仍为 Running 时返回 true，否则返回 false；true 不保证存在指定节点。</returns>
    private async Task<bool> UpdateNodeAsync(Guid runId, string nodeId, Func<OrchestrationNodeRunRecord, OrchestrationNodeRunRecord> update)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? value = await runs.GetAsync(runId);
            if (_retired.ContainsKey(runId)
                || value is null
                || value.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            await runs.SaveAsync(value with
            {
                Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                    node.NodeId == nodeId ? update(node) : node))
            });
            OrchestrationRunRecord? authoritative =
                await runs.GetAsync(runId, CancellationToken.None);
            return authoritative?.Status == OrchestrationRunStatus.Running;
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion

    #region 保存运行中编排的节点尝试记录（UpsertAttemptAsync）
    /// <summary>
    /// 保存运行中编排的节点尝试记录（UpsertAttemptAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="attempt">按节点标识和尝试序号替换或追加的尝试记录。</param>
    /// <param name="cancellationToken">用于取消等待运行互斥锁的令牌；取得锁后的读写使用 CancellationToken.None。</param>
    /// <returns>运行仍有效且详情成功保存时返回 true；运行已退出活动管理、不存在、不再运行或条件保存未生效时返回 false。</returns>
    private async Task<bool> UpsertAttemptAsync(Guid runId, OrchestrationNodeAttemptRecord attempt, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? run =
                await runs.GetAsync(
                    runId,
                    CancellationToken.None).ConfigureAwait(false);
            if (_retired.ContainsKey(runId)
                || run is null
                || run.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            OrchestrationRunDetails details =
                await runs.GetDetailsAsync(runId, CancellationToken.None) ??
                throw new InvalidOperationException("Run details disappeared.");
            var values = details.Attempts
                .Where(value =>
                    value.NodeId != attempt.NodeId
                    || value.Attempt != attempt.Attempt)
                .Append(attempt)
                .OrderBy(value => value.StartedAtUtc)
                .ThenBy(value => value.NodeId, StringComparer.Ordinal)
                .ThenBy(value => value.Attempt)
                .ToArray();
            return await runs.TrySaveRunningDetailsAsync(details with
            {
                Attempts = OrchestrationContractCloner.ReadOnly(values)
            }, CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion

    #region 保存运行中编排的输出详情（UpdateDetailsOutputAsync）
    /// <summary>
    /// 保存运行中编排的输出详情（UpdateDetailsOutputAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="output">执行输出内容。</param>
    /// <returns>运行仍有效且输出详情保存成功时返回 true；运行已退出活动管理、不存在、不再运行或条件保存未生效时返回 false。</returns>
    private async Task<bool> UpdateDetailsOutputAsync(Guid runId, string output)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? run =
                await runs.GetAsync(runId).ConfigureAwait(false);
            if (_retired.ContainsKey(runId)
                || run is null
                || run.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            OrchestrationRunDetails details =
                await runs.GetDetailsAsync(runId) ??
                throw new InvalidOperationException("Run details disappeared.");
            return await runs.TrySaveRunningDetailsAsync(
                details with { Output = output },
                CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion

    #region 尝试终结仍在运行的编排（FinishAsync）
    /// <summary>
    /// 尝试终结仍在运行的编排（FinishAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="status">拟保存的编排结束状态，同时决定节点终态及待执行节点的处理策略。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>底层仓储实际完成状态转换时返回 true；运行已退出活动管理、不存在、不再运行或未取得转换权时返回 false。</returns>
    private async Task<bool> FinishAsync(Guid runId, OrchestrationRunStatus status, string errorCode)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? value = await runs.GetAsync(runId);
            if (_retired.ContainsKey(runId)
                || value is null
                || value.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            OrchestrationNodeRunStatus nodeStatus =
                status switch
                {
                    OrchestrationRunStatus.Completed =>
                        OrchestrationNodeRunStatus.Completed,
                    OrchestrationRunStatus.Cancelled =>
                        OrchestrationNodeRunStatus.Cancelled,
                    _ => OrchestrationNodeRunStatus.Failed
                };
            DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
            OrchestrationRunTransitionResult result =
                await runs.TryFinalizeRunningAsync(
                runId,
                status,
                nodeStatus,
                status == OrchestrationRunStatus.Completed
                    ? OrchestrationTerminalTransitionPolicy.PreservePending
                    : OrchestrationTerminalTransitionPolicy.TerminalizePending,
                finishedAt,
                errorCode,
                detailsIfMissing: null,
                CancellationToken.None);
            return result.Transitioned;
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion

    #region 检查并终结宿主中断的编排运行（RecoverIfInterruptedAsync）
    /// <summary>
    /// 检查并终结宿主中断的编排运行（RecoverIfInterruptedAsync）。
    /// </summary>
    /// <param name="value">待检查的运行记录，可能为空。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>输入为空或已非 Running 时原样返回；对仍运行但本服务无活动执行的记录尝试持久化失败终态，并返回仓储记录，记录已消失时为 null。</returns>
    private async Task<OrchestrationRunRecord?> RecoverIfInterruptedAsync(OrchestrationRunRecord? value, CancellationToken cancellationToken)
    {
        if (value is null || value.Status != OrchestrationRunStatus.Running)
        {
            return value;
        }

        Guid runId = value.Id;
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? authoritative =
                await runs.GetAsync(
                    runId,
                    CancellationToken.None).ConfigureAwait(false);
            if (authoritative is null
                || authoritative.Status != OrchestrationRunStatus.Running
                || _active.ContainsKey(runId))
            {
                return authoritative;
            }

            const string recoveryErrorCode = "ORCHESTRATION_HOST_INTERRUPTED";
            DateTimeOffset recoveredAtUtc = DateTimeOffset.UtcNow;
            OrchestrationRunTransitionResult result =
                await runs.RecoverInterruptedAsync(
                    runId,
                    recoveredAtUtc,
                    recoveryErrorCode,
                    CancellationToken.None).ConfigureAwait(false);
            return result.Run;
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion

    #region 构建（BuildInput）
    /// <summary>
    /// 构建（BuildInput）
    /// </summary>
    /// <param name="node">编排节点。</param>
    /// <param name="initial">初始数据。</param>
    /// <param name="previous">先前状态。</param>
    /// <returns>根据节点输入模式选择初始输入、前序输出或替换模板占位符后的文本；未知模式使用初始输入。</returns>
    private static string BuildInput(OrchestrationNode node, string initial, string previous) =>
        node.InputMode switch
        {
            OrchestrationNodeInputMode.InitialInput => initial,
            OrchestrationNodeInputMode.PreviousOutput => previous,
            OrchestrationNodeInputMode.Template => (node.InputTemplate ?? "")
                .Replace("{{input}}", initial, StringComparison.Ordinal)
                .Replace("{{previous}}", previous, StringComparison.Ordinal),
            _ => initial
        };
    #endregion

    #region 按执行结果判断编排连线是否命中（Matches）
    /// <summary>
    /// 按执行结果判断编排连线是否命中（Matches）。
    /// </summary>
    /// <param name="edge">包含条件类型及可选条件值的编排连线。</param>
    /// <param name="succeeded">上游节点本次执行是否成功。</param>
    /// <param name="output">上游节点输出，供 OutputContains 条件匹配；空条件值匹配任意非 null 输出。</param>
    /// <returns>Always 始终返回 true，Succeeded 或 Failed 按执行结果判断，OutputContains 忽略大小写匹配输出；未知条件返回 false。</returns>
    private static bool Matches(OrchestrationEdge edge, bool succeeded, string output) =>
        edge.Condition switch
        {
            OrchestrationEdgeCondition.Always => true,
            OrchestrationEdgeCondition.Succeeded => succeeded,
            OrchestrationEdgeCondition.Failed => !succeeded,
            OrchestrationEdgeCondition.OutputContains =>
                output.Contains(edge.ConditionValue ?? "", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    #endregion

    #region 检查是否存在（Hash）
    /// <summary>
    /// 检查是否存在（Hash）
    /// </summary>
    /// <param name="value">用于计算 SHA-256 摘要的原始文本。</param>
    /// <returns>输入文本 UTF-8 字节的 SHA-256 小写十六进制摘要。</returns>
    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    #endregion

    #region 脱敏（RedactContent）
    /// <summary>
    /// 脱敏（RedactContent）
    /// </summary>
    /// <param name="value">需要按执行载荷规则脱敏的文本。</param>
    /// <returns>按执行载荷脱敏规则处理后的文本。</returns>
    private static string RedactContent(string value) =>
        ExecutionPayloadRedactor.RedactJson(value);
    #endregion

    #region 检查前置条件（EnsureWithinLimit）
    /// <summary>
    /// 检查前置条件（EnsureWithinLimit）
    /// </summary>
    /// <param name="value">需要校验 UTF-8 字节长度的载荷文本。</param>
    /// <param name="maximum">允许的最大值。</param>
    /// <param name="payloadName">载荷名称，用于识别和错误提示。</param>
    private static void EnsureWithinLimit(string value, int maximum, string payloadName)
    {
        if (value.Length <= maximum) return;
        throw new AgentRuntimeException(
            OrchestrationErrorCodes.PayloadLimitExceeded,
            $"The {payloadName} exceeds the configured {maximum}-character limit.");
    }
    #endregion

    #region 处理（CacheOutput）
    /// <summary>
    /// 处理（CacheOutput）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="value">当前运行需要暂存的输出文本。</param>
    private void CacheOutput(Guid runId, string value)
    {
        _outputs[runId] = value.Length <= 65_536 ? value : value[..65_536];
        _outputOrder.Enqueue(runId);
        while (_outputOrder.Count > 100 && _outputOrder.TryDequeue(out Guid expired))
            _outputs.TryRemove(expired, out _);
    }
    #endregion

    #region 检查前置条件（EnsureOwnership）
    /// <summary>
    /// 检查前置条件（EnsureOwnership）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="mutationAccepted">是否已接受状态修改。</param>
    private void EnsureOwnership(Guid runId, bool mutationAccepted)
    {
        if (mutationAccepted)
        {
            return;
        }

        _retired[runId] = 0;
        _outputs.TryRemove(runId, out _);
        if (_active.TryGetValue(runId, out ActiveExecution? active))
        {
            try
            {
                active.Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The execution reached cleanup while ownership was being retired.
            }
        }
        throw new OrchestrationOwnershipLostException(runId);
    }
    #endregion

    #region 获取或创建单个编排运行的互斥锁（GetRunGate）
    /// <summary>
    /// 获取或创建单个编排运行的互斥锁（GetRunGate）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <returns>返回按运行标识缓存的单通道信号量；缓存中不存在时新建，不在本方法中等待或取得锁。</returns>
    private SemaphoreSlim GetRunGate(Guid runId) =>
        _runGates.GetOrAdd(runId, static _ => new SemaphoreSlim(1, 1));
    #endregion

    #region 取消（CancelRetiredExecutionBestEffort）
    /// <summary>
    /// 取消（CancelRetiredExecutionBestEffort）
    /// </summary>
    /// <param name="retiredExecution">已退出的执行对象。</param>
    private static void CancelRetiredExecutionBestEffort(ActiveExecution retiredExecution)
    {
        try
        {
            retiredExecution.Cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The execution completed cleanup after it was retired.
        }
        catch (AggregateException)
        {
            // Cancellation callbacks cannot invalidate the durable terminal state.
        }
    }
    #endregion

    #region 处理（ObserveRetiredExecution）
    /// <summary>
    /// 处理（ObserveRetiredExecution）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="execution">当前执行对象。</param>
    private void ObserveRetiredExecution(Guid runId, Task execution) =>
        _ = ObserveRetiredExecutionAsync(runId, execution);
    #endregion

    #region 处理（ObserveRetiredExecutionAsync）
    /// <summary>
    /// 处理（ObserveRetiredExecutionAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="execution">当前执行对象。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ObserveRetiredExecutionAsync(Guid runId, Task execution)
    {
        try
        {
            await execution.ConfigureAwait(false);
        }
        catch
        {
            // A forced terminal record is authoritative after retirement.
        }
        finally
        {
            _retired.TryRemove(runId, out _);
            _runGates.TryRemove(runId, out _);
        }
    }
    #endregion

    private sealed record ActiveExecution(
        CancellationTokenSource Cancellation,
        Task Execution);

    /// <summary>
    /// 表示编排运行已失去执行所有权。
    /// </summary>
    /// <param name="runId">关联的运行记录标识。</param>
    private sealed class OrchestrationOwnershipLostException(Guid runId) :
        Exception($"Orchestration run '{runId}' is no longer owned by this runtime.");
}

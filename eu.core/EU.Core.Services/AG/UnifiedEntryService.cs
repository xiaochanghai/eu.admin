using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

// 文件职责：UnifiedEntryService 职责实现

/// <summary>
/// 提供 Agent 平台的统一会话和执行入口。
/// </summary>
public sealed class UnifiedEntryService
{
    private const int MaximumPendingDeltaEvents = 4;
    private static readonly TimeSpan MaximumDeltaPersistenceInterval =
        TimeSpan.FromMilliseconds(75);
    private const int MaximumStoredPayloadBytes =
        AgentRuntimeService.MaximumInputCharacters * 4;
    /// <summary>运行时读取的最大历史消息数量。</summary>
    public const int MaximumConversationHistoryMessages = 40;
    /// <summary>运行时读取的历史消息最大 UTF-8 字节数。</summary>
    public const int MaximumConversationHistoryUtf8Bytes = 65_536;
    private readonly IMainAgentAssignmentService _mainAgents;
    private readonly IAgentRuntimeService _agentRuntime;
    private readonly OrchestrationRuntimeService _orchestrationRuntime;
    private readonly IUnifiedEntryRepository _repository;
    private readonly UnifiedEntryLimits _limits;
    private readonly TimeProvider _timeProvider;
    private readonly BusinessQueryToolPolicy? _businessQueryPolicy;
    private readonly BusinessQueryResultLimits _businessQueryResultLimits;
    private readonly IAgentToolApprovalHandler? _toolApprovalHandler;
    private readonly ConcurrentDictionary<Guid, ActiveUnifiedEntryExecution> _active = [];

    internal Action? BeforeRuntimeOwnershipClaim { get; set; }

    #region 构造（UnifiedEntryService）
    /// <summary>
    /// 构造（UnifiedEntryService）
    /// </summary>
    /// <param name="mainAgents">主 Agent 服务。</param>
    /// <param name="agentRuntime">Agent 运行时服务。</param>
    /// <param name="orchestrationRuntime">编排运行时服务。</param>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="limits">执行次数、时间或载荷的限制配置。</param>
    /// <param name="timeProvider">用于读取当前时间的时间提供器。</param>
    /// <param name="businessQueryPolicy">受控业务查询的工具调用策略。</param>
    /// <param name="businessQueryResultLimits">业务查询结果的载荷限制。</param>
    /// <param name="toolApprovalHandler">工具调用审批处理器。</param>
    public UnifiedEntryService(
        IMainAgentAssignmentService mainAgents,
        IAgentRuntimeService agentRuntime,
        OrchestrationRuntimeService orchestrationRuntime,
        IUnifiedEntryRepository repository,
        UnifiedEntryLimits? limits = null,
        TimeProvider? timeProvider = null,
        BusinessQueryToolPolicyAccessor? businessQueryPolicy = null,
        BusinessQueryResultLimits? businessQueryResultLimits = null,
        IAgentToolApprovalHandler? toolApprovalHandler = null)
    {
        _mainAgents = mainAgents
            ?? throw new ArgumentNullException(nameof(mainAgents));
        _agentRuntime = agentRuntime
            ?? throw new ArgumentNullException(nameof(agentRuntime));
        _orchestrationRuntime = orchestrationRuntime
            ?? throw new ArgumentNullException(nameof(orchestrationRuntime));
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        _limits = limits ?? UnifiedEntryLimits.Default;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _businessQueryPolicy = businessQueryPolicy?.Policy;
        _businessQueryResultLimits = businessQueryResultLimits
            ?? BusinessQueryResultLimits.Default;
        _toolApprovalHandler = toolApprovalHandler;
    }
    #endregion

    #region 准备（PrepareAsync）
    /// <summary>
    /// 准备（PrepareAsync）
    /// </summary>
    /// <param name="input">执行输入内容。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>统一入口运行准备结果，成功时包含执行上下文，失败时包含错误信息。</returns>
    public async Task<UnifiedEntryPreparationResult> PrepareAsync(string? input, Guid? conversationId, CancellationToken cancellationToken = default) =>
        await PrepareCoreAsync(
            input, conversationId, null, null, null, cancellationToken);
    #endregion

    #region 准备（PrepareAsync）
    /// <summary>
    /// 准备（PrepareAsync）
    /// </summary>
    /// <param name="input">执行输入内容。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="executionIdentity">当前执行使用的用户、租户及权限身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>绑定指定执行身份的统一入口运行准备结果，成功时包含执行上下文，失败时包含错误信息。</returns>
    public async Task<UnifiedEntryPreparationResult> PrepareAsync(
        string? input,
        Guid? conversationId,
        AgentExecutionIdentity executionIdentity,
        CancellationToken cancellationToken = default) =>
        await PrepareCoreAsync(
            input,
            conversationId,
            executionIdentity ?? throw new ArgumentNullException(nameof(executionIdentity)),
            null,
            null,
            cancellationToken);
    #endregion

    #region 准备（PrepareEvaluationAsync）
    /// <summary>
    /// 准备（PrepareEvaluationAsync）
    /// </summary>
    /// <param name="input">执行输入内容。</param>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="executionIdentity">当前执行使用的用户、租户及权限身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>为评测固定 Agent 及版本创建的新会话运行准备结果，或对应的准备失败信息。</returns>
    internal async Task<UnifiedEntryPreparationResult> PrepareEvaluationAsync(
        string? input,
        Guid agentId,
        Guid agentVersionId,
        AgentExecutionIdentity executionIdentity,
        CancellationToken cancellationToken = default) =>
        await PrepareCoreAsync(
            input,
            null,
            executionIdentity ?? throw new ArgumentNullException(nameof(executionIdentity)),
            agentId,
            agentVersionId,
            cancellationToken);
    #endregion

    #region 准备（PrepareCoreAsync）
    /// <summary>
    /// 准备（PrepareCoreAsync）
    /// </summary>
    /// <param name="input">执行输入内容。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="executionIdentity">当前执行使用的用户、租户及权限身份。</param>
    /// <param name="evaluationAgentId">参与评估的 Agent 标识。</param>
    /// <param name="evaluationAgentVersionId">参与评估的 Agent 版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>完成输入、会话、Agent 准备及聚合持久化后的执行上下文，或对应失败信息；调用方取消异常向上传播。</returns>
    private async Task<UnifiedEntryPreparationResult> PrepareCoreAsync(
        string? input,
        Guid? conversationId,
        AgentExecutionIdentity? executionIdentity,
        Guid? evaluationAgentId,
        Guid? evaluationAgentVersionId,
        CancellationToken cancellationToken)
    {
        string normalizedInput = input?.Trim() ?? string.Empty;
        if (normalizedInput.Length is 0 or > AgentRuntimeService.MaximumInputCharacters)
        {
            return UnifiedEntryPreparationResult.Failure(
                AgentRunErrorCodes.InputInvalid,
                $"Run input must contain from 1 through {AgentRuntimeService.MaximumInputCharacters} characters.");
        }

        ConversationRecord? existingConversation = null;
        IReadOnlyList<ConversationMessageRecord> priorMessages = [];
        if (conversationId.HasValue)
        {
            existingConversation = executionIdentity is null
                ? await _repository.GetConversationAsync(
                    conversationId.Value,
                    cancellationToken).ConfigureAwait(false)
                : await _repository.GetConversationForOwnerAsync(
                    conversationId.Value,
                    executionIdentity.TenantId,
                    executionIdentity.UserId,
                    cancellationToken).ConfigureAwait(false);
            if (existingConversation is null)
            {
                return UnifiedEntryPreparationResult.Failure(
                    UnifiedEntryErrorCodes.ConversationNotFound,
                    "The requested conversation was not found.");
            }

            priorMessages = executionIdentity is null
                ? await _repository.ListMessagesAsync(
                    existingConversation.Id,
                    MaximumConversationHistoryMessages,
                    cancellationToken).ConfigureAwait(false)
                : await _repository.ListMessagesForOwnerAsync(
                    existingConversation.Id,
                    executionIdentity.TenantId,
                    executionIdentity.UserId,
                    MaximumConversationHistoryMessages,
                    cancellationToken).ConfigureAwait(false);
        }

        MainAgentAssignment assignment;
        if (evaluationAgentId.HasValue || evaluationAgentVersionId.HasValue)
        {
            if (!evaluationAgentId.HasValue
                || evaluationAgentId.Value == Guid.Empty
                || !evaluationAgentVersionId.HasValue
                || evaluationAgentVersionId.Value == Guid.Empty)
            {
                return UnifiedEntryPreparationResult.Failure(
                    AgentRunErrorCodes.AgentNotFound,
                    "The evaluation target is invalid.");
            }

            assignment = new MainAgentAssignment(
                evaluationAgentId.Value,
                evaluationAgentVersionId.Value,
                0,
                _timeProvider.GetUtcNow());
        }
        else
        {
            ServiceResult<MainAgentAssignment> assignmentResult =
                await _mainAgents.GetAsync(cancellationToken).ConfigureAwait(false);
            if (!assignmentResult.Success)
            {
                return UnifiedEntryPreparationResult.Failure(
                    MainAgentServiceStatusCodes.ToErrorCode(assignmentResult.Status),
                    assignmentResult.Message);
            }

            assignment = assignmentResult.Data!;
        }
        AgentRunPreparationResult prepared =
            await _agentRuntime.PrepareVersionAsync(
                assignment.AgentId,
                assignment.AgentVersionId,
                normalizedInput,
                cancellationToken).ConfigureAwait(false);
        if (!prepared.Succeeded)
        {
            return UnifiedEntryPreparationResult.Failure(
                prepared.Error!.Code,
                prepared.Error.Message);
        }

        AgentRunContext preparedMainContext = prepared.Context!;
        CancellationTokenSource? executionCancellation = null;
        UnifiedEntryExecutionScope? scope = null;
        UnifiedAgentExecutionLease? mainLease = null;
        bool activeAdded = false;
        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Guid resolvedConversationId =
                existingConversation?.Id ?? Guid.NewGuid();
            ProtectedUnifiedPayload protectedInput = Protect(normalizedInput);
            ConversationRecord conversation = existingConversation is null
                ? new ConversationRecord(
                    resolvedConversationId,
                    CreateConversationTitle(protectedInput.Content),
                    now,
                    now)
                {
                    TenantId = executionIdentity?.TenantId
                        ?? UnifiedEntryOwnership.LegacyUnowned,
                    UserId = executionIdentity?.UserId
                        ?? UnifiedEntryOwnership.LegacyUnowned
                }
                : existingConversation with { UpdatedAtUtc = now };
            var userMessage = new ConversationMessageRecord(
                Guid.NewGuid(),
                resolvedConversationId,
                ConversationMessageRole.User,
                protectedInput.Content,
                protectedInput.OriginalSha256,
                protectedInput.OriginalUtf8Bytes,
                now)
            {
                Kind = ConversationMessageKind.UserInput
            };
            Guid entryRunId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();
            var entryRun = new UnifiedEntryRunRecord(
                entryRunId,
                resolvedConversationId,
                correlationId,
                assignment.AgentVersionId,
                UnifiedRunStatus.Running,
                now,
                null,
                null,
                protectedInput.Content,
                protectedInput.OriginalSha256,
                string.Empty,
                string.Empty,
                string.Empty)
            {
                TenantId = executionIdentity?.TenantId
                    ?? UnifiedEntryOwnership.LegacyUnowned,
                UserId = executionIdentity?.UserId
                    ?? UnifiedEntryOwnership.LegacyUnowned
            };
            var mainRun = new UnifiedAgentRunRecord(
                preparedMainContext.RunId,
                entryRunId,
                null,
                UnifiedAgentRunKind.Main,
                assignment.AgentId,
                assignment.AgentVersionId,
                0,
                UnifiedRunStatus.Running,
                preparedMainContext.StartedAtUtc,
                null,
                null,
                protectedInput.Content,
                protectedInput.OriginalSha256,
                string.Empty,
                string.Empty,
                string.Empty);
            var aggregate = new UnifiedEntryAggregate(
                conversation,
                priorMessages.Append(userMessage).ToArray(),
                new UnifiedRunDetails(entryRun, [mainRun], [], []),
                []);

            executionCancellation = new CancellationTokenSource();
            scope = new UnifiedEntryExecutionScope(
                aggregate,
                _limits,
                correlationId,
                executionCancellation.Token,
                _timeProvider);
            mainLease = scope.EnterMainAgent(assignment.AgentVersionId);
            var internalTools = new List<IAgentInternalTool>();
            if (preparedMainContext.Skills.Count > 0)
            {
                internalTools.Add(new UseSkillTool(preparedMainContext.Skills));
            }

            if (preparedMainContext.Snapshot.ChildAgents.Count > 0)
            {
                internalTools.Add(new DelegateToAgentTool(
                    _agentRuntime,
                    scope,
                    preparedMainContext.Snapshot,
                    mainLease,
                    preparedMainContext.RunId,
                    executionIdentity,
                    _businessQueryPolicy,
                    _toolApprovalHandler,
                    new AgentToolApprovalBinding(
                        resolvedConversationId,
                        entryRunId)));
            }

            if (preparedMainContext.Snapshot.Orchestrations.Count > 0)
            {
                internalTools.Add(new RunOrchestrationTool(
                    _orchestrationRuntime,
                    scope,
                    preparedMainContext.Snapshot,
                    mainLease,
                    preparedMainContext.RunId,
                    executionIdentity,
                    _toolApprovalHandler,
                    new AgentToolApprovalBinding(
                        resolvedConversationId,
                        entryRunId)));
            }

            AgentRunContext mainContext = preparedMainContext with
            {
                ConversationHistory = BuildConversationHistory(priorMessages),
                Skills = [],
                Knowledge = preparedMainContext.Knowledge,
                InternalTools = internalTools,
                McpCallGuard = scope,
                McpResultGuard = scope,
                McpToolCallLimits = BusinessQueryMcpToolCallLimits.Create(
                    _businessQueryPolicy,
                    preparedMainContext.Tools),
                ExecutionIdentity = executionIdentity,
                ToolApprovalBinding = new AgentToolApprovalBinding(
                    resolvedConversationId,
                    entryRunId),
                ToolApprovalHandler = _toolApprovalHandler
            };
            var active = new ActiveUnifiedEntryExecution(
                entryRunId,
                resolvedConversationId,
                executionCancellation,
                scope,
                mainLease,
                mainContext);

            await scope.PersistAsync(_repository, cancellationToken)
                .ConfigureAwait(false);
            if (!_active.TryAdd(entryRunId, active))
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.InvalidState,
                    "A unified entry run with the generated identity already exists.");
            }

            activeAdded = true;
            _ = ObserveUnstartedTimeoutAsync(active);
            return UnifiedEntryPreparationResult.Success(
                new UnifiedEntryContext(mainContext, active));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TerminatePreparedAuditAsync(
                preparedMainContext,
                AgentRunStatus.Cancelled,
                UnifiedEntryErrorCodes.Cancelled).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception)
        {
            await TerminatePreparedAuditAsync(
                preparedMainContext,
                AgentRunStatus.Failed,
                exception is UnifiedEntryException unified
                    ? unified.ErrorCode
                    : UnifiedEntryErrorCodes.PersistenceFailed).ConfigureAwait(false);
            return UnifiedEntryPreparationResult.Failure(
                exception is UnifiedEntryException known
                    ? known.ErrorCode
                    : UnifiedEntryErrorCodes.PersistenceFailed,
                "The unified entry run could not be prepared.");
        }
        finally
        {
            if (!activeAdded)
            {
                mainLease?.Dispose();
                scope?.Dispose();
                executionCancellation?.Dispose();
            }
        }
    }
    #endregion

    #region 流式输出（StreamAsync）
    /// <summary>
    /// 流式输出（StreamAsync）
    /// </summary>
    /// <param name="context">统一入口执行上下文，包含执行范围和主 Agent 运行信息。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按执行顺序产生的异步事件流。</returns>
    public IAsyncEnumerable<UnifiedRunEvent> StreamAsync(UnifiedEntryContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new UnifiedEntryEventEnumerable(this, context, cancellationToken);
    }
    #endregion

    #region 流式输出（StreamStartedAsync）
    /// <summary>
    /// 流式输出（StreamStartedAsync）
    /// </summary>
    /// <param name="context">统一入口执行上下文，包含执行范围和主 Agent 运行信息。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按执行顺序产生的异步事件流。</returns>
    private async IAsyncEnumerable<UnifiedRunEvent> StreamStartedAsync(
        UnifiedEntryContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ActiveUnifiedEntryExecution active = context.Execution;
        var channel = Channel.CreateUnbounded<UnifiedRunEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
        Task producer = ProduceStreamAsync(
            context,
            active,
            channel.Writer,
            cancellationToken);
        try
        {
            await foreach (UnifiedRunEvent value in channel.Reader
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return value;
            }
        }
        finally
        {
            TryCancel(active.Cancellation);
            await producer.ConfigureAwait(false);
        }
    }
    #endregion

    /// <summary>
    /// 封装统一入口运行事件的异步枚举序列。
    /// </summary>
    /// <param name="owner">拥有本次运行并提供事件流的统一入口服务。</param>
    /// <param name="context">本次统一入口运行的执行上下文。</param>
    /// <param name="invocationCancellation">用于取消统一入口调用的令牌。</param>
    private sealed class UnifiedEntryEventEnumerable(
        UnifiedEntryService owner,
        UnifiedEntryContext context,
        CancellationToken invocationCancellation) : IAsyncEnumerable<UnifiedRunEvent>
    {
        #region 获取（GetAsyncEnumerator）
        /// <summary>
        /// 获取（GetAsyncEnumerator）
        /// </summary>
        /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
        /// <returns>持有当前执行流所有权的异步事件枚举器；运行不再活动或已被枚举时抛出 InvalidState 异常。</returns>
        public IAsyncEnumerator<UnifiedRunEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            ActiveUnifiedEntryExecution active = context.Execution;
            if (!owner._active.TryGetValue(
                    active.RunId,
                    out ActiveUnifiedEntryExecution? current)
                || !ReferenceEquals(current, active)
                || !active.TryStartStream())
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.InvalidState,
                    "The unified entry run is not active or has already been streamed.");
            }

            return new UnifiedEntryEventEnumerator(
                owner,
                context,
                invocationCancellation,
                cancellationToken);
        }
        #endregion
    }

    private sealed class UnifiedEntryEventEnumerator
        : IAsyncEnumerator<UnifiedRunEvent>
    {
        private readonly ActiveUnifiedEntryExecution _active;
        private readonly CancellationTokenSource _cancellation;
        private readonly CancellationToken _joinCancellationToken;
        private readonly UnifiedEntryService _owner;
        private readonly IAsyncEnumerator<UnifiedRunEvent> _inner;
        private int _disposed;
        private int _ownershipAttempted;
        private volatile bool _runtimeOwned;

        #region 构造（UnifiedEntryEventEnumerator）
        /// <summary>
        /// 构造（UnifiedEntryEventEnumerator）
        /// </summary>
        /// <param name="owner">所属执行对象。</param>
        /// <param name="context">枚举器所属的统一入口上下文，提供活动执行和主 Agent 运行信息。</param>
        /// <param name="invocationCancellation">调用过程使用的取消控制对象。</param>
        /// <param name="enumerationCancellation">枚举过程使用的取消控制对象。</param>
        public UnifiedEntryEventEnumerator(
            UnifiedEntryService owner,
            UnifiedEntryContext context,
            CancellationToken invocationCancellation,
            CancellationToken enumerationCancellation)
        {
            _owner = owner;
            _active = context.Execution;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                invocationCancellation,
                enumerationCancellation);
            _joinCancellationToken = _cancellation.Token;
            _inner = owner.StreamStartedAsync(context, _cancellation.Token)
                .GetAsyncEnumerator();
        }
        #endregion

        /// <summary>
        /// 获取枚举器当前指向的统一入口运行事件。
        /// </summary>
        public UnifiedRunEvent Current => _inner.Current;

        #region 移动到下一个运行事件（MoveNextAsync）
        /// <summary>
        /// 首次枚举时尝试取得运行所有权，并移动到下一个运行事件（MoveNextAsync）。
        /// </summary>
        /// <returns>异步操作结果：存在下一个事件时返回 true；事件流结束或未取得运行所有权时返回 false。</returns>
        public ValueTask<bool> MoveNextAsync()
        {
            if (Interlocked.Exchange(ref _ownershipAttempted, 1) == 0)
            {
                _owner.BeforeRuntimeOwnershipClaim?.Invoke();
                if (!_active.TryClaimRuntimeOwnership())
                {
                    return new ValueTask<bool>(
                        _owner.JoinRuntimeOwnershipLossAsync(
                            _active,
                            _joinCancellationToken));
                }

                _runtimeOwned = true;
            }

            return _inner.MoveNextAsync();
        }
        #endregion

        #region 释放资源（DisposeAsync）
        /// <summary>
        /// 释放资源（DisposeAsync）
        /// </summary>
        /// <returns>表示该异步操作完成的任务。</returns>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                if (!_runtimeOwned)
                {
                    TryCancel(_active.Cancellation);
                    if (_active.TryClaimPreparedFinalization())
                    {
                        await _owner.FinalizeOwnedAsync(
                            _active,
                            UnifiedRunStatus.Cancelled,
                            string.Empty,
                            UnifiedEntryErrorCodes.Cancelled,
                            afterSequence: 0,
                            terminatePreparedAudit: true).ConfigureAwait(false);
                    }
                    else
                    {
                        await _owner.JoinOrRetryFinalizationAsync(
                            _active,
                            afterSequence: 0).ConfigureAwait(false);
                    }
                }

                await _inner.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _cancellation.Dispose();
            }
        }
        #endregion
    }

    #region 处理（ProduceStreamAsync）
    /// <summary>
    /// 处理（ProduceStreamAsync）
    /// </summary>
    /// <param name="context">统一入口执行上下文，包含执行范围和主 Agent 运行信息。</param>
    /// <param name="active">当前活动状态。</param>
    /// <param name="writer">用于输出 JSON 内容的写入器。</param>
    /// <param name="consumerCancellationToken">消费方取消流式读取的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ProduceStreamAsync(
        UnifiedEntryContext context,
        ActiveUnifiedEntryExecution active,
        ChannelWriter<UnifiedRunEvent> writer,
        CancellationToken consumerCancellationToken)
    {
        string output = string.Empty;
        string terminalErrorCode = string.Empty;
        UnifiedRunStatus terminalStatus = UnifiedRunStatus.Cancelled;
        bool terminalRequested = false;
        bool waitingForApproval = false;
        bool routeSelected = false;
        long yieldedSequence = 0;
        IAsyncEnumerator<AgentRunEvent>? enumerator = null;
        using var effectiveCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                consumerCancellationToken,
                active.Scope.EntryCancellationToken);
        try
        {
            await AppendEventAsync(
                active,
                "run-started",
                active.Scope.GetAggregateSnapshotAsync(CancellationToken.None),
                null,
                depth: 0,
                CancellationToken.None).ConfigureAwait(false);
            foreach (UnifiedRunEvent value in await PersistAndCollectAsync(
                         active,
                         yieldedSequence).ConfigureAwait(false))
            {
                yieldedSequence = value.Sequence;
                await writer.WriteAsync(value, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            enumerator = _agentRuntime
                .StreamAsync(context.MainAgentContext, effectiveCancellation.Token)
                .GetAsyncEnumerator(effectiveCancellation.Token);
            Task<bool> moveNext = enumerator.MoveNextAsync().AsTask();
            int pendingDeltaEvents = 0;
            DateTimeOffset lastPersistenceAt = _timeProvider.GetUtcNow();
            while (true)
            {
                if (pendingDeltaEvents > 0)
                {
                    TimeSpan remaining = MaximumDeltaPersistenceInterval
                        - (_timeProvider.GetUtcNow() - lastPersistenceAt);
                    if (remaining > TimeSpan.Zero)
                    {
                        Task interval = Task.Delay(
                            remaining,
                            effectiveCancellation.Token);
                        Task completed = await Task.WhenAny(moveNext, interval)
                            .ConfigureAwait(false);
                        if (completed == interval)
                        {
                            await interval.ConfigureAwait(false);
                        }
                    }

                    if (!moveNext.IsCompleted)
                    {
                        foreach (UnifiedRunEvent value in await PersistAndCollectAsync(
                                     active,
                                     yieldedSequence).ConfigureAwait(false))
                        {
                            yieldedSequence = value.Sequence;
                            await writer.WriteAsync(value, CancellationToken.None)
                                .ConfigureAwait(false);
                        }

                        pendingDeltaEvents = 0;
                        lastPersistenceAt = _timeProvider.GetUtcNow();
                        continue;
                    }
                }

                if (!await moveNext.ConfigureAwait(false))
                {
                    break;
                }

                AgentRunEvent source = enumerator.Current;
                string? route = source.Kind switch
                {
                    AgentRunEventKind.ToolStarted =>
                        ClassifyToolRoute(source.ToolName),
                    AgentRunEventKind.Delta => "direct",
                    _ => null
                };
                if (!routeSelected && route is not null)
                {
                    await AppendRouteEventAsync(active, route)
                        .ConfigureAwait(false);
                    routeSelected = true;
                }

                if (source.Kind == AgentRunEventKind.Delta)
                {
                    output += source.Text;
                    pendingDeltaEvents++;
                }

                if (source.Kind == AgentRunEventKind.ApprovalRequired)
                {
                    if (source.ApprovalId is not Guid approvalId
                        || approvalId == Guid.Empty)
                    {
                        throw new UnifiedEntryException(
                            UnifiedEntryErrorCodes.InvalidState,
                            "An approval-required event must identify its approval.");
                    }

                    waitingForApproval = true;
                }

                if (source.Kind is AgentRunEventKind.Completed
                    or AgentRunEventKind.Failed
                    or AgentRunEventKind.Cancelled)
                {
                    terminalStatus = source.Kind switch
                    {
                        AgentRunEventKind.Completed => UnifiedRunStatus.Completed,
                        AgentRunEventKind.Failed => UnifiedRunStatus.Failed,
                        _ => UnifiedRunStatus.Cancelled
                    };
                    terminalErrorCode = source.Kind switch
                    {
                        AgentRunEventKind.Failed when source.ErrorCode.Length > 0 =>
                            source.ErrorCode,
                        AgentRunEventKind.Failed => AgentRunErrorCodes.ModelFailed,
                        AgentRunEventKind.Cancelled => UnifiedEntryErrorCodes.Cancelled,
                        _ => string.Empty
                    };
                    terminalRequested = true;
                }
                else
                {
                    await PersistMainEventAsync(
                        active,
                        source,
                        output,
                        effectiveCancellation.Token).ConfigureAwait(false);

                    if (source.Kind == AgentRunEventKind.ToolFailed
                        && IsFatalPlatformFailure(source.ErrorCode))
                    {
                        terminalStatus = UnifiedRunStatus.Failed;
                        terminalErrorCode = source.ErrorCode;
                        terminalRequested = true;
                        TryCancel(active.Cancellation);
                    }
                }

                bool persistNow = source.Kind != AgentRunEventKind.Delta
                    || pendingDeltaEvents >= MaximumPendingDeltaEvents
                    || terminalRequested
                    || waitingForApproval;
                if (persistNow)
                {
                    foreach (UnifiedRunEvent value in await PersistAndCollectAsync(
                                 active,
                                 yieldedSequence).ConfigureAwait(false))
                    {
                        yieldedSequence = value.Sequence;
                        await writer.WriteAsync(value, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    pendingDeltaEvents = 0;
                    lastPersistenceAt = _timeProvider.GetUtcNow();
                }

                if (terminalRequested)
                {
                    break;
                }

                if (waitingForApproval)
                {
                    break;
                }

                moveNext = enumerator.MoveNextAsync().AsTask();
            }

            if (!terminalRequested && !waitingForApproval)
            {
                effectiveCancellation.Token.ThrowIfCancellationRequested();
                terminalStatus = UnifiedRunStatus.Completed;
            }
        }
        catch (OperationCanceledException)
        {
            terminalStatus = active.Scope.ClassifyCancellation(
                    active.MainLease,
                    consumerCancellationToken) == UnifiedEntryErrorCodes.EntryTimeout
                ? UnifiedRunStatus.Failed
                : UnifiedRunStatus.Cancelled;
            terminalErrorCode = active.Scope.ClassifyCancellation(
                active.MainLease,
                consumerCancellationToken);
        }
        catch (Exception exception)
        {
            waitingForApproval = false;
            terminalStatus = UnifiedRunStatus.Failed;
            terminalErrorCode = exception switch
            {
                UnifiedEntryException unified => unified.ErrorCode,
                AgentRuntimeException runtime => runtime.ErrorCode,
                _ => AgentRunErrorCodes.ModelFailed
            };
            if (terminalErrorCode is UnifiedEntryErrorCodes.PayloadLimitExceeded
                or UnifiedEntryErrorCodes.PayloadInvalidEncoding)
            {
                output = string.Empty;
            }
        }
        finally
        {
            TryCancel(active.Cancellation);
            if (enumerator is not null)
            {
                try
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception) when (
                    exception is OperationCanceledException
                    || terminalStatus != UnifiedRunStatus.Completed)
                {
                    if (terminalErrorCode.Length == 0)
                    {
                        terminalStatus = UnifiedRunStatus.Cancelled;
                        terminalErrorCode = UnifiedEntryErrorCodes.Cancelled;
                    }
                }
            }
        }

        if (waitingForApproval)
        {
            try
            {
                await active.Scope.PersistAsync(
                    _repository,
                    CancellationToken.None).ConfigureAwait(false);
                active.PrimaryFinalizationCompleted.TrySetResult();
                Retire(active);
            }
            finally
            {
                writer.TryComplete();
            }

            return;
        }

        try
        {
            IReadOnlyList<UnifiedRunEvent> terminalEvents =
                active.TryClaimRuntimeFinalization()
                    ? await FinalizeOwnedAsync(
                        active,
                        terminalStatus,
                        output,
                        terminalErrorCode,
                        yieldedSequence).ConfigureAwait(false)
                    : await JoinOrRetryFinalizationAsync(
                        active,
                        yieldedSequence).ConfigureAwait(false);
            foreach (UnifiedRunEvent value in terminalEvents)
            {
                await writer.WriteAsync(value, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }
    #endregion

    #region 请求取消统一入口运行并等待收尾（CancelAsync）
    /// <summary>
    /// 请求取消统一入口运行并等待收尾（CancelAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步操作结果：活动运行已处理取消并完成收尾，或存储记录已处于 Completed、Failed、Cancelled 状态时返回 true；未找到匹配运行或运行尚未终结时返回 false。</returns>
    public async Task<bool> CancelAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await CancelCoreAsync(runId, null, cancellationToken);
    #endregion

    #region 校验调用方归属后取消统一入口运行（CancelAsync）
    /// <summary>
    /// 校验调用方归属后取消统一入口运行（CancelAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="executionIdentity">用于校验运行租户和用户归属的非空执行身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步操作结果：归属匹配且活动运行完成取消收尾，或归属匹配的存储记录已处于终态时返回 true；归属不匹配、记录不存在或存储记录尚未终结时返回 false。</returns>
    public async Task<bool> CancelAsync(Guid runId, AgentExecutionIdentity executionIdentity, CancellationToken cancellationToken = default) =>
        await CancelCoreAsync(
            runId,
            executionIdentity ?? throw new ArgumentNullException(nameof(executionIdentity)),
            cancellationToken);
    #endregion

    #region 按可选执行身份取消运行并等待收尾（CancelCoreAsync）
    /// <summary>
    /// 按可选执行身份取消运行并等待收尾（CancelCoreAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="executionIdentity">用于校验运行归属的执行身份；为 null 时不按调用方身份筛选。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步操作结果：活动运行已处理取消并完成收尾，或匹配的存储记录已处于终态时返回 true；归属不匹配、记录不存在或存储记录尚未终结时返回 false。</returns>
    private async Task<bool> CancelCoreAsync(Guid runId, AgentExecutionIdentity? executionIdentity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_active.TryGetValue(runId, out ActiveUnifiedEntryExecution? active))
        {
            if (executionIdentity is not null
                && !SameOwner(active.MainContext.ExecutionIdentity, executionIdentity))
            {
                return false;
            }

            TryCancel(active.Cancellation);
            if (active.TryClaimPreparedFinalization())
            {
                await FinalizeOwnedAsync(
                    active,
                    UnifiedRunStatus.Cancelled,
                    string.Empty,
                    UnifiedEntryErrorCodes.Cancelled,
                    afterSequence: 0,
                    terminatePreparedAudit: true).ConfigureAwait(false);
            }
            else
            {
                await JoinOrRetryFinalizationAsync(
                    active,
                    afterSequence: 0).ConfigureAwait(false);
            }

            return true;
        }

        UnifiedEntryRunRecord? existing = executionIdentity is null
            ? await _repository.GetRunAsync(runId, cancellationToken)
                .ConfigureAwait(false)
            : await _repository.GetRunForOwnerAsync(
                runId,
                executionIdentity.TenantId,
                executionIdentity.UserId,
                cancellationToken).ConfigureAwait(false);
        return existing?.Status is UnifiedRunStatus.Completed
            or UnifiedRunStatus.Failed
            or UnifiedRunStatus.Cancelled;
    }
    #endregion

    #region 比较执行身份的租户和用户（SameOwner）
    /// <summary>
    /// 比较执行身份的租户和用户（SameOwner）。
    /// </summary>
    /// <param name="stored">运行中保存的执行身份。</param>
    /// <param name="requested">请求操作该运行的执行身份。</param>
    /// <returns>已存储身份非 null，且租户和用户标识均按区分大小写的方式匹配时返回 true，否则返回 false。</returns>
    private static bool SameOwner(AgentExecutionIdentity? stored, AgentExecutionIdentity requested) =>
        stored is not null
        && string.Equals(stored.TenantId, requested.TenantId, StringComparison.Ordinal)
        && string.Equals(stored.UserId, requested.UserId, StringComparison.Ordinal);
    #endregion

    #region 处理（PersistMainEventAsync）
    /// <summary>
    /// 处理（PersistMainEventAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="source">源数据。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task PersistMainEventAsync(ActiveUnifiedEntryExecution active, AgentRunEvent source, string output, CancellationToken cancellationToken)
    {
        string rawPayloadJson = JsonSerializer.Serialize(new
        {
            agentRunId = source.RunId,
            eventKind = source.Kind.ToString(),
            text = source.Text,
            source.ArgumentsJson,
            source.ErrorCode,
            source.ToolVersionId,
            source.ToolName,
            source.ToolCallId,
            source.SkillVersionId,
            source.SkillName,
            source.ApprovalId,
            source.KnowledgeBaseCount,
            source.KnowledgeHitCount
        });
        ProtectedUnifiedPayload rawPayload = Protect(rawPayloadJson);
        ProtectedUnifiedPayload persistedPayload = Protect(JsonSerializer.Serialize(new
        {
            agentRunId = source.RunId,
            eventKind = source.Kind.ToString(),
            text = Protect(source.Text).Content,
            argumentsJson = Protect(source.ArgumentsJson).Content,
            source.ErrorCode,
            source.ToolVersionId,
            source.ToolName,
            source.ToolCallId,
            source.SkillVersionId,
            source.SkillName,
            source.ApprovalId,
            source.KnowledgeBaseCount,
            source.KnowledgeHitCount
        }));
        string kind = MapMainEventKind(source);
        await active.Scope.MutateAggregateAndAppendEventAsync(
            aggregate => ApplyMainEvent(aggregate, source, output),
            (aggregate, sequence) => new UnifiedRunEventRecord(
                Guid.NewGuid(),
                aggregate.Details.EntryRun.Id,
                sequence,
                active.Scope.CorrelationId,
                kind,
                source.OccurredAtUtc,
                null,
                0,
                persistedPayload.Content,
                rawPayload.OriginalSha256),
            cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region 处理（ApplyMainEvent）
    /// <summary>
    /// 处理（ApplyMainEvent）
    /// </summary>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="source">源数据。</param>
    /// <param name="output">执行输出内容。</param>
    /// <returns>根据主 Agent 事件更新输出、审批等待状态和工具调用后的聚合副本。</returns>
    private UnifiedEntryAggregate ApplyMainEvent(UnifiedEntryAggregate aggregate, AgentRunEvent source, string output)
    {
        UnifiedAgentRunRecord main = aggregate.Details.AgentRuns.Single(
            value => value.Kind == UnifiedAgentRunKind.Main);
        ProtectedUnifiedPayload protectedOutput = Protect(output);
        UnifiedAgentRunRecord updatedMain = main with
        {
            Status = source.Kind == AgentRunEventKind.ApprovalRequired
                ? UnifiedRunStatus.WaitingForApproval
                : main.Status,
            Output = protectedOutput.Content,
            OutputSha256 = output.Length == 0
                ? string.Empty
                : protectedOutput.OriginalSha256
        };
        IReadOnlyList<UnifiedToolCallRecord> toolCalls =
            ApplyToolEvent(aggregate, source);
        return aggregate.WithDetails(new UnifiedRunDetails(
            source.Kind == AgentRunEventKind.ApprovalRequired
                ? aggregate.Details.EntryRun with
                {
                    Status = UnifiedRunStatus.WaitingForApproval,
                    ErrorCode = AgentRunErrorCodes.ToolApprovalRequired
                }
                : aggregate.Details.EntryRun,
            aggregate.Details.AgentRuns
                .Select(value => value.Id == main.Id ? updatedMain : value)
                .ToArray(),
            aggregate.Details.Orchestrations,
            toolCalls));
    }
    #endregion

    #region 处理（ApplyToolEvent）
    /// <summary>
    /// 处理（ApplyToolEvent）
    /// </summary>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="source">源数据。</param>
    /// <returns>应用工具开始、审批等待或终态事件后的工具调用集合；不相关事件保留原集合。</returns>
    private IReadOnlyList<UnifiedToolCallRecord> ApplyToolEvent(UnifiedEntryAggregate aggregate, AgentRunEvent source)
    {
        if (source.ToolVersionId is not Guid toolVersionId)
        {
            return aggregate.Details.ToolCalls;
        }

        Guid callId = source.ToolCallId ?? toolVersionId;
        UnifiedToolCallRecord? current = aggregate.Details.ToolCalls
            .FirstOrDefault(value => value.Id == callId);
        if (source.Kind is AgentRunEventKind.ToolStarted
                or AgentRunEventKind.ApprovalRequired
            && current is null)
        {
            ProtectedUnifiedPayload arguments = Protect(source.ArgumentsJson);
            var started = new UnifiedToolCallRecord(
                callId,
                aggregate.Details.EntryRun.Id,
                source.RunId,
                toolVersionId,
                0,
                source.Kind == AgentRunEventKind.ApprovalRequired
                    ? UnifiedRunStatus.WaitingForApproval
                    : UnifiedRunStatus.Running,
                source.OccurredAtUtc,
                null,
                null,
                arguments.Content,
                arguments.OriginalSha256,
                string.Empty,
                string.Empty,
                source.Kind == AgentRunEventKind.ApprovalRequired
                    ? AgentRunErrorCodes.ToolApprovalRequired
                    : string.Empty);
            return aggregate.Details.ToolCalls.Append(started).ToArray();
        }

        if (current is null
            || source.Kind is not (
                AgentRunEventKind.ToolSucceeded
                or AgentRunEventKind.ToolBlocked
                or AgentRunEventKind.ToolFailed))
        {
            return aggregate.Details.ToolCalls;
        }

        ProtectedUnifiedPayload result = Protect(source.Text);
        UnifiedRunStatus status = source.Kind switch
        {
            AgentRunEventKind.ToolSucceeded => UnifiedRunStatus.Completed,
            AgentRunEventKind.ToolBlocked => UnifiedRunStatus.Blocked,
            _ => UnifiedRunStatus.Failed
        };
        UnifiedToolCallRecord terminal = current with
        {
            Status = status,
            FinishedAtUtc = source.OccurredAtUtc,
            Duration = NonNegative(source.OccurredAtUtc - current.StartedAtUtc),
            ResultContent = result.Content,
            ResultSha256 = source.Text.Length == 0
                ? string.Empty
                : result.OriginalSha256,
            ErrorCode = source.ErrorCode
        };
        return aggregate.Details.ToolCalls
            .Select(value => value.Id == current.Id ? terminal : value)
            .ToArray();
    }
    #endregion

    #region 处理（AppendRouteEventAsync）
    /// <summary>
    /// 处理（AppendRouteEventAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="route">请求路由。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task AppendRouteEventAsync(ActiveUnifiedEntryExecution active, string route)
    {
        ProtectedUnifiedPayload payload = Protect(JsonSerializer.Serialize(new
        {
            route
        }));
        await active.Scope.AppendEventAsync(sequence => new UnifiedRunEventRecord(
            Guid.NewGuid(),
            active.RunId,
            sequence,
            active.Scope.CorrelationId,
            "route-selected",
            _timeProvider.GetUtcNow(),
            null,
            0,
            payload.Content,
            payload.OriginalSha256),
            CancellationToken.None).ConfigureAwait(false);
    }
    #endregion

    #region 处理（AppendEventAsync）
    /// <summary>
    /// 处理（AppendEventAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="kind">记录或事件类型。</param>
    /// <param name="aggregateTask">聚合操作的异步任务。</param>
    /// <param name="parentRunId">父运行标识。</param>
    /// <param name="depth">当前递归或执行树深度。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task AppendEventAsync(
        ActiveUnifiedEntryExecution active,
        string kind,
        Task<UnifiedEntryAggregate> aggregateTask,
        Guid? parentRunId,
        int depth,
        CancellationToken cancellationToken)
    {
        UnifiedEntryAggregate aggregate = await aggregateTask.ConfigureAwait(false);
        ProtectedUnifiedPayload payload = Protect(JsonSerializer.Serialize(new
        {
            runId = aggregate.Details.EntryRun.Id
        }));
        await active.Scope.AppendEventAsync(sequence => new UnifiedRunEventRecord(
            Guid.NewGuid(),
            active.RunId,
            sequence,
            active.Scope.CorrelationId,
            kind,
            _timeProvider.GetUtcNow(),
            parentRunId,
            depth,
            payload.Content,
            payload.OriginalSha256),
            cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region 处理（FinalizeOwnedAsync）
    /// <summary>
    /// 处理（FinalizeOwnedAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="afterSequence">查询事件的起始序号，不包含该序号。</param>
    /// <param name="terminatePreparedAudit">是否终结已准备的审计记录。</param>
    /// <returns>主终结过程完成后指定序号之后的持久化运行事件；无论成功或异常都会通知终结等待方。</returns>
    private async Task<IReadOnlyList<UnifiedRunEvent>> FinalizeOwnedAsync(
        ActiveUnifiedEntryExecution active,
        UnifiedRunStatus status,
        string output,
        string errorCode,
        long afterSequence,
        bool terminatePreparedAudit = false)
    {
        active.TerminalStatus = status;
        active.TerminalErrorCode = errorCode;
        try
        {
            return await FinalizeCoreAsync(
                active,
                status,
                output,
                errorCode,
                afterSequence,
                terminatePreparedAudit).ConfigureAwait(false);
        }
        finally
        {
            active.PrimaryFinalizationCompleted.TrySetResult();
        }
    }
    #endregion

    #region 处理（FinalizeCoreAsync）
    /// <summary>
    /// 处理（FinalizeCoreAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="afterSequence">查询事件的起始序号，不包含该序号。</param>
    /// <param name="terminatePreparedAudit">是否终结已准备的审计记录。</param>
    /// <returns>终态持久化后指定序号之后的运行事件；已持久化的终态复用现有事件，持久化失败时抛出异常。</returns>
    private async Task<IReadOnlyList<UnifiedRunEvent>> FinalizeCoreAsync(
        ActiveUnifiedEntryExecution active,
        UnifiedRunStatus status,
        string output,
        string errorCode,
        long afterSequence,
        bool terminatePreparedAudit = false)
    {
        await active.TerminalGate.WaitAsync(CancellationToken.None)
            .ConfigureAwait(false);
        try
        {
            if (active.TerminalPersisted)
            {
                UnifiedEntryAggregate durable = active.TerminalSnapshot
                    ?? throw new InvalidOperationException(
                        "A durable terminal run requires its terminal snapshot.");
                IReadOnlyList<UnifiedRunEvent> durableEvents =
                    MapEvents(durable, afterSequence);
                Retire(active);
                return durableEvents;
            }

            if (active.TerminalSnapshot is null)
            {
                DateTimeOffset finishedAt = _timeProvider.GetUtcNow();
                IReadOnlyList<BusinessQueryAuthoritativeResult> businessResults =
                    status == UnifiedRunStatus.Completed
                        ? active.Scope.GetBusinessQueryResults()
                        : [];
                string[] businessContents = businessResults
                    .Select(value => value.ToPersistedContent())
                    .ToArray();
                UnifiedEntryAggregate preFinalization =
                    await active.Scope.GetAggregateSnapshotAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                int existingBusinessBytes = preFinalization.Messages
                    .Where(value => value.Kind == ConversationMessageKind.BusinessQueryResult)
                    .Sum(value => value.ContentUtf8Bytes);
                int newBusinessBytes = businessContents.Sum(Encoding.UTF8.GetByteCount);
                if (businessContents.Any(value => Encoding.UTF8.GetByteCount(value)
                        > _businessQueryResultLimits.MaximumResultBytes)
                    || existingBusinessBytes > _businessQueryResultLimits.MaximumConversationBytes
                        - newBusinessBytes)
                {
                    status = UnifiedRunStatus.Failed;
                    errorCode = UnifiedEntryErrorCodes.BusinessQueryResultLimitExceeded;
                    output = string.Empty;
                    businessResults = [];
                    businessContents = [];
                }

                ProtectedUnifiedPayload protectedOutput = Protect(output);
                (BusinessQueryAuthoritativeResult Result, ProtectedUnifiedPayload Payload)[]
                    protectedBusinessResults = businessResults
                        .Select((value, index) => (value, Protect(businessContents[index])))
                        .ToArray();
                await active.Scope.MutateAggregateAsync(aggregate =>
                {
                    UnifiedEntryRunRecord entry = aggregate.Details.EntryRun with
                    {
                        Output = protectedOutput.Content,
                        OutputSha256 = output.Length == 0
                            ? string.Empty
                            : protectedOutput.OriginalSha256
                    };
                    IReadOnlyList<ConversationMessageRecord> messages =
                        aggregate.Messages;
                    foreach ((BusinessQueryAuthoritativeResult result,
                              ProtectedUnifiedPayload payload) in protectedBusinessResults)
                    {
                        messages = messages.Append(new ConversationMessageRecord(
                            Guid.NewGuid(),
                            aggregate.Conversation.Id,
                            ConversationMessageRole.Assistant,
                            payload.Content,
                            payload.OriginalSha256,
                            payload.OriginalUtf8Bytes,
                            finishedAt)
                        {
                            Kind = ConversationMessageKind.BusinessQueryResult,
                            BusinessQueryId = result.QueryId,
                            BusinessQueryReceiptJson = result.ReceiptJson,
                            BusinessQueryPresentationJson = result.PresentationJson,
                            BusinessQueryIntegritySha256 = result.IntegritySha256
                        }).ToArray();
                    }

                    if (status == UnifiedRunStatus.Completed
                        || (status is UnifiedRunStatus.Cancelled or UnifiedRunStatus.Failed
                            && !string.IsNullOrWhiteSpace(output)))
                    {
                        var assistant = new ConversationMessageRecord(
                            Guid.NewGuid(),
                            aggregate.Conversation.Id,
                            ConversationMessageRole.Assistant,
                            protectedOutput.Content,
                            protectedOutput.OriginalSha256,
                            protectedOutput.OriginalUtf8Bytes,
                            finishedAt)
                        {
                            Kind = ConversationMessageKind.AssistantNarrative
                        };
                        messages = messages.Append(assistant).ToArray();
                    }

                    return new UnifiedEntryAggregate(
                        aggregate.Conversation with { UpdatedAtUtc = finishedAt },
                        messages,
                        new UnifiedRunDetails(
                            entry,
                            SweepAgentRuns(
                                aggregate.Details.AgentRuns,
                                status,
                                protectedOutput,
                                output,
                                errorCode,
                                finishedAt),
                            SweepOrchestrations(
                                aggregate.Details.Orchestrations,
                                status,
                                errorCode,
                                finishedAt),
                            SweepToolCalls(
                                aggregate.Details.ToolCalls,
                                status,
                                errorCode,
                                finishedAt)),
                        aggregate.Events,
                        aggregate.PersistenceRevision);
                }, CancellationToken.None).ConfigureAwait(false);

                foreach ((BusinessQueryAuthoritativeResult result,
                          ProtectedUnifiedPayload payload) in protectedBusinessResults)
                {
                    await active.Scope.AppendEventAsync(sequence =>
                        new UnifiedRunEventRecord(
                            Guid.NewGuid(),
                            active.RunId,
                            sequence,
                            active.Scope.CorrelationId,
                            "business-query-result",
                            finishedAt,
                            null,
                            0,
                            payload.Content,
                            payload.OriginalSha256),
                        CancellationToken.None).ConfigureAwait(false);
                }

                string terminalKind = status switch
                {
                    UnifiedRunStatus.Completed => "completed",
                    UnifiedRunStatus.Cancelled => "cancelled",
                    _ => "failed"
                };
                string rawPayloadJson = JsonSerializer.Serialize(new
                {
                    output,
                    errorCode
                });
                ProtectedUnifiedPayload rawPayload = Protect(rawPayloadJson);
                ProtectedUnifiedPayload persistedPayload = Protect(
                    JsonSerializer.Serialize(new
                    {
                        output = protectedOutput.Content,
                        errorCode
                    }));
                await active.Scope.AppendEventAsync(sequence => new UnifiedRunEventRecord(
                    Guid.NewGuid(),
                    active.RunId,
                    sequence,
                    active.Scope.CorrelationId,
                    terminalKind,
                    finishedAt,
                    null,
                    0,
                    persistedPayload.Content,
                    rawPayload.OriginalSha256),
                    CancellationToken.None).ConfigureAwait(false);
                await active.Scope.TryTransitionTerminalAsync(
                    status,
                    errorCode,
                    CancellationToken.None).ConfigureAwait(false);
                active.TerminalSnapshot =
                    await active.Scope.GetAggregateSnapshotAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                active.TerminalStatus = status;
                active.TerminalErrorCode = errorCode;

                if (terminatePreparedAudit)
                {
                    await TerminatePreparedAuditAsync(
                        active.MainContext,
                        status == UnifiedRunStatus.Cancelled
                            ? AgentRunStatus.Cancelled
                            : AgentRunStatus.Failed,
                        errorCode).ConfigureAwait(false);
                }
            }

            UnifiedEntryAggregate terminal = active.TerminalSnapshot;
            try
            {
                terminal = await active.Scope.PersistAsync(
                        _repository,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                active.TerminalSnapshot = terminal;
                active.TerminalPersisted = true;
            }
            catch
            {
                ScheduleTerminalRecovery(active);
                throw;
            }

            IReadOnlyList<UnifiedRunEvent> terminalEvents =
                MapEvents(terminal, afterSequence);
            Retire(active);
            return terminalEvents;
        }
        finally
        {
            active.TerminalGate.Release();
        }
    }
    #endregion

    #region 处理（JoinOrRetryFinalizationAsync）
    /// <summary>
    /// 处理（JoinOrRetryFinalizationAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="afterSequence">查询事件的起始序号，不包含该序号。</param>
    /// <param name="joinCancellationToken">等待执行结束时使用的取消令牌。</param>
    /// <returns>等待主终结后读取或重试终结得到的运行事件；等待过程支持调用方取消。</returns>
    private async Task<IReadOnlyList<UnifiedRunEvent>> JoinOrRetryFinalizationAsync(
        ActiveUnifiedEntryExecution active,
        long afterSequence,
        CancellationToken joinCancellationToken = default)
    {
        await active.PrimaryFinalizationCompleted.Task
            .WaitAsync(joinCancellationToken).ConfigureAwait(false);
        return await FinalizeCoreAsync(
            active,
            active.TerminalStatus,
            string.Empty,
            active.TerminalErrorCode,
            afterSequence).ConfigureAwait(false);
    }
    #endregion

    #region 失去运行所有权后等待或重试收尾（JoinRuntimeOwnershipLossAsync）
    /// <summary>
    /// 失去运行所有权后等待或重试收尾（JoinRuntimeOwnershipLossAsync）。
    /// </summary>
    /// <param name="active">已失去运行所有权、需要等待收尾的活动执行对象。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>正常完成时固定返回 false，表示当前枚举器不再产出事件；取消或收尾失败通过异常报告。</returns>
    private async Task<bool> JoinRuntimeOwnershipLossAsync(ActiveUnifiedEntryExecution active, CancellationToken cancellationToken)
    {
        try
        {
            await JoinOrRetryFinalizationAsync(
                active,
                afterSequence: 0,
                joinCancellationToken: cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnifiedEntryException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new UnifiedEntryException(
                UnifiedEntryErrorCodes.PersistenceFailed,
                "The terminal unified entry aggregate could not be persisted.");
        }
    }
    #endregion

    #region 处理（SweepAgentRuns）
    /// <summary>
    /// 处理（SweepAgentRuns）
    /// </summary>
    /// <param name="values">需要保留已有终态并清理活动记录的子运行或工具调用集合。</param>
    /// <param name="rootStatus">根运行状态。</param>
    /// <param name="protectedOutput">已加密保护的输出内容。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="rootErrorCode">根运行错误码。</param>
    /// <param name="finishedAt">完成时间（UTC）。</param>
    /// <returns>设置主 Agent 终态并清理仍活动的子 Agent 后的运行记录集合。</returns>
    private static IReadOnlyList<UnifiedAgentRunRecord> SweepAgentRuns(
        IReadOnlyList<UnifiedAgentRunRecord> values,
        UnifiedRunStatus rootStatus,
        ProtectedUnifiedPayload protectedOutput,
        string output,
        string rootErrorCode,
        DateTimeOffset finishedAt)
    {
        return values.Select(value =>
        {
            if (value.Kind == UnifiedAgentRunKind.Main)
            {
                return value with
                {
                    Status = rootStatus,
                    FinishedAtUtc = finishedAt,
                    Duration = NonNegative(finishedAt - value.StartedAtUtc),
                    Output = protectedOutput.Content,
                    OutputSha256 = output.Length == 0
                        ? string.Empty
                        : protectedOutput.OriginalSha256,
                    ErrorCode = rootErrorCode
                };
            }

            return IsLive(value.Status)
                ? value with
                {
                    Status = DescendantTerminalStatus(rootStatus),
                    FinishedAtUtc = finishedAt,
                    Duration = NonNegative(finishedAt - value.StartedAtUtc),
                    ErrorCode = DescendantErrorCode(rootStatus, rootErrorCode)
                }
                : value;
        }).ToArray();
    }
    #endregion

    #region 处理（SweepOrchestrations）
    /// <summary>
    /// 处理（SweepOrchestrations）
    /// </summary>
    /// <param name="values">需要保留已有终态并清理活动记录的子运行或工具调用集合。</param>
    /// <param name="rootStatus">根运行状态。</param>
    /// <param name="rootErrorCode">根运行错误码。</param>
    /// <param name="finishedAt">完成时间（UTC）。</param>
    /// <returns>将仍活动的编排关联置为取消或失败并补齐结束时间的集合，已有终态保持不变。</returns>
    private static IReadOnlyList<UnifiedOrchestrationRunLink> SweepOrchestrations(
        IReadOnlyList<UnifiedOrchestrationRunLink> values,
        UnifiedRunStatus rootStatus,
        string rootErrorCode,
        DateTimeOffset finishedAt) =>
        values.Select(value => IsLive(value.Status)
            ? value with
            {
                Status = DescendantTerminalStatus(rootStatus),
                FinishedAtUtc = finishedAt,
                Duration = NonNegative(finishedAt - value.StartedAtUtc),
                ErrorCode = DescendantErrorCode(rootStatus, rootErrorCode)
            }
            : value).ToArray();
    #endregion

    #region 处理（SweepToolCalls）
    /// <summary>
    /// 处理（SweepToolCalls）
    /// </summary>
    /// <param name="values">需要保留已有终态并清理活动记录的子运行或工具调用集合。</param>
    /// <param name="rootStatus">根运行状态。</param>
    /// <param name="rootErrorCode">根运行错误码。</param>
    /// <param name="finishedAt">完成时间（UTC）。</param>
    /// <returns>将仍活动的工具调用置为取消或失败并补齐结束时间的集合，已有终态保持不变。</returns>
    private static IReadOnlyList<UnifiedToolCallRecord> SweepToolCalls(
        IReadOnlyList<UnifiedToolCallRecord> values,
        UnifiedRunStatus rootStatus,
        string rootErrorCode,
        DateTimeOffset finishedAt) =>
        values.Select(value => IsLive(value.Status)
            ? value with
            {
                Status = DescendantTerminalStatus(rootStatus),
                FinishedAtUtc = finishedAt,
                Duration = NonNegative(finishedAt - value.StartedAtUtc),
                ErrorCode = DescendantErrorCode(rootStatus, rootErrorCode)
            }
            : value).ToArray();
    #endregion

    #region 判断运行是否尚未终结（IsLive）
    /// <summary>
    /// 判断运行是否尚未终结（IsLive）。
    /// </summary>
    /// <param name="status">待检查的统一入口运行状态。</param>
    /// <returns>状态为 Pending 或 Running 时返回 true，否则返回 false。</returns>
    private static bool IsLive(UnifiedRunStatus status) =>
        status is UnifiedRunStatus.Pending or UnifiedRunStatus.Running;
    #endregion

    #region 处理（DescendantTerminalStatus）
    /// <summary>
    /// 处理（DescendantTerminalStatus）
    /// </summary>
    /// <param name="rootStatus">根运行状态。</param>
    /// <returns>根运行取消时返回 Cancelled，否则未完成后代统一返回 Failed。</returns>
    private static UnifiedRunStatus DescendantTerminalStatus(UnifiedRunStatus rootStatus) =>
        rootStatus == UnifiedRunStatus.Cancelled
            ? UnifiedRunStatus.Cancelled
            : UnifiedRunStatus.Failed;
    #endregion

    #region 处理（DescendantErrorCode）
    /// <summary>
    /// 处理（DescendantErrorCode）
    /// </summary>
    /// <param name="rootStatus">根运行状态。</param>
    /// <param name="rootErrorCode">根运行错误码。</param>
    /// <returns>根运行取消时的取消错误码，或根错误码；根错误码为空时返回 InvalidState。</returns>
    private static string DescendantErrorCode(UnifiedRunStatus rootStatus, string rootErrorCode) =>
        rootStatus == UnifiedRunStatus.Cancelled
            ? UnifiedEntryErrorCodes.Cancelled
            : string.IsNullOrWhiteSpace(rootErrorCode)
                ? UnifiedEntryErrorCodes.InvalidState
                : rootErrorCode;
    #endregion

    #region 处理（ObserveUnstartedTimeoutAsync）
    /// <summary>
    /// 处理（ObserveUnstartedTimeoutAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ObserveUnstartedTimeoutAsync(ActiveUnifiedEntryExecution active)
    {
        try
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                active.Scope.EntryCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is the wake-up signal.
        }

        if (!_active.TryGetValue(
                active.RunId,
                out ActiveUnifiedEntryExecution? current)
            || !ReferenceEquals(current, active))
        {
            return;
        }

        try
        {
            if (active.TryClaimPreparedFinalization())
            {
                string errorCode = active.Scope.ClassifyCancellation(
                    active.MainLease,
                    CancellationToken.None);
                UnifiedRunStatus status =
                    errorCode == UnifiedEntryErrorCodes.EntryTimeout
                        ? UnifiedRunStatus.Failed
                        : UnifiedRunStatus.Cancelled;
                await FinalizeOwnedAsync(
                    active,
                    status,
                    string.Empty,
                    errorCode,
                    afterSequence: 0,
                    terminatePreparedAudit: true).ConfigureAwait(false);
            }
            else
            {
                await JoinOrRetryFinalizationAsync(
                    active,
                    afterSequence: 0).ConfigureAwait(false);
            }
        }
        catch
        {
            // FinalizeCoreAsync schedules bounded persistence recovery.
        }
    }
    #endregion

    #region 处理（ScheduleTerminalRecovery）
    /// <summary>
    /// 处理（ScheduleTerminalRecovery）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    private void ScheduleTerminalRecovery(ActiveUnifiedEntryExecution active)
    {
        if (!active.TryScheduleRecovery())
        {
            return;
        }

        _ = RecoverTerminalPersistenceAsync(active);
    }
    #endregion

    #region 恢复（RecoverTerminalPersistenceAsync）
    /// <summary>
    /// 恢复（RecoverTerminalPersistenceAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task RecoverTerminalPersistenceAsync(ActiveUnifiedEntryExecution active)
    {
        TimeSpan[] delays =
        [
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(500)
        ];
        foreach (TimeSpan delay in delays)
        {
            await Task.Delay(delay).ConfigureAwait(false);
            if (!_active.TryGetValue(
                    active.RunId,
                    out ActiveUnifiedEntryExecution? current)
                || !ReferenceEquals(current, active))
            {
                return;
            }

            try
            {
                await FinalizeCoreAsync(
                    active,
                    active.TerminalStatus,
                    string.Empty,
                    active.TerminalErrorCode,
                    afterSequence: 0).ConfigureAwait(false);
                return;
            }
            catch
            {
                // Continue through the bounded retry schedule.
            }
        }
    }
    #endregion

    #region 处理（Retire）
    /// <summary>
    /// 处理（Retire）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    private void Retire(ActiveUnifiedEntryExecution active)
    {
        if (!_active.TryRemove(
                new KeyValuePair<Guid, ActiveUnifiedEntryExecution>(
                    active.RunId,
                    active)))
        {
            return;
        }

        active.MarkRetired();
        try
        {
            active.MainLease.Dispose();
        }
        catch
        {
            // Durable terminal state owns the outcome; cleanup is best effort.
        }

        try
        {
            active.Scope.Dispose();
        }
        catch
        {
            // Scope disposal attempts every owned cancellation resource first.
        }

        try
        {
            active.Cancellation.Dispose();
        }
        catch
        {
            // The handle is already retired after confirmed persistence.
        }
    }
    #endregion

    #region 处理（PersistAndCollectAsync）
    /// <summary>
    /// 处理（PersistAndCollectAsync）
    /// </summary>
    /// <param name="active">当前活动状态。</param>
    /// <param name="afterSequence">查询事件的起始序号，不包含该序号。</param>
    /// <returns>聚合成功持久化后指定序号之后的运行事件。</returns>
    private async Task<IReadOnlyList<UnifiedRunEvent>> PersistAndCollectAsync(ActiveUnifiedEntryExecution active, long afterSequence)
    {
        UnifiedEntryAggregate snapshot = await active.Scope.PersistAsync(
                _repository,
                CancellationToken.None)
            .ConfigureAwait(false);
        return MapEvents(snapshot, afterSequence);
    }
    #endregion

    #region 映射（MapEvents）
    /// <summary>
    /// 映射（MapEvents）
    /// </summary>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="afterSequence">查询事件的起始序号，不包含该序号。</param>
    /// <returns>序号大于指定边界、按序号排序的对外运行事件，并为路由选择事件提取路由。</returns>
    private static IReadOnlyList<UnifiedRunEvent> MapEvents(UnifiedEntryAggregate aggregate, long afterSequence) =>
        aggregate.Events
            .Where(value => value.Sequence > afterSequence)
            .OrderBy(value => value.Sequence)
            .Select(value => new UnifiedRunEvent(
                value.EntryRunId,
                aggregate.Conversation.Id,
                value.Sequence,
                value.Kind,
                value.OccurredAtUtc,
                value.CorrelationId,
                value.ParentRunId,
                value.Depth,
                value.PayloadJson)
            {
                Route = value.Kind == "route-selected"
                    ? ReadRoute(value.PayloadJson)
                    : string.Empty
            })
            .ToArray();
    #endregion

    #region 读取（ReadRoute）
    /// <summary>
    /// 读取（ReadRoute）
    /// </summary>
    /// <param name="payloadJson">载荷的 JSON 文本。</param>
    /// <returns>事件载荷中的 route 文本；字段缺失、值为 null 或 JSON 解析失败时返回空字符串。</returns>
    private static string ReadRoute(string payloadJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty(
                    "route",
                    out JsonElement route)
                ? route.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
    #endregion

    #region 处理（ClassifyToolRoute）
    /// <summary>
    /// 处理（ClassifyToolRoute）
    /// </summary>
    /// <param name="toolName">工具名称。</param>
    /// <returns>内部工具对应的 skill、child-agent 或 orchestration 路由，其他工具归为 mcp。</returns>
    private static string ClassifyToolRoute(string toolName) =>
        toolName switch
        {
            "use_skill" => "skill",
            "delegate_to_agent" => "child-agent",
            "run_orchestration" => "orchestration",
            _ => "mcp"
        };
    #endregion

    #region 映射（MapMainEventKind）
    /// <summary>
    /// 映射（MapMainEventKind）
    /// </summary>
    /// <param name="source">源数据。</param>
    /// <returns>映射到统一入口协议的主 Agent 事件名称，未单独映射的类型使用 message。</returns>
    private static string MapMainEventKind(AgentRunEvent source) =>
        source.Kind switch
        {
            AgentRunEventKind.Started => "main-agent-started",
            AgentRunEventKind.SkillStarted => "skill-started",
            AgentRunEventKind.KnowledgeRetrieved => "knowledge-retrieved",
            AgentRunEventKind.Citation => "knowledge-citation",
            AgentRunEventKind.ToolStarted when source.ToolName == "use_skill" =>
                "skill-started",
            AgentRunEventKind.ToolStarted => "tool-started",
            AgentRunEventKind.ToolSucceeded => "tool-succeeded",
            AgentRunEventKind.ToolBlocked => "tool-blocked",
            AgentRunEventKind.ToolFailed => "tool-failed",
            AgentRunEventKind.ApprovalRequired => "approval-required",
            _ => "message"
        };
    #endregion

    #region 识别不可继续执行的平台错误（IsFatalPlatformFailure）
    /// <summary>
    /// 识别不可继续执行的平台错误（IsFatalPlatformFailure）。
    /// </summary>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>知识访问被拒绝，或错误码以 UNIFIED_ENTRY_ 开头且不是子运行失败或编排执行失败时返回 true，否则返回 false。</returns>
    private static bool IsFatalPlatformFailure(string errorCode) =>
        errorCode == UnifiedEntryErrorCodes.KnowledgeAccessDenied
        || (errorCode.StartsWith("UNIFIED_ENTRY_", StringComparison.Ordinal)
            && errorCode is not UnifiedEntryErrorCodes.ChildExecutionFailed
            && errorCode is not UnifiedEntryErrorCodes.OrchestrationExecutionFailed);
    #endregion

    #region 创建（CreateConversationTitle）
    /// <summary>
    /// 创建（CreateConversationTitle）
    /// </summary>
    /// <param name="protectedInput">已加密保护的输入内容。</param>
    /// <returns>最多 80 个 UTF-16 代码单元的会话标题，截断时避免拆分代理对。</returns>
    private static string CreateConversationTitle(string protectedInput)
    {
        const int maximumCharacters = 80;
        if (protectedInput.Length <= maximumCharacters)
        {
            return protectedInput;
        }

        int length = maximumCharacters;
        if (char.IsHighSurrogate(protectedInput[length - 1])
            && char.IsLowSurrogate(protectedInput[length]))
        {
            length--;
        }

        return protectedInput[..length];
    }
    #endregion

    #region 构建（BuildConversationHistory）
    /// <summary>
    /// 构建（BuildConversationHistory）
    /// </summary>
    /// <param name="messages">会话消息集合。</param>
    /// <returns>满足消息数量及 UTF-8 字节预算的最近对话历史，按原顺序排列并排除业务查询结果消息。</returns>
    private static IReadOnlyList<AgentConversationMessage> BuildConversationHistory(IReadOnlyList<ConversationMessageRecord> messages)
    {
        var selected = new List<AgentConversationMessage>();
        int usedBytes = 0;
        for (int index = messages.Count - 1;
             index >= 0 && selected.Count < MaximumConversationHistoryMessages;
             index--)
        {
            ConversationMessageRecord source = messages[index];
            if (source.Kind == ConversationMessageKind.BusinessQueryResult)
            {
                continue;
            }

            int contentBytes = Encoding.UTF8.GetByteCount(source.Content);
            if (contentBytes > MaximumConversationHistoryUtf8Bytes - usedBytes)
            {
                break;
            }

            selected.Add(new AgentConversationMessage(
                source.Role == ConversationMessageRole.User
                    ? AgentConversationRole.User
                    : AgentConversationRole.Assistant,
                source.Content));
            usedBytes += contentBytes;
        }

        selected.Reverse();
        return new System.Collections.ObjectModel.ReadOnlyCollection<AgentConversationMessage>(
            selected);
    }
    #endregion

    #region 加密保护（Protect）
    /// <summary>
    /// 加密保护（Protect）
    /// </summary>
    /// <param name="value">待校验字节上限并脱敏的原始文本；null 按空字符串处理。</param>
    /// <returns>按持久化载荷上限处理的脱敏内容、原始摘要及字节数。</returns>
    private static ProtectedUnifiedPayload Protect(string? value) =>
        UnifiedEntryPayloadProtector.Protect(
            value,
            MaximumStoredPayloadBytes,
            MaximumStoredPayloadBytes);
    #endregion

    #region 处理（NonNegative）
    /// <summary>
    /// 将负时长归零（NonNegative）。
    /// </summary>
    /// <param name="value">待检查的持续时间。</param>
    /// <returns>输入为负数时返回零时长，否则返回原时长。</returns>
    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;
    #endregion

    #region 处理（TerminatePreparedAuditAsync）
    /// <summary>
    /// 处理（TerminatePreparedAuditAsync）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task TerminatePreparedAuditAsync(AgentRunContext context, AgentRunStatus status, string errorCode)
    {
        try
        {
            await _agentRuntime.TerminatePreparedRunAsync(
                context,
                status,
                errorCode,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Preserve the primary preparation failure.
        }
    }
    #endregion

    #region 尝试执行（TryCancel）
    /// <summary>
    /// 尝试执行（TryCancel）
    /// </summary>
    /// <param name="cancellation">执行取消控制对象。</param>
    private static void TryCancel(CancellationTokenSource cancellation)
    {
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // A concurrent terminal cleanup already retired the handle.
        }
        catch (AggregateException)
        {
            // Terminal cleanup still runs with CancellationToken.None.
        }
    }
    #endregion
}

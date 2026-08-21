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

public sealed class UnifiedEntryService
{
    private const int MaximumPendingDeltaEvents = 4;
    private static readonly TimeSpan MaximumDeltaPersistenceInterval =
        TimeSpan.FromMilliseconds(75);
    private const int MaximumStoredPayloadBytes =
        AgentRuntimeService.MaximumInputCharacters * 4;
    public const int MaximumConversationHistoryMessages = 40;
    public const int MaximumConversationHistoryUtf8Bytes = 65_536;
    private readonly MainAgentAssignmentService _mainAgents;
    private readonly AgentRuntimeService _agentRuntime;
    private readonly OrchestrationRuntimeService _orchestrationRuntime;
    private readonly IUnifiedEntryRepository _repository;
    private readonly UnifiedEntryLimits _limits;
    private readonly TimeProvider _timeProvider;
    private readonly BusinessQueryToolPolicy? _businessQueryPolicy;
    private readonly BusinessQueryResultLimits _businessQueryResultLimits;
    private readonly IAgentToolApprovalHandler? _toolApprovalHandler;
    private readonly ConcurrentDictionary<Guid, ActiveUnifiedEntryExecution> _active = [];

    internal Action? BeforeRuntimeOwnershipClaim { get; set; }

    public UnifiedEntryService(
        MainAgentAssignmentService mainAgents,
        AgentRuntimeService agentRuntime,
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

    public async Task<UnifiedEntryPreparationResult> PrepareAsync(
        string? input,
        Guid? conversationId,
        CancellationToken cancellationToken = default) =>
        await PrepareCoreAsync(
            input, conversationId, null, null, null, cancellationToken);

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

    public IAsyncEnumerable<UnifiedRunEvent> StreamAsync(
        UnifiedEntryContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new UnifiedEntryEventEnumerable(this, context, cancellationToken);
    }

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

    private sealed class UnifiedEntryEventEnumerable(
        UnifiedEntryService owner,
        UnifiedEntryContext context,
        CancellationToken invocationCancellation) : IAsyncEnumerable<UnifiedRunEvent>
    {
        public IAsyncEnumerator<UnifiedRunEvent> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
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

        public UnifiedRunEvent Current => _inner.Current;

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
    }

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

    public async Task<bool> CancelAsync(
        Guid runId,
        CancellationToken cancellationToken = default) =>
        await CancelCoreAsync(runId, null, cancellationToken);

    public async Task<bool> CancelAsync(
        Guid runId,
        AgentExecutionIdentity executionIdentity,
        CancellationToken cancellationToken = default) =>
        await CancelCoreAsync(
            runId,
            executionIdentity ?? throw new ArgumentNullException(nameof(executionIdentity)),
            cancellationToken);

    private async Task<bool> CancelCoreAsync(
        Guid runId,
        AgentExecutionIdentity? executionIdentity,
        CancellationToken cancellationToken)
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

    private static bool SameOwner(
        AgentExecutionIdentity? stored,
        AgentExecutionIdentity requested) =>
        stored is not null
        && string.Equals(stored.TenantId, requested.TenantId, StringComparison.Ordinal)
        && string.Equals(stored.UserId, requested.UserId, StringComparison.Ordinal);

    private async Task PersistMainEventAsync(
        ActiveUnifiedEntryExecution active,
        AgentRunEvent source,
        string output,
        CancellationToken cancellationToken)
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

    private UnifiedEntryAggregate ApplyMainEvent(
        UnifiedEntryAggregate aggregate,
        AgentRunEvent source,
        string output)
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

    private IReadOnlyList<UnifiedToolCallRecord> ApplyToolEvent(
        UnifiedEntryAggregate aggregate,
        AgentRunEvent source)
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

    private async Task AppendRouteEventAsync(
        ActiveUnifiedEntryExecution active,
        string route)
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

    private async Task<bool> JoinRuntimeOwnershipLossAsync(
        ActiveUnifiedEntryExecution active,
        CancellationToken cancellationToken)
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

    private static bool IsLive(UnifiedRunStatus status) =>
        status is UnifiedRunStatus.Pending or UnifiedRunStatus.Running;

    private static UnifiedRunStatus DescendantTerminalStatus(
        UnifiedRunStatus rootStatus) =>
        rootStatus == UnifiedRunStatus.Cancelled
            ? UnifiedRunStatus.Cancelled
            : UnifiedRunStatus.Failed;

    private static string DescendantErrorCode(
        UnifiedRunStatus rootStatus,
        string rootErrorCode) =>
        rootStatus == UnifiedRunStatus.Cancelled
            ? UnifiedEntryErrorCodes.Cancelled
            : string.IsNullOrWhiteSpace(rootErrorCode)
                ? UnifiedEntryErrorCodes.InvalidState
                : rootErrorCode;

    private async Task ObserveUnstartedTimeoutAsync(
        ActiveUnifiedEntryExecution active)
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

    private void ScheduleTerminalRecovery(ActiveUnifiedEntryExecution active)
    {
        if (!active.TryScheduleRecovery())
        {
            return;
        }

        _ = RecoverTerminalPersistenceAsync(active);
    }

    private async Task RecoverTerminalPersistenceAsync(
        ActiveUnifiedEntryExecution active)
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

    private async Task<IReadOnlyList<UnifiedRunEvent>> PersistAndCollectAsync(
        ActiveUnifiedEntryExecution active,
        long afterSequence)
    {
        UnifiedEntryAggregate snapshot = await active.Scope.PersistAsync(
                _repository,
                CancellationToken.None)
            .ConfigureAwait(false);
        return MapEvents(snapshot, afterSequence);
    }

    private static IReadOnlyList<UnifiedRunEvent> MapEvents(
        UnifiedEntryAggregate aggregate,
        long afterSequence) =>
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

    private static string ClassifyToolRoute(string toolName) =>
        toolName switch
        {
            "use_skill" => "skill",
            "delegate_to_agent" => "child-agent",
            "run_orchestration" => "orchestration",
            _ => "mcp"
        };

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

    private static bool IsFatalPlatformFailure(string errorCode) =>
        errorCode == UnifiedEntryErrorCodes.KnowledgeAccessDenied
        || (errorCode.StartsWith("UNIFIED_ENTRY_", StringComparison.Ordinal)
            && errorCode is not UnifiedEntryErrorCodes.ChildExecutionFailed
            && errorCode is not UnifiedEntryErrorCodes.OrchestrationExecutionFailed);

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

    private static IReadOnlyList<AgentConversationMessage> BuildConversationHistory(
        IReadOnlyList<ConversationMessageRecord> messages)
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

    private static ProtectedUnifiedPayload Protect(string? value) =>
        UnifiedEntryPayloadProtector.Protect(
            value,
            MaximumStoredPayloadBytes,
            MaximumStoredPayloadBytes);

    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;

    private async Task TerminatePreparedAuditAsync(
        AgentRunContext context,
        AgentRunStatus status,
        string errorCode)
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
}

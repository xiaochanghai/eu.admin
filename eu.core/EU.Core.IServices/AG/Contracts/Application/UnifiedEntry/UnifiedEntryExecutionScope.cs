#nullable enable

using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.UnifiedEntry;

public sealed class UnifiedEntryExecutionScope :
    IAgentMcpCallGuard,
    IAgentMcpResultGuard,
    IDisposable
{
    private const double MaxCancelAfterMilliseconds = uint.MaxValue - 1d;
    private readonly object _stateGate = new();
    private readonly HashSet<UnifiedAgentExecutionLease> _activeAgentLeases = [];
    private readonly SemaphoreSlim _aggregateGate = new(1, 1);
    private readonly AsyncLocal<bool> _insideAggregateOperation = new();
    private readonly CancellationToken _callerCancellationToken;
    private readonly CancellationTokenSource _entryCancellation;
    private readonly CancellationTokenSource _entryTimeoutCancellation;
    private readonly TimeProvider _timeProvider;
    private UnifiedEntryAggregate? _aggregate;
    private long _sequence;
    private int _childCallCount;
    private int _orchestrationCallCount;
    private int _mcpCallCount;
    private int _mcpResultUtf8Bytes;
    private int _terminalTransitionCount;
    private readonly Dictionary<Guid, BusinessQueryAuthoritativeResult>
        _businessQueryResults = [];
    private bool _mainEntered;
    private bool _disposed;

    public UnifiedEntryExecutionScope(
        UnifiedEntryAggregate? aggregate = null,
        UnifiedEntryLimits? limits = null,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default,
        TimeProvider? timeProvider = null)
    {
        Limits = ValidateLimits(limits ?? UnifiedEntryLimits.Default);
        Guid? aggregateCorrelationId = aggregate?.Details.EntryRun.CorrelationId;
        if (aggregateCorrelationId == Guid.Empty || correlationId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty correlation ID is required.");
        }

        if (aggregateCorrelationId.HasValue
            && correlationId.HasValue
            && aggregateCorrelationId.Value != correlationId.Value)
        {
            throw new ArgumentException(
                "The scope correlation ID must match the aggregate correlation ID.",
                nameof(correlationId));
        }

        CorrelationId = correlationId ?? aggregateCorrelationId ?? Guid.NewGuid();
        if (aggregate is not null)
        {
            ValidateAggregateHistory(aggregate, CorrelationId);
            _aggregate = UnifiedEntryContractCloner.Clone(aggregate);
            _sequence = aggregate.Events.Count == 0
                ? 0
                : aggregate.Events[^1].Sequence;
            _terminalTransitionCount = IsTerminal(aggregate.Details.EntryRun.Status)
                ? 1
                : 0;
        }

        _timeProvider = timeProvider ?? TimeProvider.System;
        _callerCancellationToken = cancellationToken;
        _entryTimeoutCancellation = new CancellationTokenSource();
        _entryCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _entryTimeoutCancellation.Token);
        try
        {
            _entryTimeoutCancellation.CancelAfter(Limits.EntryTimeout);
        }
        catch
        {
            _entryCancellation.Dispose();
            _entryTimeoutCancellation.Dispose();
            throw;
        }
    }

    public UnifiedEntryLimits Limits { get; }

    public Guid CorrelationId { get; }

    public CancellationToken EntryCancellationToken => _entryCancellation.Token;

    public int ChildCallCount { get { lock (_stateGate) return _childCallCount; } }

    public int OrchestrationCallCount
    {
        get { lock (_stateGate) return _orchestrationCallCount; }
    }

    public int McpCallCount { get { lock (_stateGate) return _mcpCallCount; } }

    public int McpResultUtf8Bytes
    {
        get { lock (_stateGate) return _mcpResultUtf8Bytes; }
    }

    public int TerminalTransitionCount
    {
        get { lock (_stateGate) return _terminalTransitionCount; }
    }

    public bool TryRegisterBusinessQueryResult(
        Guid childRunId,
        BusinessQueryAuthoritativeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (childRunId == Guid.Empty)
        {
            return false;
        }

        lock (_stateGate)
        {
            if (_disposed || _businessQueryResults.ContainsKey(childRunId))
            {
                return false;
            }

            _businessQueryResults.Add(childRunId, result);
            return true;
        }
    }

    public IReadOnlyList<BusinessQueryAuthoritativeResult>
        GetBusinessQueryResults()
    {
        lock (_stateGate)
        {
            return _businessQueryResults.Values
                .Select(value => value with { })
                .ToArray();
        }
    }

    public long NextSequence()
    {
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            return AllocateSequenceLocked();
        }
    }

    public UnifiedAgentExecutionLease EnterMainAgent(Guid agentVersionId)
    {
        ValidateVersionId(agentVersionId);
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            if (_mainEntered)
            {
                throw InvalidState("The Main Agent execution lease already exists.");
            }

            var lease = new UnifiedAgentExecutionLease(
                this,
                agentVersionId,
                depth: 0,
                parent: null,
                _entryCancellation.Token,
                timeoutCancellation: null);
            _mainEntered = true;
            _activeAgentLeases.Add(lease);
            return lease;
        }
    }

    public UnifiedAgentExecutionLease ReserveChildAgent(
        Guid agentVersionId,
        UnifiedAgentExecutionLease parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ValidateVersionId(agentVersionId);
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            if (!ReferenceEquals(parent.Owner, this) || !parent.IsActive)
            {
                throw InvalidState("The parent Agent execution lease is not active in this scope.");
            }

            if (HasAncestorVersion(parent, agentVersionId))
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.AgentCycleDetected,
                    "The requested Agent version is already in the active ancestry.");
            }

            int depth = checked(parent.Depth + 1);
            if (depth > Limits.MaxAgentDepth)
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.AgentDepthLimitExceeded,
                    "The unified entry Agent depth limit was exceeded.");
            }

            if (_childCallCount >= Limits.MaxChildCalls)
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.ChildCallLimitExceeded,
                    "The unified entry child Agent call limit was exceeded.");
            }

            CancellationTokenSource? childTimeout = null;
            CancellationTokenSource? childCancellation = null;
            try
            {
                childTimeout = new CancellationTokenSource();
                childTimeout.CancelAfter(Limits.ChildTimeout);
                childCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    parent.CancellationToken,
                    childTimeout.Token);
                var lease = new UnifiedAgentExecutionLease(
                    this,
                    agentVersionId,
                    depth,
                    parent,
                    childCancellation.Token,
                    childTimeout,
                    childCancellation);
                _activeAgentLeases.Add(lease);
                _childCallCount++;
                childTimeout = null;
                childCancellation = null;
                return lease;
            }
            finally
            {
                childCancellation?.Dispose();
                childTimeout?.Dispose();
            }
        }
    }

    public bool ReserveOrchestration()
    {
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            if (_orchestrationCallCount >= Limits.MaxOrchestrationCalls)
            {
                return false;
            }

            _orchestrationCallCount++;
            return true;
        }
    }

    public ValueTask<AgentMcpCallGuardResult> ReserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            if (_mcpCallCount >= Limits.MaxMcpCalls)
            {
                return ValueTask.FromResult(AgentMcpCallGuardResult.Deny(
                    UnifiedEntryErrorCodes.McpCallLimitExceeded,
                    "The unified entry MCP call limit was exceeded."));
            }

            _mcpCallCount++;
            return ValueTask.FromResult(AgentMcpCallGuardResult.Allow());
        }
    }

    ValueTask<AgentMcpResultGuardResult> IAgentMcpResultGuard.ReserveAsync(
        int resultUtf8Bytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (resultUtf8Bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resultUtf8Bytes));
        }

        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            if (resultUtf8Bytes > Limits.MaxMcpResultUtf8Bytes - _mcpResultUtf8Bytes)
            {
                return ValueTask.FromResult(AgentMcpResultGuardResult.Deny(
                    UnifiedEntryErrorCodes.McpResultLimitExceeded,
                    "The unified entry MCP result budget was exceeded."));
            }

            _mcpResultUtf8Bytes += resultUtf8Bytes;
            return ValueTask.FromResult(AgentMcpResultGuardResult.Allow());
        }
    }

    public async Task MutateAggregateAsync(
        Func<UnifiedEntryAggregate, UnifiedEntryAggregate> mutation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        EnterAggregateOperation();
        try
        {
            await EnsureNotDisposedAsync(cancellationToken).ConfigureAwait(false);
            await _aggregateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                UnifiedEntryAggregate current = RequireAggregate();
                if (IsTerminal(current.Details.EntryRun.Status))
                {
                    throw new InvalidOperationException(
                        "A terminal unified entry aggregate cannot be mutated.");
                }

                UnifiedEntryAggregate next =
                    mutation(UnifiedEntryContractCloner.Clone(current))
                    ?? throw new InvalidOperationException(
                        "Aggregate mutation cannot return null.");
                ValidateMutation(current, next);
                _aggregate = UnifiedEntryContractCloner.Clone(next);
            }
            finally
            {
                _aggregateGate.Release();
            }
        }
        finally
        {
            ExitAggregateOperation();
        }
    }

    public async Task<UnifiedRunEventRecord> AppendEventAsync(
        Func<long, UnifiedRunEventRecord> eventFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventFactory);
        EnterAggregateOperation();
        try
        {
            await EnsureNotDisposedAsync(cancellationToken).ConfigureAwait(false);
            await _aggregateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                UnifiedEntryAggregate current = RequireAggregate();
                if (IsTerminal(current.Details.EntryRun.Status))
                {
                    throw new InvalidOperationException(
                        "A terminal unified entry aggregate cannot append events.");
                }

                long sequence;
                lock (_stateGate)
                {
                    sequence = AllocateSequenceLocked();
                }

                UnifiedRunEventRecord value = eventFactory(sequence)
                    ?? throw new InvalidOperationException(
                        "The unified entry event factory cannot return null.");
                if (value.Id == Guid.Empty
                    || value.EntryRunId != current.Details.EntryRun.Id
                    || value.CorrelationId != CorrelationId
                    || value.Sequence != sequence)
                {
                    throw new InvalidOperationException(
                        "The unified entry event factory returned mismatched identity or sequence.");
                }

                UnifiedEntryAggregate next = current.WithEvent(value);
                ValidateAggregateHistory(next, CorrelationId);
                _aggregate = UnifiedEntryContractCloner.Clone(next);
                return value with { };
            }
            finally
            {
                _aggregateGate.Release();
            }
        }
        finally
        {
            ExitAggregateOperation();
        }
    }

    public async Task<UnifiedRunEventRecord> MutateAggregateAndAppendEventAsync(
        Func<UnifiedEntryAggregate, UnifiedEntryAggregate> mutation,
        Func<UnifiedEntryAggregate, long, UnifiedRunEventRecord> eventFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(eventFactory);
        EnterAggregateOperation();
        try
        {
            await EnsureNotDisposedAsync(cancellationToken).ConfigureAwait(false);
            await _aggregateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                UnifiedEntryAggregate current = RequireAggregate();
                if (IsTerminal(current.Details.EntryRun.Status))
                {
                    throw new InvalidOperationException(
                        "A terminal unified entry aggregate cannot be mutated.");
                }

                UnifiedEntryAggregate mutated =
                    mutation(UnifiedEntryContractCloner.Clone(current))
                    ?? throw new InvalidOperationException(
                        "Aggregate mutation cannot return null.");
                ValidateMutation(current, mutated);

                long sequence;
                lock (_stateGate)
                {
                    sequence = AllocateSequenceLocked();
                }

                UnifiedRunEventRecord value = eventFactory(
                    UnifiedEntryContractCloner.Clone(mutated),
                    sequence)
                    ?? throw new InvalidOperationException(
                        "The unified entry event factory cannot return null.");
                if (value.Id == Guid.Empty
                    || value.EntryRunId != current.Details.EntryRun.Id
                    || value.CorrelationId != CorrelationId
                    || value.Sequence != sequence)
                {
                    throw new InvalidOperationException(
                        "The unified entry event factory returned mismatched identity or sequence.");
                }

                UnifiedEntryAggregate next = mutated.WithEvent(value);
                ValidateAggregateHistory(next, CorrelationId);
                _aggregate = UnifiedEntryContractCloner.Clone(next);
                return value with { };
            }
            finally
            {
                _aggregateGate.Release();
            }
        }
        finally
        {
            ExitAggregateOperation();
        }
    }

    public async Task<UnifiedEntryAggregate> GetAggregateSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        EnterAggregateOperation();
        try
        {
            await EnsureNotDisposedAsync(cancellationToken).ConfigureAwait(false);
            await _aggregateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return UnifiedEntryContractCloner.Clone(RequireAggregate());
            }
            finally
            {
                _aggregateGate.Release();
            }
        }
        finally
        {
            ExitAggregateOperation();
        }
    }

    public async Task<UnifiedEntryAggregate> PersistAsync(
        IUnifiedEntryRepository repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        EnterAggregateOperation();
        try
        {
            await EnsureNotDisposedAsync(cancellationToken).ConfigureAwait(false);
            await _aggregateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                UnifiedEntryAggregate current =
                    UnifiedEntryContractCloner.Clone(RequireAggregate());
                UnifiedEntryAggregate saved = await repository.SaveAsync(
                        current,
                        cancellationToken)
                    .ConfigureAwait(false);
                long expectedRevision = checked(current.PersistenceRevision + 1);
                if (saved.Details.EntryRun.Id != current.Details.EntryRun.Id
                    || saved.Conversation.Id != current.Conversation.Id
                    || saved.PersistenceRevision != expectedRevision)
                {
                    throw new InvalidOperationException(
                        "The unified entry repository returned mismatched persistence evidence.");
                }

                _aggregate = UnifiedEntryContractCloner.Clone(saved);
                return UnifiedEntryContractCloner.Clone(saved);
            }
            finally
            {
                _aggregateGate.Release();
            }
        }
        finally
        {
            ExitAggregateOperation();
        }
    }

    public async Task<bool> TryTransitionTerminalAsync(
        UnifiedRunStatus status,
        string errorCode,
        CancellationToken cancellationToken = default)
    {
        if (!IsTerminal(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A terminal transition requires Completed, Failed, or Cancelled.");
        }

        EnterAggregateOperation();
        bool ownsTransition = false;
        try
        {
            await EnsureNotDisposedAsync(cancellationToken).ConfigureAwait(false);
            lock (_stateGate)
            {
                ThrowIfDisposedLocked();
                if (_terminalTransitionCount != 0)
                {
                    return false;
                }

                _terminalTransitionCount = 1;
                ownsTransition = true;
            }

            await _aggregateGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                UnifiedEntryAggregate current = RequireAggregate();
                DateTimeOffset finishedAt = _timeProvider.GetUtcNow();
                UnifiedEntryRunRecord entry = current.Details.EntryRun;
                TimeSpan elapsed = finishedAt - entry.StartedAtUtc;
                UnifiedEntryRunRecord terminal = entry with
                {
                    Status = status,
                    FinishedAtUtc = finishedAt,
                    Duration = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed,
                    ErrorCode = errorCode ?? string.Empty
                };
                _aggregate = current.WithDetails(
                    current.Details.WithEntryRun(terminal));
                ownsTransition = false;
            }
            finally
            {
                _aggregateGate.Release();
            }

            return true;
        }
        finally
        {
            if (ownsTransition)
            {
                lock (_stateGate)
                {
                    _terminalTransitionCount = 0;
                }
            }

            ExitAggregateOperation();
        }
    }

    public void Dispose()
    {
        lock (_stateGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        var failures = new List<Exception>();
        try
        {
            _entryCancellation.Cancel();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        lock (_stateGate)
        {
            foreach (UnifiedAgentExecutionLease lease in _activeAgentLeases.ToArray())
            {
                try
                {
                    lease.DisposeFromOwner();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }

            _activeAgentLeases.Clear();
        }

        try
        {
            _entryCancellation.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            _entryTimeoutCancellation.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        if (failures.Count != 0)
        {
            throw new AggregateException(
                "One or more unified entry cancellation callbacks failed.",
                failures);
        }
    }

    internal void Release(UnifiedAgentExecutionLease lease)
    {
        lock (_stateGate)
        {
            if (!lease.MarkInactive())
            {
                return;
            }

            _activeAgentLeases.Remove(lease);
        }

        lease.DisposeTimeoutCancellation();
    }

    internal string ClassifyCancellation(
        UnifiedAgentExecutionLease? lease,
        CancellationToken invocationToken)
    {
        if (_callerCancellationToken.IsCancellationRequested)
        {
            return UnifiedEntryErrorCodes.Cancelled;
        }

        if (lease?.TimedOut == true)
        {
            return UnifiedEntryErrorCodes.ChildTimeout;
        }

        if (_entryTimeoutCancellation.IsCancellationRequested)
        {
            return UnifiedEntryErrorCodes.EntryTimeout;
        }

        if (invocationToken.IsCancellationRequested)
        {
            return UnifiedEntryErrorCodes.Cancelled;
        }

        return UnifiedEntryErrorCodes.Cancelled;
    }

    internal DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow();

    private Task EnsureNotDisposedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
        }

        return Task.CompletedTask;
    }

    private void EnterAggregateOperation()
    {
        if (_insideAggregateOperation.Value)
        {
            throw new InvalidOperationException(
                "Reentrant unified entry aggregate operations are not supported.");
        }

        _insideAggregateOperation.Value = true;
    }

    private void ExitAggregateOperation() =>
        _insideAggregateOperation.Value = false;

    private static bool HasAncestorVersion(
        UnifiedAgentExecutionLease parent,
        Guid agentVersionId)
    {
        for (UnifiedAgentExecutionLease? current = parent;
             current is not null;
             current = current.Parent)
        {
            if (current.AgentVersionId == agentVersionId)
            {
                return true;
            }
        }

        return false;
    }

    private long AllocateSequenceLocked()
    {
        if (_sequence == long.MaxValue)
        {
            throw new UnifiedEntryException(
                UnifiedEntryErrorCodes.SequenceExhausted,
                "The unified entry event sequence is exhausted.");
        }

        return ++_sequence;
    }

    private static void ValidateAggregateHistory(
        UnifiedEntryAggregate aggregate,
        Guid correlationId)
    {
        UnifiedEntryRunRecord entry = aggregate.Details.EntryRun;
        if (entry.CorrelationId != correlationId || entry.Id == Guid.Empty)
        {
            throw new ArgumentException("The unified entry aggregate identity is invalid.");
        }

        long previous = 0;
        var ids = new HashSet<Guid>();
        foreach (UnifiedRunEventRecord value in aggregate.Events)
        {
            if (value.Id == Guid.Empty
                || !ids.Add(value.Id)
                || value.EntryRunId != entry.Id
                || value.CorrelationId != correlationId
                || value.Sequence <= previous)
            {
                throw new ArgumentException(
                    "Unified entry events require unique IDs and strictly increasing positive sequences with matching identities.");
            }

            previous = value.Sequence;
        }

        bool terminal = IsTerminal(entry.Status);
        bool hasFinishedAt = entry.FinishedAtUtc.HasValue;
        bool hasDuration = entry.Duration.HasValue;
        if (terminal
            ? !hasFinishedAt || !hasDuration || entry.Duration < TimeSpan.Zero
            : hasFinishedAt || hasDuration)
        {
            throw new ArgumentException(
                "Unified entry terminal timestamps and duration are inconsistent.");
        }
    }

    private static void ValidateMutation(
        UnifiedEntryAggregate current,
        UnifiedEntryAggregate next)
    {
        if (next.PersistenceRevision != current.PersistenceRevision)
        {
            throw new InvalidOperationException(
                "General aggregate mutation must preserve the owned persistence revision.");
        }

        UnifiedEntryRunRecord before = current.Details.EntryRun;
        UnifiedEntryRunRecord after = next.Details.EntryRun;
        bool lifecyclePreserved = before.Status == after.Status
            || before.Status == UnifiedRunStatus.Running
                && after.Status == UnifiedRunStatus.WaitingForApproval
                && after.FinishedAtUtc is null
                && after.Duration is null;
        if (before.Id != after.Id
            || before.ConversationId != after.ConversationId
            || before.CorrelationId != after.CorrelationId
            || before.MainAgentVersionId != after.MainAgentVersionId
            || before.StartedAtUtc != after.StartedAtUtc
            || !lifecyclePreserved
            || before.FinishedAtUtc != after.FinishedAtUtc
            || before.Duration != after.Duration)
        {
            throw new InvalidOperationException(
                "General aggregate mutation cannot replace entry identity or terminal lifecycle.");
        }

        if (next.Conversation.Id != current.Conversation.Id
            || next.Events.Count != current.Events.Count)
        {
            throw new InvalidOperationException(
                "General aggregate mutation cannot replace existing aggregate history.");
        }

        for (int index = 0; index < current.Events.Count; index++)
        {
            if (next.Events[index] != current.Events[index])
            {
                throw new InvalidOperationException(
                    "General aggregate mutation cannot replace existing events.");
            }
        }

        try
        {
            ValidateAggregateHistory(next, before.CorrelationId);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "Aggregate mutation introduced invalid event history.",
                exception);
        }
    }

    private UnifiedEntryAggregate RequireAggregate() =>
        _aggregate
        ?? throw new InvalidOperationException(
            "This execution scope was created without a unified entry aggregate.");

    private static UnifiedEntryLimits ValidateLimits(UnifiedEntryLimits limits)
    {
        if (limits.MaxAgentDepth < 0
            || limits.MaxChildCalls < 0
            || limits.MaxOrchestrationCalls < 0
            || limits.MaxMcpCalls < 0
            || limits.InternalPayloadUtf8Bytes < 0
            || limits.MaxMcpResultUtf8Bytes < 0
            || !IsSupportedTimeout(limits.EntryTimeout)
            || !IsSupportedTimeout(limits.ChildTimeout))
        {
            throw new ArgumentOutOfRangeException(
                nameof(limits),
                "Unified entry limits must be non-negative and timeouts must be positive supported CancelAfter values.");
        }

        return limits with { };
    }

    private static bool IsSupportedTimeout(TimeSpan value) =>
        value > TimeSpan.Zero
        && value.TotalMilliseconds <= MaxCancelAfterMilliseconds;

    private static bool IsTerminal(UnifiedRunStatus status) =>
        status is UnifiedRunStatus.Completed
            or UnifiedRunStatus.Failed
            or UnifiedRunStatus.Cancelled;

    private static void ValidateVersionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "An immutable Agent version ID is required.",
                nameof(value));
        }
    }

    private static UnifiedEntryException InvalidState(string message) =>
        new(UnifiedEntryErrorCodes.InvalidState, message);

    private void ThrowIfDisposedLocked() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}

public sealed class UnifiedAgentExecutionLease : IDisposable
{
    private readonly CancellationTokenSource? _linkedCancellation;
    private readonly CancellationTokenSource? _timeoutCancellation;
    private int _active = 1;

    internal UnifiedAgentExecutionLease(
        UnifiedEntryExecutionScope owner,
        Guid agentVersionId,
        int depth,
        UnifiedAgentExecutionLease? parent,
        CancellationToken cancellationToken,
        CancellationTokenSource? timeoutCancellation,
        CancellationTokenSource? linkedCancellation = null)
    {
        Owner = owner;
        AgentVersionId = agentVersionId;
        Depth = depth;
        Parent = parent;
        CancellationToken = cancellationToken;
        _timeoutCancellation = timeoutCancellation;
        _linkedCancellation = linkedCancellation;
    }

    internal UnifiedEntryExecutionScope Owner { get; }

    internal UnifiedAgentExecutionLease? Parent { get; }

    internal bool IsActive => Volatile.Read(ref _active) != 0;

    internal bool TimedOut => _timeoutCancellation?.IsCancellationRequested == true;

    public Guid AgentVersionId { get; }

    public int Depth { get; }

    public CancellationToken CancellationToken { get; }

    public void Dispose() => Owner.Release(this);

    internal bool MarkInactive() =>
        Interlocked.Exchange(ref _active, 0) != 0;

    internal void DisposeTimeoutCancellation()
    {
        _linkedCancellation?.Dispose();
        _timeoutCancellation?.Dispose();
    }

    internal void DisposeFromOwner()
    {
        MarkInactive();
        DisposeTimeoutCancellation();
    }
}

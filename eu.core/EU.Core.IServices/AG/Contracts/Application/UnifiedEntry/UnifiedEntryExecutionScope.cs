#nullable enable

using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>
/// 跟踪统一入口执行树、调用限额和取消状态。
/// </summary>
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

    #region 构造（UnifiedEntryExecutionScope）
    /// <summary>
    /// 构造（UnifiedEntryExecutionScope）
    /// </summary>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="limits">执行次数、时间或载荷的限制配置。</param>
    /// <param name="correlationId">关联当前请求与运行记录的标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <param name="timeProvider">用于读取当前时间的时间提供器。</param>
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
    #endregion

    /// <summary>
    /// 获取统一入口执行限制。
    /// </summary>
    public UnifiedEntryLimits Limits { get; }

    /// <summary>
    /// 获取关联标识。
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// 获取统一入口级取消令牌。
    /// </summary>
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

    #region 登记子运行的业务查询权威结果（TryRegisterBusinessQueryResult）
    /// <summary>
    /// 登记子运行的业务查询权威结果（TryRegisterBusinessQueryResult）。
    /// </summary>
    /// <param name="childRunId">子运行标识。</param>
    /// <param name="result">待关联到子运行的非空业务查询权威结果。</param>
    /// <returns>登记成功时返回 true；子运行标识为空、执行作用域已释放或该子运行已登记结果时返回 false。</returns>
    /// <exception cref="ArgumentNullException">result 为 null。</exception>
    public bool TryRegisterBusinessQueryResult(Guid childRunId, BusinessQueryAuthoritativeResult result)
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
    #endregion

    #region 获取（GetBusinessQueryResults）
    /// <summary>
    /// 获取（GetBusinessQueryResults）
    /// </summary>
    /// <returns>当前范围已登记的业务查询权威结果副本集合。</returns>
    public IReadOnlyList<BusinessQueryAuthoritativeResult> GetBusinessQueryResults()
    {
        lock (_stateGate)
        {
            return _businessQueryResults.Values
                .Select(value => value with { })
                .ToArray();
        }
    }
    #endregion

    #region 处理（NextSequence）
    /// <summary>
    /// 处理（NextSequence）
    /// </summary>
    /// <returns>加锁分配的下一个事件序号；范围已释放或序号耗尽时抛出异常。</returns>
    public long NextSequence()
    {
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
            return AllocateSequenceLocked();
        }
    }
    #endregion

    #region 处理（EnterMainAgent）
    /// <summary>
    /// 处理（EnterMainAgent）
    /// </summary>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <returns>深度为零且绑定入口取消令牌的主 Agent 执行租约；重复进入时抛出 InvalidState 异常。</returns>
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
    #endregion

    #region 处理（ReserveChildAgent）
    /// <summary>
    /// 处理（ReserveChildAgent）
    /// </summary>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="parent">父级 Agent 执行租约。</param>
    /// <returns>通过父租约、循环、深度及调用次数检查后预留的子 Agent 执行租约；不满足限制时抛出异常。</returns>
    public UnifiedAgentExecutionLease ReserveChildAgent(Guid agentVersionId, UnifiedAgentExecutionLease parent)
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
    #endregion

    #region 预占一次编排调用额度（ReserveOrchestration）
    /// <summary>
    /// 预占一次编排调用额度（ReserveOrchestration）。
    /// </summary>
    /// <returns>当前计数未达到编排调用上限且成功增加计数时返回 true；额度已耗尽时返回 false。</returns>
    /// <exception cref="ObjectDisposedException">执行作用域已释放。</exception>
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
    #endregion

    #region 处理（ReserveAsync）
    /// <summary>
    /// 处理（ReserveAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功占用一次 MCP 调用预算的允许结果，或调用次数超限的拒绝结果。</returns>
    public ValueTask<AgentMcpCallGuardResult> ReserveAsync(CancellationToken cancellationToken = default)
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
    #endregion

    #region 处理（ReserveAsync）
    /// <summary>
    /// 处理（ReserveAsync）
    /// </summary>
    /// <param name="resultUtf8Bytes">工具结果按 UTF-8 编码后的字节数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功预留结果字节预算的允许结果，或容量超限的拒绝结果；负字节数抛出参数异常。</returns>
    ValueTask<AgentMcpResultGuardResult> IAgentMcpResultGuard.ReserveAsync(int resultUtf8Bytes, CancellationToken cancellationToken)
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
    #endregion

    #region 处理（MutateAggregateAsync）
    /// <summary>
    /// 处理（MutateAggregateAsync）
    /// </summary>
    /// <param name="mutation">用于修改聚合状态的委托。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    public async Task MutateAggregateAsync(Func<UnifiedEntryAggregate, UnifiedEntryAggregate> mutation, CancellationToken cancellationToken = default)
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
    #endregion

    #region 处理（AppendEventAsync）
    /// <summary>
    /// 处理（AppendEventAsync）
    /// </summary>
    /// <param name="eventFactory">根据事件序号及相应聚合状态创建运行事件的工厂委托。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功分配序号、校验并追加到内存聚合的事件副本；不直接持久化。</returns>
    public async Task<UnifiedRunEventRecord> AppendEventAsync(Func<long, UnifiedRunEventRecord> eventFactory, CancellationToken cancellationToken = default)
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
    #endregion

    #region 处理（MutateAggregateAndAppendEventAsync）
    /// <summary>
    /// 处理（MutateAggregateAndAppendEventAsync）
    /// </summary>
    /// <param name="mutation">用于修改聚合状态的委托。</param>
    /// <param name="eventFactory">根据事件序号及相应聚合状态创建运行事件的工厂委托。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>原子更新内存聚合并追加事件后的事件副本；不直接持久化。</returns>
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
    #endregion

    #region 获取（GetAggregateSnapshotAsync）
    /// <summary>
    /// 获取（GetAggregateSnapshotAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>在聚合锁保护下复制的当前内存聚合快照。</returns>
    public async Task<UnifiedEntryAggregate> GetAggregateSnapshotAsync(CancellationToken cancellationToken = default)
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
    #endregion

    #region 处理（PersistAsync）
    /// <summary>
    /// 处理（PersistAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>持久化成功且标识、版本证据通过校验的聚合副本；返回证据不匹配时抛出异常。</returns>
    public async Task<UnifiedEntryAggregate> PersistAsync(IUnifiedEntryRepository repository, CancellationToken cancellationToken = default)
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
    #endregion

    #region 将内存中的统一入口聚合切换为终态（TryTransitionTerminalAsync）
    /// <summary>
    /// 将内存中的统一入口聚合切换为终态（TryTransitionTerminalAsync）。
    /// </summary>
    /// <param name="status">目标终态，只允许 Completed、Failed 或 Cancelled。</param>
    /// <param name="errorCode">写入运行记录的终态错误码；为 null 时按空字符串保存。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步操作结果：取得本次终态转换权并更新内存聚合时返回 true；已有终态转换被认领时返回 false。</returns>
    /// <exception cref="ArgumentOutOfRangeException">status 不是受支持的终态。</exception>
    public async Task<bool> TryTransitionTerminalAsync(UnifiedRunStatus status, string errorCode, CancellationToken cancellationToken = default)
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
    #endregion

    #region 释放资源（Dispose）
    /// <summary>
    /// 释放资源（Dispose）
    /// </summary>
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
    #endregion

    #region 处理（Release）
    /// <summary>
    /// 处理（Release）
    /// </summary>
    /// <param name="lease">执行租约。</param>
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
    #endregion

    #region 处理（ClassifyCancellation）
    /// <summary>
    /// 处理（ClassifyCancellation）
    /// </summary>
    /// <param name="lease">执行租约。</param>
    /// <param name="invocationToken">当前调用使用的取消令牌。</param>
    /// <returns>按调用方取消、子 Agent 超时、入口超时的优先级判定的错误码；其余情况归为普通取消。</returns>
    internal string ClassifyCancellation(UnifiedAgentExecutionLease? lease, CancellationToken invocationToken)
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
    #endregion

    #region 获取（GetUtcNow）
    /// <summary>
    /// 获取（GetUtcNow）
    /// </summary>
    /// <returns>当前时间提供器给出的 UTC 时间。</returns>
    internal DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow();
    #endregion

    #region 检查前置条件（EnsureNotDisposedAsync）
    /// <summary>
    /// 检查前置条件（EnsureNotDisposedAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    private Task EnsureNotDisposedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateGate)
        {
            ThrowIfDisposedLocked();
        }

        return Task.CompletedTask;
    }
    #endregion

    #region 处理（EnterAggregateOperation）
    /// <summary>
    /// 处理（EnterAggregateOperation）
    /// </summary>
    private void EnterAggregateOperation()
    {
        if (_insideAggregateOperation.Value)
        {
            throw new InvalidOperationException(
                "Reentrant unified entry aggregate operations are not supported.");
        }

        _insideAggregateOperation.Value = true;
    }
    #endregion

    #region 处理（ExitAggregateOperation）
    /// <summary>
    /// 处理（ExitAggregateOperation）
    /// </summary>
    private void ExitAggregateOperation() =>
        _insideAggregateOperation.Value = false;
    #endregion

    #region 检查父级执行链中是否包含指定版本（HasAncestorVersion）
    /// <summary>
    /// 检查父级执行链中是否包含指定版本（HasAncestorVersion）。
    /// </summary>
    /// <param name="parent">父级 Agent 执行租约。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <returns>从给定父租约开始向上遍历，存在相同 Agent 版本时返回 true，否则返回 false。</returns>
    private static bool HasAncestorVersion(UnifiedAgentExecutionLease parent, Guid agentVersionId)
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
    #endregion

    #region 处理（AllocateSequenceLocked）
    /// <summary>
    /// 处理（AllocateSequenceLocked）
    /// </summary>
    /// <returns>递增后的事件序号；达到 long 最大值时抛出 SequenceExhausted 异常。</returns>
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
    #endregion

    #region 校验（ValidateAggregateHistory）
    /// <summary>
    /// 校验（ValidateAggregateHistory）
    /// </summary>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="correlationId">关联当前请求与运行记录的标识。</param>
    private static void ValidateAggregateHistory(UnifiedEntryAggregate aggregate, Guid correlationId)
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
    #endregion

    #region 校验（ValidateMutation）
    /// <summary>
    /// 校验（ValidateMutation）
    /// </summary>
    /// <param name="current">当前数据。</param>
    /// <param name="next">修改后待校验的聚合状态。</param>
    private static void ValidateMutation(UnifiedEntryAggregate current, UnifiedEntryAggregate next)
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
    #endregion

    #region 处理（RequireAggregate）
    /// <summary>
    /// 处理（RequireAggregate）
    /// </summary>
    /// <returns>当前已初始化的内存聚合；未设置聚合时抛出 InvalidOperationException。</returns>
    private UnifiedEntryAggregate RequireAggregate() =>
        _aggregate
        ?? throw new InvalidOperationException(
            "This execution scope was created without a unified entry aggregate.");
    #endregion

    #region 校验（ValidateLimits）
    /// <summary>
    /// 校验（ValidateLimits）
    /// </summary>
    /// <param name="limits">执行次数、时间或载荷的限制配置。</param>
    /// <returns>通过非负预算及受支持超时检查的限制配置副本；无效配置抛出参数异常。</returns>
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
    #endregion

    #region 检查取消计时器是否支持超时时长（IsSupportedTimeout）
    /// <summary>
    /// 检查取消计时器是否支持超时时长（IsSupportedTimeout）。
    /// </summary>
    /// <param name="value">拟用于取消计时器的超时时长。</param>
    /// <returns>时长大于零且总毫秒数不超过 MaxCancelAfterMilliseconds 时返回 true，否则返回 false。</returns>
    private static bool IsSupportedTimeout(TimeSpan value) =>
        value > TimeSpan.Zero
        && value.TotalMilliseconds <= MaxCancelAfterMilliseconds;
    #endregion

    #region 判断统一入口运行是否处于终态（IsTerminal）
    /// <summary>
    /// 判断统一入口运行是否处于终态（IsTerminal）。
    /// </summary>
    /// <param name="status">待检查的统一入口运行状态。</param>
    /// <returns>状态为 Completed、Failed 或 Cancelled 时返回 true，否则返回 false。</returns>
    private static bool IsTerminal(UnifiedRunStatus status) =>
        status is UnifiedRunStatus.Completed
            or UnifiedRunStatus.Failed
            or UnifiedRunStatus.Cancelled;
    #endregion

    #region 校验（ValidateVersionId）
    /// <summary>
    /// 校验（ValidateVersionId）
    /// </summary>
    /// <param name="value">不允许为 Guid.Empty 的 Agent 版本标识。</param>
    private static void ValidateVersionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "An immutable Agent version ID is required.",
                nameof(value));
        }
    }
    #endregion

    #region 处理（InvalidState）
    /// <summary>
    /// 处理（InvalidState）
    /// </summary>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含指定消息的统一入口 InvalidState 异常。</returns>
    private static UnifiedEntryException InvalidState(string message) =>
        new(UnifiedEntryErrorCodes.InvalidState, message);
    #endregion

    #region 处理（ThrowIfDisposedLocked）
    /// <summary>
    /// 处理（ThrowIfDisposedLocked）
    /// </summary>
    private void ThrowIfDisposedLocked() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
    #endregion
}

/// <summary>
/// 表示统一入口中一次 Agent 执行占用的可释放租约。
/// </summary>
public sealed class UnifiedAgentExecutionLease : IDisposable
{
    private readonly CancellationTokenSource? _linkedCancellation;
    private readonly CancellationTokenSource? _timeoutCancellation;
    private int _active = 1;

    #region 构造（UnifiedAgentExecutionLease）
    /// <summary>
    /// 构造（UnifiedAgentExecutionLease）
    /// </summary>
    /// <param name="owner">所属执行对象。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="depth">当前递归或执行树深度。</param>
    /// <param name="parent">父级 Agent 执行租约。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <param name="timeoutCancellation">用于触发执行超时的取消令牌源。</param>
    /// <param name="linkedCancellation">组合外部取消信号的取消令牌源。</param>
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
    #endregion

    internal UnifiedEntryExecutionScope Owner { get; }

    internal UnifiedAgentExecutionLease? Parent { get; }

    internal bool IsActive => Volatile.Read(ref _active) != 0;

    internal bool TimedOut => _timeoutCancellation?.IsCancellationRequested == true;

    /// <summary>
    /// 获取 Agent 版本标识。
    /// </summary>
    public Guid AgentVersionId { get; }

    /// <summary>
    /// 获取当前 Agent 在执行树中的深度。
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// 获取当前 Agent 执行的取消令牌。
    /// </summary>
    public CancellationToken CancellationToken { get; }

    #region 释放资源（Dispose）
    /// <summary>
    /// 释放资源（Dispose）
    /// </summary>
    public void Dispose() => Owner.Release(this);
    #endregion

    #region 原子地将执行租约标记为非活动（MarkInactive）
    /// <summary>
    /// 原子地将执行租约标记为非活动（MarkInactive）。
    /// </summary>
    /// <returns>租约原先处于活动状态且本次将其改为非活动时返回 true；原先已非活动时返回 false。</returns>
    internal bool MarkInactive() =>
        Interlocked.Exchange(ref _active, 0) != 0;
    #endregion

    #region 释放资源（DisposeTimeoutCancellation）
    /// <summary>
    /// 释放资源（DisposeTimeoutCancellation）
    /// </summary>
    internal void DisposeTimeoutCancellation()
    {
        _linkedCancellation?.Dispose();
        _timeoutCancellation?.Dispose();
    }
    #endregion

    #region 释放资源（DisposeFromOwner）
    /// <summary>
    /// 释放资源（DisposeFromOwner）
    /// </summary>
    internal void DisposeFromOwner()
    {
        MarkInactive();
        DisposeTimeoutCancellation();
    }
    #endregion
}

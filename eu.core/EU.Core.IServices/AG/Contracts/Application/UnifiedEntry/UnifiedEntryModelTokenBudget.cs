#nullable enable
using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>执行作用域拥有的共享预算；只在启用限制时串行化供应商请求，不持有工具执行阶段。</summary>
/// <param name="maximumTokens">已经由执行作用域校验为正数的执行树总 Token 阈值。</param>
/// <param name="scopeCancellation">作用域取消令牌，用于取消仍在等待额度的请求。</param>
/// <param name="warningPercent">接近上限的预警百分比；null 只关闭预警。</param>
/// <param name="telemetry">可选宿主监控，不拥有其生命周期。</param>
internal sealed class UnifiedEntryModelTokenBudget(long maximumTokens, CancellationToken scopeCancellation, int? warningPercent, IAgentRuntimeTelemetry? telemetry) : IAgentModelTokenBudget, IDisposable
{
    private readonly object _stateGate = new();
    private readonly SemaphoreSlim _requests = new(1, 1);
    private long _usedTokens;
    private string? _denialCode;
    private bool _disposed;
    private readonly AgentTokenBudgetMonitor _monitor = new(AgentTokenBudgetScope.ExecutionTree, warningPercent, telemetry);

    #region 取得模型请求租约（ReserveAsync）
    /// <summary>串行读取剩余额度；取消等待不会污染其他运行的账本。</summary>
    /// <param name="cancellationToken">当前模型请求的取消令牌。</param>
    /// <returns>只覆盖一次供应商请求的额度租约。</returns>
    public async ValueTask<IAgentModelTokenBudgetLease> ReserveAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scopeCancellation);
        await _requests.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            lock (_stateGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_denialCode is not null) throw Failure(_denialCode);
                if (_usedTokens >= maximumTokens) throw Failure(AgentRunErrorCodes.ModelTokenBudgetExceeded);
                return new Lease(this, maximumTokens - _usedTokens);
            }
        }
        catch
        {
            _requests.Release();
            throw;
        }
    }
    #endregion

    #region 结算模型响应（Complete）
    /// <summary>只累计真实总用量；达到额度后当前响应仍可完成，但下一请求被拒绝。</summary>
    /// <param name="totalTokens">单次响应的总 Token。</param>
    /// <param name="invalidTotal">本轮是否包含负总用量。</param>
    private void Complete(long? totalTokens, bool invalidTotal)
    {
        string? denial;
        long used;
        lock (_stateGate)
        {
            if (invalidTotal || totalTokens is null or < 0)
                _denialCode ??= AgentRunErrorCodes.ModelTokenUsageUnavailable;
            else if (totalTokens.Value > maximumTokens - _usedTokens)
                _denialCode ??= AgentRunErrorCodes.ModelTokenBudgetExceeded;
            else
                _usedTokens += totalTokens.Value;
            denial = _denialCode;
            used = _usedTokens;
        }
        if (denial == AgentRunErrorCodes.ModelTokenUsageUnavailable) _monitor.Record(AgentTokenBudgetSignal.UsageUnavailable);
        else if (denial == AgentRunErrorCodes.ModelTokenBudgetExceeded) _monitor.Record(AgentTokenBudgetSignal.Exceeded);
        else _monitor.Observe(used, maximumTokens);
        if (denial is not null) throw Failure(denial);
    }
    #endregion

    #region 释放请求租约（Release）
    /// <summary>取消、异常或提前释放后的未知消耗关闭后续请求，不重复扣减。</summary>
    /// <param name="unknownSpend">请求已发出但没有完成结算。</param>
    private void Release(bool unknownSpend)
    {
        lock (_stateGate)
        {
            if (unknownSpend) _denialCode ??= AgentRunErrorCodes.ModelTokenUsageUnavailable;
        }
        if (unknownSpend) _monitor.Record(AgentTokenBudgetSignal.UsageUnavailable);
        _requests.Release();
    }
    #endregion

    #region 创建预算错误（Failure）
    /// <summary>使用现有领域错误码，不包含模型输入或凭据。</summary>
    /// <param name="errorCode">预算超限或用量不可用错误码。</param>
    /// <returns>可用于运行终态审计的固定异常。</returns>
    private static AgentRuntimeException Failure(string errorCode) => new(errorCode,
        errorCode == AgentRunErrorCodes.ModelTokenUsageUnavailable
            ? "The model did not report usable total Token usage for the enabled execution-tree budget."
            : "The configured execution-tree Token budget was exceeded or exhausted.");
    #endregion

    #region 关闭共享预算（Dispose）
    /// <summary>作用域关闭后拒绝新请求；仍允许在途租约释放，等待者由作用域令牌取消。</summary>
    public void Dispose()
    {
        lock (_stateGate) _disposed = true;
        // 不创建 WaitHandle；保留托管门直至在途等待/租约退出，避免释放与 Release 竞争。
    }
    #endregion

    private sealed class Lease(UnifiedEntryModelTokenBudget owner, long remainingTokens) : IAgentModelTokenBudgetLease
    {
        private bool _started;
        private bool _completed;
        private int _released;
        public long RemainingTokens { get; } = remainingTokens;

        #region 标记请求开始（MarkStarted）
        /// <summary>租约只供当前请求使用，不能在释放后再次发出请求。</summary>
        public void MarkStarted()
        {
            ObjectDisposedException.ThrowIf(_released != 0, this);
            _started = true;
        }
        #endregion

        #region 结算一次响应（Complete）
        /// <summary>同一租约只允许结算一次，防止重复快照重复扣减。</summary>
        /// <param name="totalTokens">单次响应的总用量。</param>
        /// <param name="invalidTotal">是否包含非法负用量。</param>
        public void Complete(long? totalTokens, bool invalidTotal)
        {
            ObjectDisposedException.ThrowIf(_released != 0, this);
            if (!_started || _completed) throw new InvalidOperationException("The model request lease cannot be settled twice or before dispatch.");
            _completed = true;
            owner.Complete(totalTokens, invalidTotal);
        }
        #endregion

        #region 释放单次请求（Dispose）
        /// <summary>只释放一次；未发出请求不收费，已发出而未结算则标为未知。</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0) owner.Release(_started && !_completed);
        }
        #endregion
    }
}

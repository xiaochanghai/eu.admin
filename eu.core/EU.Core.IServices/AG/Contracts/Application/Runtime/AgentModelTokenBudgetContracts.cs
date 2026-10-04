#nullable enable

namespace EU.Core.IServices.Runtime;

/// <summary>当前执行树共享的模型 Token 预算；不是用户周期配额或跨进程账本。</summary>
public interface IAgentModelTokenBudget
{
    #region 取得单次模型请求额度（ReserveAsync）
    /// <summary>等待前一模型请求结算，避免并行子运行分别使用同一剩余额度。</summary>
    /// <param name="cancellationToken">等待额度的取消令牌；等待取消不计作已发出请求。</param>
    /// <returns>仅覆盖一次供应商请求的租约，必须在执行工具前释放。</returns>
    ValueTask<IAgentModelTokenBudgetLease> ReserveAsync(CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>单次模型请求预算租约；未完成的已发出请求不能按零用量结算。</summary>
public interface IAgentModelTokenBudgetLease : IDisposable
{
    /// <summary>取得租约时可用的总 Token；只用于收紧输出上限，不冒充输入估算。</summary>
    long RemainingTokens { get; }

    #region 标记模型请求已发出（MarkStarted）
    /// <summary>在调用供应商前标记；未发出请求的租约释放不扣额度。</summary>
    void MarkStarted();
    #endregion

    #region 按供应商用量结算（Complete）
    /// <summary>结算一次实际响应；缺失、非法或超限用量会拒绝执行树的后续模型请求。</summary>
    /// <param name="totalTokens">供应商报告的单次响应总 Token，不自行估算或补零。</param>
    /// <param name="invalidTotal">该响应是否曾报告负总用量。</param>
    void Complete(long? totalTokens, bool invalidTotal);
    #endregion
}

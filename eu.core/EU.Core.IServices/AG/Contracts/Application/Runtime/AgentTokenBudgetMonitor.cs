#nullable enable

namespace EU.Core.IServices.Runtime;

/// <summary>一个预算生命周期内的去重信号；只做监控，不决定是否允许模型请求。</summary>
public sealed class AgentTokenBudgetMonitor
{
    private readonly AgentTokenBudgetScope _scope;
    private readonly int? _warningPercent;
    private readonly IAgentRuntimeTelemetry? _telemetry;
    private int _reported;

    #region 初始化预算监控（AgentTokenBudgetMonitor）
    /// <summary>创建当前预算的监控，不保存用户、运行标识或模型输入。</summary>
    /// <param name="scope">固定预算范围。</param>
    /// <param name="warningPercent">预警百分比，1–99；null 只关闭接近上限预警。</param>
    /// <param name="telemetry">宿主可选监控，不拥有其生命周期。</param>
    public AgentTokenBudgetMonitor(AgentTokenBudgetScope scope, int? warningPercent, IAgentRuntimeTelemetry? telemetry)
    {
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        if (warningPercent is < 1 or > 99) throw new ArgumentOutOfRangeException(nameof(warningPercent));
        _scope = scope;
        _warningPercent = warningPercent;
        _telemetry = telemetry;
    }
    #endregion

    #region 按真实用量观察阈值（Observe）
    /// <summary>不估算未知消耗；跨过上限时只报告耗尽或超限，不补发接近上限预警。</summary>
    /// <param name="usedTokens">当前预算已知消耗，不能用未知值补零。</param>
    /// <param name="maximumTokens">当前预算上限，必须为正数。</param>
    public void Observe(long usedTokens, long maximumTokens)
    {
        if (usedTokens < 0 || maximumTokens <= 0) return;
        if (usedTokens > maximumTokens) Record(AgentTokenBudgetSignal.Exceeded);
        else if (usedTokens == maximumTokens) Record(AgentTokenBudgetSignal.Exhausted);
        else if (_warningPercent is int percent && (decimal)usedTokens * 100 >= (decimal)maximumTokens * percent)
            Record(AgentTokenBudgetSignal.Warning);
    }
    #endregion

    #region 去重记录固定信号（Record）
    /// <summary>同一范围及信号每个预算生命周期只记录一次；监控异常不能放宽预算或使查询失败。</summary>
    /// <param name="signal">固定预算状态。</param>
    public void Record(AgentTokenBudgetSignal signal)
    {
        if (!Enum.IsDefined(signal) || _telemetry is null) return;
        int mask = 1 << (int)signal;
        if ((Interlocked.Or(ref _reported, mask) & mask) != 0) return;
        try { _telemetry.RecordTokenBudget(_scope, signal); }
        catch (Exception)
        {
            // 监控为尽力而为；不传播日志/导出器异常，不改变预算账本或终态审计。
        }
    }
    #endregion
}

#nullable enable

namespace EU.Core.IServices.Runtime;

/// <summary>模型返回的 Token 用量完整性；未知值不得替换为零。</summary>
public enum AgentTokenUsageStatus
{
    /// <summary>未收到可用统计。</summary>
    Unknown,
    /// <summary>只收到部分响应统计，或流未正常结束。</summary>
    Partial,
    /// <summary>正常结束且所有已识别模型响应均返回完整统计。</summary>
    Reported
}

/// <summary>一次 Agent 模型循环的用量和耗时，不包含输入、输出或凭据。</summary>
/// <param name="InputTokens">已收到的输入 Token 数；未报告时为 null。</param>
/// <param name="OutputTokens">已收到的输出 Token 数；未报告时为 null。</param>
/// <param name="TotalTokens">模型报告的总 Token 数；不自行推算。</param>
/// <param name="Status">统计完整性，Partial 的数值仅代表已知部分。</param>
/// <param name="ModelDurationMilliseconds">模型循环总耗时，包含循环内工具等待。</param>
/// <param name="TimeToFirstTextMilliseconds">首段非空文本等待时间；没有文本时为 null。</param>
public sealed record AgentModelUsage(long? InputTokens, long? OutputTokens, long? TotalTokens, AgentTokenUsageStatus Status, long? ModelDurationMilliseconds, long? TimeToFirstTextMilliseconds);

/// <summary>宿主提供的运行监控，缺省不影响运行；标签不包含用户或运行标识。</summary>
public interface IAgentRuntimeTelemetry
{
    #region 记录预算信号
    /// <summary>记录固定范围和状态的预算信号；缺省不实现，不改变预算拦截或运行终态。</summary>
    /// <param name="scope">输出、单运行或执行树预算范围，不包含运行标识。</param>
    /// <param name="signal">接近上限、耗尽、超限、用量不可用或输出截断信号。</param>
    void RecordTokenBudget(AgentTokenBudgetScope scope, AgentTokenBudgetSignal signal) { }
    #endregion

    #region 记录运行终态
    /// <summary>记录一次运行的终态、实际收到的用量和耗时。</summary>
    /// <param name="status">运行终态，审批暂停单独计数。</param>
    /// <param name="usage">模型统计；没有调用模型或未收到事件时为 null。</param>
    /// <param name="durationMilliseconds">本次执行的总耗时。</param>
    void RecordRun(AgentRunStatus status, AgentModelUsage? usage, long durationMilliseconds);
    #endregion

    #region 记录工具终态
    /// <summary>记录一次 MCP 工具调用的终态和耗时。</summary>
    /// <param name="status">成功、失败或阻止。</param>
    /// <param name="durationMilliseconds">工具调用耗时。</param>
    void RecordTool(AgentRunEventKind status, long durationMilliseconds);
    #endregion
}

/// <summary>预算监控的固定范围，避免按用户、运行或模型创建指标标签。</summary>
public enum AgentTokenBudgetScope
{
    /// <summary>一次模型响应的实际输出上限。</summary>
    ModelOutput,
    /// <summary>一次 Agent 模型循环的累计总用量。</summary>
    AgentRun,
    /// <summary>统一入口主、子 Agent 及编排节点共享的累计总用量。</summary>
    ExecutionTree
}

/// <summary>预算监控的固定信号；达到上限不等于当前运行失败。</summary>
public enum AgentTokenBudgetSignal
{
    /// <summary>已知消耗达到预警百分比，但未达到预算上限。</summary>
    Warning,
    /// <summary>已知消耗恰好达到上限；累计预算拒绝后续请求，输出预算仅表示本次响应达到上限。</summary>
    Exhausted,
    /// <summary>供应商已报告的消耗超过上限。</summary>
    Exceeded,
    /// <summary>请求已发出但未收到可用的完整总用量。</summary>
    UsageUnavailable,
    /// <summary>输出被供应商以 length 截断；不等于已报告消耗超过上限。</summary>
    OutputLimited
}

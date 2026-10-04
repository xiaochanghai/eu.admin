namespace EU.Core.Agent.Runtime;

/// <summary>工具调用与模型输入输出预算；模型地址、凭据、超时及思考设置由模型配置解析器提供。</summary>
/// <param name="ToolCallTimeout">单次工具调用允许的最大耗时。</param>
/// <param name="MaximumToolResultBytes">单次 MCP 工具返回的最大 UTF-8 字节数。</param>
/// <param name="MaximumModelOutputBytes">一次模型执行内累计文本输出的最大 UTF-8 字节数。</param>
/// <param name="MaximumModelOutputEvents">一次模型执行允许的最大文本输出事件数。</param>
/// <param name="MaximumModelInputBytes">模型初始输入、历史、知识、指令和工具定义的累计字节上限。</param>
/// <param name="MaximumToolArgumentBytes">单次工具参数的最大 UTF-8 字节数。</param>
/// <param name="MaximumInternalToolResultBytes">单次内部工具结果的最大 UTF-8 字节数。</param>
/// <param name="MaximumInternalToolCalls">一次模型执行允许的内部工具调用次数。</param>
/// <param name="MaximumMcpToolCalls">一次模型执行允许的 MCP 工具调用次数。</param>
/// <param name="MaximumModelOutputTokens">单次模型响应的输出 Token 上限；null 不覆盖供应商默认值。</param>
/// <param name="MaximumRunTotalTokens">单个 Agent 运行内已报告总 Token 的累计阈值；null 关闭累计预算。</param>
/// <param name="TokenBudgetWarningPercent">已知预算用量预警百分比，1–99，默认 80；null 只关闭预警。</param>
public sealed record AgentRuntimeOptions(
    TimeSpan ToolCallTimeout,
    int MaximumToolResultBytes = 1_048_576,
    int MaximumModelOutputBytes = 32_768,
    int MaximumModelOutputEvents = 4_096,
    int MaximumModelInputBytes = 262_144,
    int MaximumToolArgumentBytes = 32_768,
    int MaximumInternalToolResultBytes = 32_768,
    int MaximumInternalToolCalls = 32,
    int MaximumMcpToolCalls = 32,
    int? MaximumModelOutputTokens = null,
    long? MaximumRunTotalTokens = null,
    int? TokenBudgetWarningPercent = 80);

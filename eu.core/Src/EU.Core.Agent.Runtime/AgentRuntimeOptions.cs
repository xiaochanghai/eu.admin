namespace EU.Core.Agent.Runtime;

/// <summary>工具调用与模型输入输出预算；模型地址、凭据、超时及思考设置由模型配置解析器提供。</summary>
public sealed record AgentRuntimeOptions(
    TimeSpan ToolCallTimeout,
    int MaximumToolResultBytes = 1_048_576,
    int MaximumModelOutputBytes = 32_768,
    int MaximumModelOutputEvents = 4_096,
    int MaximumModelInputBytes = 262_144,
    int MaximumToolArgumentBytes = 32_768,
    int MaximumInternalToolResultBytes = 32_768,
    int MaximumInternalToolCalls = 32,
    int MaximumMcpToolCalls = 32);

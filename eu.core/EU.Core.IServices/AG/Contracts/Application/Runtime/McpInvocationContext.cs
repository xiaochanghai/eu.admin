#nullable enable

namespace EU.Core.IServices.Runtime;

/// <summary>
/// 封装一次 MCP 工具调用的执行身份和运行标识。
/// </summary>
public sealed record McpInvocationContext
{
    public McpInvocationContext(AgentExecutionIdentity identity, Guid agentRunId)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (agentRunId == Guid.Empty)
        {
            throw new ArgumentException("The Agent Run identifier is required.", nameof(agentRunId));
        }

        Identity = identity;
        AgentRunId = agentRunId;
    }

    /// <summary>
    /// 获取发起 MCP 调用的执行身份。
    /// </summary>
    public AgentExecutionIdentity Identity { get; }

    /// <summary>
    /// 获取关联的 Agent 运行标识。
    /// </summary>
    public Guid AgentRunId { get; }
}

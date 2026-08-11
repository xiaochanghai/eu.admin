namespace EU.Core.Agent.Application.Runtime;

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

    public AgentExecutionIdentity Identity { get; }

    public Guid AgentRunId { get; }
}

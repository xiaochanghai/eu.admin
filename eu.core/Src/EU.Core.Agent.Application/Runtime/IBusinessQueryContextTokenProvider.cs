using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.UnifiedEntry;

namespace EU.Core.Agent.Application.Runtime;

public interface IBusinessQueryContextTokenProvider
{
    ValueTask<string> CreateAsync(
        McpInvocationContext invocationContext,
        BusinessQueryToolPolicy policy,
        PublishedMcpToolReference tool,
        CancellationToken cancellationToken = default);
}

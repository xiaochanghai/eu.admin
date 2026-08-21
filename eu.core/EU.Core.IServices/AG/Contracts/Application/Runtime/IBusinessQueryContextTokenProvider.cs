#nullable enable

using EU.Core.IServices.Mcp;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Runtime;

public interface IBusinessQueryContextTokenProvider
{
    ValueTask<string> CreateAsync(
        McpInvocationContext invocationContext,
        BusinessQueryToolPolicy policy,
        PublishedMcpToolReference tool,
        CancellationToken cancellationToken = default);
}

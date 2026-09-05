#nullable enable

using EU.Core.IServices.Mcp;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Runtime;

/// <summary>
/// 定义业务查询授权上下文令牌的获取能力。
/// </summary>
public interface IBusinessQueryContextTokenProvider
{
    /// <summary>创建业务查询上下文令牌。</summary>
    ValueTask<string> CreateAsync(
        McpInvocationContext invocationContext,
        BusinessQueryToolPolicy policy,
        PublishedMcpToolReference tool,
        CancellationToken cancellationToken = default);
}

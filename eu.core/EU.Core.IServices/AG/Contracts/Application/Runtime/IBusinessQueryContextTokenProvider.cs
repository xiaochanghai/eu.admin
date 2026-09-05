#nullable enable

using EU.Core.IServices.Mcp;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Runtime;

/// <summary>
/// 定义业务查询授权上下文令牌的获取能力。
/// </summary>
public interface IBusinessQueryContextTokenProvider
{
    #region 创建业务查询上下文令牌。
    /// <summary>创建业务查询上下文令牌。</summary>
    /// <param name="invocationContext">MCP 调用所用的执行身份和运行上下文。</param>
    /// <param name="policy">受控业务查询工具策略。</param>
    /// <param name="tool">工具定义。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>绑定业务查询调用上下文、策略和工具版本的上下文令牌。</returns>
    ValueTask<string> CreateAsync(
        McpInvocationContext invocationContext,
        BusinessQueryToolPolicy policy,
        PublishedMcpToolReference tool,
        CancellationToken cancellationToken = default);
    #endregion
}

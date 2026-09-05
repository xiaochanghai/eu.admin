using EU.Core.IServices.Mcp;
using EU.Core.Api.Agent.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Model;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：McpToolVersionsController 接口处理

/// <summary>
/// 提供 MCP 工具版本查询的 HTTP 接口。
/// </summary>
/// <param name="catalog">用于查询已发布 MCP 工具版本的目录。</param>
[Route("api/mcp/tool-versions")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class McpToolVersionsController(
    IPublishedMcpToolCatalog catalog) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含已发布 MCP 工具版本引用集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<PublishedMcpToolReference>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedMcpToolReference> values =
            await catalog.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<PublishedMcpToolReference>>.QuerySuccess(values);
    }
    #endregion
}

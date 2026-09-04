using EU.Core.IServices.Mcp;
using EU.Core.Api.Agent.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Model;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：McpToolVersionsController 接口处理

[Route("api/mcp/tool-versions")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class McpToolVersionsController(
    IPublishedMcpToolCatalog catalog) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<PublishedMcpToolReference>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedMcpToolReference> values =
            await catalog.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<PublishedMcpToolReference>>.QuerySuccess(values);
    }
}

#endregion

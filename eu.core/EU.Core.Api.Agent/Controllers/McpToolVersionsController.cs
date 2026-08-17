using EU.Core.Agent.Application.Mcp;
using EU.Core.Api.Agent.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/mcp/tool-versions")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class McpToolVersionsController(
    IPublishedMcpToolCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedMcpToolReference> values =
            await catalog.ListAsync(cancellationToken);
        return new JsonResult(
            ServiceResult<IReadOnlyList<PublishedMcpToolReference>>.QuerySuccess(values))
        {
            StatusCode = StatusCodes.Status200OK
        };
    }
}

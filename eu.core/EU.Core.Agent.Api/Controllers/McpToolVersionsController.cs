using EU.Core.Agent.Application.Mcp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Agent.Api.Security;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/mcp/tool-versions")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class McpToolVersionsController(
    IPublishedMcpToolCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await catalog.ListAsync(cancellationToken));
}

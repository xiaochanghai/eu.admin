using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/skill-versions")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class SkillVersionsController(
    IPublishedSkillVersionCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await catalog.ListAsync(cancellationToken));
}

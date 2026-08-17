using EU.Core.Agent.Application.Skills;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Model;
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
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedSkillReference> values = await catalog.ListAsync(cancellationToken);
        return new JsonResult(
            ServiceResult<IReadOnlyList<PublishedSkillReference>>.QuerySuccess(values),
            AgentJsonSerialization.PascalCase)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }
}

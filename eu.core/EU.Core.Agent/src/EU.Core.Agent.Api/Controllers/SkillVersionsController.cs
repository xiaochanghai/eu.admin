using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/skill-versions")]
public sealed class SkillVersionsController(
    IPublishedSkillVersionCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await catalog.ListAsync(cancellationToken));
}

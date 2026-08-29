using EU.Core.IServices.Skills;
using EU.Core.Model;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/skill-versions")]
public sealed class SkillVersionsController(
    IPublishedSkillVersionCatalog catalog) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<PublishedSkillReference>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedSkillReference> values = await catalog.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<PublishedSkillReference>>.QuerySuccess(values);
    }
}

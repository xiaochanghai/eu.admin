using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Security;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/business-query-results")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class BusinessQueryRetentionController(
    IUnifiedEntryRepository repository,
    IOptions<BusinessQueryResultRetentionOptions> options,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost("cleanup")]
    public async Task<IActionResult> Cleanup(
        CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().AddDays(
            -options.Value.RetentionDays);
        BusinessQueryCleanupResult result =
            await repository.RedactExpiredBusinessQueryResultsAsync(
                cutoff, cancellationToken);
        return new JsonResult(
            ServiceResult<BusinessQueryCleanupResult>.OprateSuccess(result),
            AgentJsonSerialization.PascalCase)
        { StatusCode = StatusCodes.Status200OK };
    }
}

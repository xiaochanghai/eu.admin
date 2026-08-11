using EU.Core.Agent.Api.Configuration;
using EU.Core.Agent.Api.Security;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/business-query-results")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class BusinessQueryRetentionController(
    IUnifiedEntryRepository repository,
    IOptions<BusinessQueryResultRetentionOptions> options,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost("cleanup")]
    public async Task<ActionResult<BusinessQueryCleanupResult>> Cleanup(
        CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().AddDays(
            -options.Value.RetentionDays);
        return Ok(await repository.RedactExpiredBusinessQueryResultsAsync(
            cutoff, cancellationToken));
    }
}

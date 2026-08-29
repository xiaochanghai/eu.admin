using EU.Core.Api.Agent.Configuration;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/business-query-results")]
public sealed class BusinessQueryRetentionController(
    IUnifiedEntryRepository repository,
    IOptions<BusinessQueryResultRetentionOptions> options,
    TimeProvider timeProvider) : Base.ControllerBase
{
    [HttpPost("cleanup")]
    public async Task<ServiceResult<BusinessQueryCleanupResult>> Cleanup(
        CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().AddDays(
            -options.Value.RetentionDays);
        BusinessQueryCleanupResult result =
            await repository.RedactExpiredBusinessQueryResultsAsync(
                cutoff, cancellationToken);
        return ServiceResult<BusinessQueryCleanupResult>.OprateSuccess(result);
    }
}

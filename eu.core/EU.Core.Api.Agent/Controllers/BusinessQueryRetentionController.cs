using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：BusinessQueryRetentionController 接口处理

/// <summary>
/// 提供业务查询敏感载荷清理的 HTTP 接口。
/// </summary>
[Route("api/business-query-results")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class BusinessQueryRetentionController(
    IUnifiedEntryRepository repository,
    IOptions<BusinessQueryResultRetentionOptions> options,
    TimeProvider timeProvider) : Base.ControllerBase
{
    [HttpPost("cleanup")]
    public async Task<ServiceResult<BusinessQueryCleanupResult>> Cleanup(CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().AddDays(
            -options.Value.RetentionDays);
        BusinessQueryCleanupResult result =
            await repository.RedactExpiredBusinessQueryResultsAsync(
                cutoff, cancellationToken);
        return Success(result);
    }
}

#endregion

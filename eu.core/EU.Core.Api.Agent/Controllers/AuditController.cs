using EU.Core.Api.Agent.Security;
using EU.Core.Agent.Application.Abstractions.Auditing;
using EU.Core.Agent.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/audit/operations")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class AuditController(
    IAgentOperationAuditRepository repository,
    ICallerContext caller) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await repository.ListAsync(
            caller.TenantId,
            Math.Clamp(take, 1, 100),
            cancellationToken));
}

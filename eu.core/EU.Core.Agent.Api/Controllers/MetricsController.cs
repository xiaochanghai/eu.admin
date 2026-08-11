using EU.Core.Agent.Api.Configuration;
using EU.Core.Agent.Api.Observability;
using EU.Core.Agent.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("metrics")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class MetricsController(
    AgentMetrics metrics,
    IOptions<AgentDeploymentOptions> deployment) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => deployment.Value.MetricsEnabled
        ? new ContentResult
        {
            Content = metrics.RenderPrometheus(),
            ContentType = "text/plain; version=0.0.4; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        }
        : NotFound();
}

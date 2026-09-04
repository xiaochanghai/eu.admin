using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：MetricsController 接口处理

[Route("metrics")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class MetricsController(
    AgentMetrics metrics,
    IOptions<AgentDeploymentOptions> deployment) : Base.ControllerBase
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

#endregion

using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：MetricsController 接口处理

/// <summary>
/// 提供 Agent 服务运行指标的 HTTP 接口。
/// </summary>
/// <param name="metrics">用于汇总并输出 Agent 运行指标的指标收集器。</param>
/// <param name="deployment">包含指标端点启用开关的部署配置。</param>
[Route("metrics")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class MetricsController(
    AgentMetrics metrics,
    IOptions<AgentDeploymentOptions> deployment) : Base.ControllerBase
{
    #region 获取 Prometheus 指标（Get）
    /// <summary>
    /// 在部署配置启用指标端点时返回 Prometheus 文本指标（Get）。
    /// </summary>
    /// <returns>启用时返回 HTTP 200 和指标文本；未启用时返回 HTTP 404。</returns>
    [HttpGet]
    public IActionResult Get() => deployment.Value.MetricsEnabled
        ? new ContentResult
        {
            Content = metrics.RenderPrometheus(),
            ContentType = "text/plain; version=0.0.4; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        }
        : NotFound();
    #endregion
}

using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Health;
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class PlatformController(
    IOptions<AgentPlatformOptions> platform,
    IOptions<AgentEvaluationOptions> evaluation,
    IPublicModelProfileCatalog modelProfiles,
    MainAgentAssignmentService mainAgentAssignments) : ControllerBase
{
    [HttpGet("service")]
    public IActionResult Service() =>
        QuerySuccess(new PlatformServiceResponse(
            platform.Value.ServiceName,
            ReplicaModeHealthCheck.ReplicaMode));

    [HttpGet("capabilities")]
    public async Task<IActionResult> Capabilities(CancellationToken cancellationToken)
    {
        bool mainAgent = (await mainAgentAssignments.GetAsync(cancellationToken)).Succeeded;
        return QuerySuccess(new PlatformCapabilitiesResponse(
            "sqlsugar",
            false,
            new PlatformDeploymentResponse(
                AgentDefinition.ServerDeploymentTarget,
                AgentDefinition.ApiHost),
            modelProfiles.ProfileIds,
            new PlatformFeatureResponse(
                true, true, true, true, true, true,
                evaluation.Value.EnableModelJudge,
                mainAgent,
                false)));
    }

    private IActionResult QuerySuccess<T>(T value) => new JsonResult(
        ServiceResult<T>.QuerySuccess(value), AgentJsonSerialization.PascalCase)
    { StatusCode = StatusCodes.Status200OK };
}

public sealed record PlatformServiceResponse(string Service, string ReplicaMode);

public sealed record PlatformCapabilitiesResponse(
    string StorageMode,
    bool Volatile,
    PlatformDeploymentResponse Deployment,
    IReadOnlyList<string> ModelProfileIds,
    PlatformFeatureResponse Features);

public sealed record PlatformDeploymentResponse(string Target, string Host);

public sealed record PlatformFeatureResponse(
    bool AgentControl,
    bool Runtime,
    bool Skills,
    bool Mcp,
    bool Knowledge,
    bool Orchestration,
    bool ModelJudge,
    bool MainAgent,
    bool Schedules);

using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Health;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Services;

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
    public ServiceResult<PlatformServiceResponse> Service() =>
        ServiceResult<PlatformServiceResponse>.QuerySuccess(new PlatformServiceResponse(
            platform.Value.ServiceName,
            ReplicaModeHealthCheck.ReplicaMode));

    [HttpGet("capabilities")]
    public async Task<ServiceResult<PlatformCapabilitiesResponse>> Capabilities(
        CancellationToken cancellationToken)
    {
        bool mainAgent = (await mainAgentAssignments.GetAsync(cancellationToken)).Success;
        return ServiceResult<PlatformCapabilitiesResponse>.QuerySuccess(
            new PlatformCapabilitiesResponse(
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

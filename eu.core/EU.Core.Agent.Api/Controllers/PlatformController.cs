using EU.Core.Agent.Api.Configuration;
using EU.Core.Agent.Api.Health;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Agent.Api.Security;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class PlatformController(
    IOptions<AgentPlatformOptions> platform,
    IOptions<AgentStorageOptions> storage,
    IOptions<AgentEvaluationOptions> evaluation,
    IPublicModelProfileCatalog modelProfiles,
    MainAgentAssignmentService mainAgentAssignments) : ControllerBase
{
    [HttpGet("service")]
    public IActionResult Service() =>
        Ok(new
        {
            service = platform.Value.ServiceName,
            replicaMode = ReplicaModeHealthCheck.ReplicaMode
        });

    [HttpGet("capabilities")]
    public async Task<IActionResult> Capabilities(CancellationToken cancellationToken)
    {
        bool inMemory = string.Equals(
            storage.Value.Provider,
            "InMemory",
            StringComparison.OrdinalIgnoreCase);
        bool mainAgent = (await mainAgentAssignments.GetAsync(cancellationToken)).Succeeded;
        return Ok(new
        {
            storageMode = inMemory ? "memory" : "sqlite",
            @volatile = inMemory,
            deployment = new
            {
                target = AgentDefinition.ServerDeploymentTarget,
                host = AgentDefinition.ApiHost
            },
            modelProfileIds = modelProfiles.ProfileIds,
            features = new
            {
                agentControl = true,
                runtime = true,
                skills = true,
                mcp = true,
                knowledge = true,
                orchestration = true,
                modelJudge = evaluation.Value.EnableModelJudge,
                mainAgent,
                schedules = false
            }
        });
    }
}

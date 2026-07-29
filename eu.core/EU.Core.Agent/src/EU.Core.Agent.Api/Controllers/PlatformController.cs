using EU.Core.Agent.Api.Configuration;
using EU.Core.Agent.Api.Health;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController(
    IOptions<AgentPlatformOptions> platform,
    IOptions<AgentStorageOptions> storage,
    IPublicModelProfileCatalog modelProfiles) : ControllerBase
{
    [HttpGet("service")]
    public IActionResult Service() =>
        Ok(new
        {
            service = platform.Value.ServiceName,
            replicaMode = ReplicaModeHealthCheck.ReplicaMode
        });

    [HttpGet("capabilities")]
    public IActionResult Capabilities()
    {
        bool inMemory = string.Equals(
            storage.Value.Provider,
            "InMemory",
            StringComparison.OrdinalIgnoreCase);
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
                schedules = false
            }
        });
    }
}

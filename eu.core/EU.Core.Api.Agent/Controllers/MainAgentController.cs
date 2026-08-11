using EU.Core.Agent.Application.MainAgent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/platform/main-agent")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class MainAgentController(MainAgentAssignmentService assignments) : ControllerBase
{
    private readonly MainAgentAssignmentService _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        MainAgentOperationResult result = await _assignments.GetAsync(cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPut]
    public async Task<IActionResult> Set(
        [FromBody] SetMainAgentRequest request,
        CancellationToken cancellationToken)
    {
        MainAgentOperationResult result = await _assignments.SetAsync(
            new SetMainAgentCommand(request.AgentId, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    private IActionResult FromError(MainAgentError error)
    {
        int status = error.Code switch
        {
            MainAgentErrorCodes.NotConfigured or MainAgentErrorCodes.AgentNotFound => StatusCodes.Status404NotFound,
            MainAgentErrorCodes.RowVersionConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return ApiProblemResults.Create(
            HttpContext,
            status,
            error.Code,
            "The Main Agent assignment could not be completed.",
            error.Message);
    }
}

public sealed record SetMainAgentRequest(Guid AgentId, long? ExpectedLogicalRevision);

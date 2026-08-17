using EU.Core.Agent.Application.MainAgent;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
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
        return result.Succeeded
            ? new JsonResult(
                ServiceResult<MainAgentAssignment>.QuerySuccess(result.Value!),
                AgentJsonSerialization.PascalCase)
            {
                StatusCode = StatusCodes.Status200OK
            }
            : FromError(result.Error!);
    }

    [HttpPut]
    public async Task<IActionResult> Set(
        [FromBody] SetMainAgentRequest request,
        CancellationToken cancellationToken)
    {
        MainAgentOperationResult result = await _assignments.SetAsync(
            new SetMainAgentCommand(request.AgentId, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Succeeded
            ? new JsonResult(
                ServiceResult<MainAgentAssignment>.OprateSuccess(result.Value!),
                AgentJsonSerialization.PascalCase)
            {
                StatusCode = StatusCodes.Status200OK
            }
            : FromError(result.Error!);
    }

    private IActionResult FromError(MainAgentError error)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorCatalog.Resolve(error.Code);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                error.Message,
                new AgentApiErrorData(error.Code, HttpContext.TraceIdentifier)),
            AgentJsonSerialization.PascalCase)
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record SetMainAgentRequest(Guid AgentId, long? ExpectedLogicalRevision);

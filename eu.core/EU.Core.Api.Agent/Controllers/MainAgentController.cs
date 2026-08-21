using EU.Core.IServices.MainAgent;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.Services;

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
        ServiceResult<MainAgentAssignment> result = await _assignments.GetAsync(cancellationToken);
        return result.Success
            ? new JsonResult(
                ServiceResult<MainAgentAssignment>.QuerySuccess(result.Data!))
            {
                StatusCode = StatusCodes.Status200OK
            }
            : FromServiceError(result);
    }

    [HttpPut]
    public async Task<IActionResult> Set(
        [FromBody] SetMainAgentRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<MainAgentAssignment> result = await _assignments.SetAsync(
            new SetMainAgentCommand(request.AgentId, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Success
            ? new JsonResult(
                ServiceResult<MainAgentAssignment>.OprateSuccess(result.Data!))
            {
                StatusCode = StatusCodes.Status200OK
            }
            : FromServiceError(result);
    }

    private IActionResult FromServiceError(ServiceResult<MainAgentAssignment> result)
    {
        string errorCode = MainAgentServiceStatusCodes.ToErrorCode(result.Status);
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                result.Message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record SetMainAgentRequest(Guid AgentId, long? ExpectedLogicalRevision);

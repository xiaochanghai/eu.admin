using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Security;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/tool-approvals")]
public sealed class ToolApprovalsController(
    ToolApprovalManagementService approvals,
    ICallerContext caller,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalRead)]
    public async Task<IActionResult> List(
        [FromQuery] ToolApprovalStatus? status = null,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > ToolApprovalStateMachine.MaximumTake)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                ToolApprovalErrorCodes.Invalid,
                "The approval page size is invalid.");
        }

        return Ok(await approvals.ListAsync(
            caller.TenantId,
            status,
            take,
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalRead)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        ToolApprovalRequestRecord? approval = await approvals.GetAsync(
            id,
            caller.TenantId,
            cancellationToken);
        if (approval is null)
        {
            return NotFoundProblem();
        }

        return Ok(new
        {
            approval,
            decisions = await approvals.ListDecisionsAsync(
                id,
                caller.TenantId,
                cancellationToken)
        });
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalDecide)]
    public Task<IActionResult> Approve(
        Guid id,
        [FromBody] ToolApprovalDecisionApiRequest request,
        CancellationToken cancellationToken) =>
        Decide(id, request, ToolApprovalDecisionAction.Approve, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalDecide)]
    public Task<IActionResult> Reject(
        Guid id,
        [FromBody] ToolApprovalDecisionApiRequest request,
        CancellationToken cancellationToken) =>
        Decide(id, request, ToolApprovalDecisionAction.Reject, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public Task<IActionResult> Cancel(
        Guid id,
        [FromBody] ToolApprovalDecisionApiRequest request,
        CancellationToken cancellationToken) =>
        Decide(id, request, ToolApprovalDecisionAction.Cancel, cancellationToken);

    [HttpPost("{id:guid}/resume")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<IActionResult> Resume(
        Guid id,
        CancellationToken cancellationToken)
    {
        ToolApprovalConversationResumeService? resumeService =
            HttpContext.RequestServices.GetService<
                ToolApprovalConversationResumeService>();
        if (resumeService is null)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "TOOL_APPROVAL_DISABLED",
                "Tool approval execution is not enabled.");
        }

        try
        {
            return Ok(await resumeService.ResumeAsync(
                id,
                new AgentExecutionIdentity(
                    caller.UserId,
                    caller.TenantId,
                    caller.Permissions,
                    caller.CorrelationId),
                cancellationToken));
        }
        catch (ToolApprovalException exception)
        {
            int status = exception.ErrorCode switch
            {
                ToolApprovalErrorCodes.InvalidState =>
                    StatusCodes.Status409Conflict,
                ToolApprovalErrorCodes.Expired =>
                    StatusCodes.Status409Conflict,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return Problem(
                status,
                exception.ErrorCode,
                "The approved tool call could not be resumed.");
        }
    }

    private async Task<IActionResult> Decide(
        Guid id,
        ToolApprovalDecisionApiRequest request,
        ToolApprovalDecisionAction action,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                ToolApprovalErrorCodes.Invalid,
                "The approval decision contains an unsupported property.");
        }

        ToolApprovalRequestRecord? current = await approvals.GetAsync(
            id,
            caller.TenantId,
            cancellationToken);
        if (current is null)
        {
            return NotFoundProblem();
        }

        if (action == ToolApprovalDecisionAction.Approve
            && current.Risk == McpToolRisk.HighRisk
            && !HasPermission(
                AgentAuthorizationPolicies.ApprovalDecideHighRiskPermission))
        {
            return Problem(
                StatusCodes.Status403Forbidden,
                "AUTHORIZATION_DENIED",
                "High-risk approval permission is required.");
        }

        try
        {
            ToolApprovalRequestRecord decided = await approvals.DecideAsync(
                new ToolApprovalDecisionCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    action,
                    request.Reason ?? string.Empty,
                    timeProvider.GetUtcNow()),
                cancellationToken);
            return Ok(decided);
        }
        catch (ToolApprovalException exception)
        {
            int status = exception.ErrorCode switch
            {
                ToolApprovalErrorCodes.SelfApprovalForbidden
                    or ToolApprovalErrorCodes.CancellationForbidden =>
                    StatusCodes.Status403Forbidden,
                ToolApprovalErrorCodes.Invalid =>
                    StatusCodes.Status400BadRequest,
                ToolApprovalErrorCodes.Expired
                    or ToolApprovalErrorCodes.InvalidState =>
                    StatusCodes.Status409Conflict,
                _ => StatusCodes.Status409Conflict
            };
            return Problem(
                status,
                exception.ErrorCode,
                "The approval decision could not be completed.");
        }
    }

    private bool HasPermission(string permission) =>
        caller.Permissions.Contains(permission, StringComparer.Ordinal)
        || caller.Permissions.Contains(
            AgentAuthorizationPolicies.AdminPermission,
            StringComparer.Ordinal);

    private IActionResult NotFoundProblem() =>
        Problem(
            StatusCodes.Status404NotFound,
            "TOOL_APPROVAL_NOT_FOUND",
            "The tool approval was not found.");

    private IActionResult Problem(int status, string errorCode, string title) =>
        ApiProblemResults.Create(HttpContext, status, errorCode, title);
}

public sealed class ToolApprovalDecisionApiRequest
{
    public string? Reason { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

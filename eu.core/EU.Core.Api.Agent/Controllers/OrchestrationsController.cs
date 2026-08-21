using EU.Core.IServices.Orchestration;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Services;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/orchestrations")]
public sealed class OrchestrationsController(
    OrchestrationLifecycleService lifecycle,
    OrchestrationRuntimeService runtime) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        OrchestrationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(OrchestrationStatus.Enabled), StringComparison.Ordinal))
                parsedStatus = OrchestrationStatus.Enabled;
            else if (string.Equals(status, nameof(OrchestrationStatus.Disabled), StringComparison.Ordinal))
                parsedStatus = OrchestrationStatus.Disabled;
            else if (string.Equals(status, nameof(OrchestrationStatus.Archived), StringComparison.Ordinal))
                parsedStatus = OrchestrationStatus.Archived;
            else
            {
                return FromError(
                    OrchestrationErrorCodes.LifecycleTransitionInvalid,
                    "Orchestration status must be Enabled, Disabled, or Archived.");
            }
        }
        return QuerySuccess(await lifecycle.ListAsync(parsedStatus, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        OrchestrationDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        return value is null
            ? FromError(OrchestrationErrorCodes.NotFound, "The orchestration was not found.")
            : QuerySuccess(value);
    }

    [HttpPost]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.CreateAsync(
            new CreateOrchestrationCommand(request.Code, request.Name, request.Description), cancellationToken);
        if (!result.Succeeded)
            return FromError(result.Error!);

        Response.Headers.Location = $"/api/orchestrations/{result.Value!.Id}";
        return OperationSuccess(result.Value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}/draft")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<IActionResult> SaveDraft(
        Guid id,
        [FromBody] SaveOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.SaveDraftAsync(
            new SaveOrchestrationDraftCommand(
                id, request.ExpectedLogicalRevision, request.Name, request.Description,
                request.Status, request.StartNodeId, request.Nodes, request.Edges), cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromBody] PublishOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.PublishAsync(
            new PublishOrchestrationCommand(id, request.ExpectedLogicalRevision), cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/archive")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetOrchestrationArchiveRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.SetArchivedAsync(
            new SetOrchestrationArchiveCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Archived),
            cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/runs")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<IActionResult> Start(
        Guid id,
        [FromBody] StartOrchestrationRunRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationRunRecord> result =
            await runtime.StartAsync(id, request.Input, cancellationToken);
        if (!result.Succeeded)
            return FromError(result.Error!);

        Response.Headers.Location = $"/api/orchestrations/{id}/runs/{result.Value!.Id}";
        return OperationSuccess(result.Value, StatusCodes.Status202Accepted);
    }

    [HttpGet("{id:guid}/runs")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<IActionResult> Runs(
        Guid id, [FromQuery] int take = 20, CancellationToken cancellationToken = default) =>
        QuerySuccess(await runtime.ListAsync(
            id,
            Math.Clamp(take, 1, 100),
            cancellationToken));

    [HttpGet("{id:guid}/runs/{runId:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<IActionResult> Run(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        return value is null || value.OrchestrationId != id
            ? FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.")
            : QuerySuccess(value);
    }

    [HttpPost("{id:guid}/runs/{runId:guid}/cancel")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<IActionResult> Cancel(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        await runtime.CancelAsync(runId, cancellationToken);
        return OperationSuccess(
            new OrchestrationRunCancelResponse(runId),
            StatusCodes.Status202Accepted);
    }

    [HttpGet("{id:guid}/runs/{runId:guid}/details")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<IActionResult> Details(
        Guid id,
        Guid runId,
        CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        OrchestrationRunDetails? details = await runtime.GetDetailsAsync(runId, cancellationToken);
        return details is null
            ? FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run details were not found.")
            : QuerySuccess(details);
    }

    [HttpGet("{id:guid}/runs/{runId:guid}/output")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<IActionResult> Output(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        if (value.Status != OrchestrationRunStatus.Completed)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run has not completed.");
        OrchestrationRunDetails? details = await runtime.GetDetailsAsync(runId, cancellationToken);
        return details is null
            ? NoContent()
            : QuerySuccess(new OrchestrationRunOutputResponse(details.Output, false));
    }

    private IActionResult FromError(OrchestrationError error) =>
        FromError(error.Code, error.Message);

    private IActionResult QuerySuccess<T>(T value) =>
        new JsonResult(
            ServiceResult<T>.QuerySuccess(value))
        {
            StatusCode = StatusCodes.Status200OK
        };

    private IActionResult OperationSuccess<T>(
        T value,
        int httpStatus = StatusCodes.Status200OK) =>
        new JsonResult(
            ServiceResult<T>.OprateSuccess(value))
        {
            StatusCode = httpStatus
        };

    private IActionResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record CreateOrchestrationRequest(string Code, string Name, string Description);
public sealed record SaveOrchestrationRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    OrchestrationStatus Status,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges);
public sealed record PublishOrchestrationRequest(long ExpectedLogicalRevision);
public sealed record SetOrchestrationArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived);
public sealed record StartOrchestrationRunRequest(string Input);
public sealed record OrchestrationRunCancelResponse(Guid RunId);
public sealed record OrchestrationRunOutputResponse(string Output, bool Ephemeral);

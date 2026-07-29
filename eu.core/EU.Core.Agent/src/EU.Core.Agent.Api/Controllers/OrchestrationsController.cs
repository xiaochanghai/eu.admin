using EU.Core.Agent.Application.Orchestration;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/orchestrations")]
public sealed class OrchestrationsController(
    OrchestrationLifecycleService lifecycle,
    OrchestrationRuntimeService runtime) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await lifecycle.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        OrchestrationDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        return value is null ? Error(404, OrchestrationErrorCodes.NotFound, "The orchestration was not found.") : Ok(value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.CreateAsync(
            new CreateOrchestrationCommand(request.Code, request.Name, request.Description), cancellationToken);
        return result.Succeeded
            ? Created($"/api/orchestrations/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<IActionResult> SaveDraft(
        Guid id,
        [FromBody] SaveOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.SaveDraftAsync(
            new SaveOrchestrationDraftCommand(
                id, request.ExpectedLogicalRevision, request.Name, request.Description,
                request.Status, request.StartNodeId, request.Nodes, request.Edges), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromBody] PublishOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationDefinition> result = await lifecycle.PublishAsync(
            new PublishOrchestrationCommand(id, request.ExpectedLogicalRevision), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/runs")]
    public async Task<IActionResult> Start(
        Guid id,
        [FromBody] StartOrchestrationRunRequest request,
        CancellationToken cancellationToken)
    {
        OrchestrationOperationResult<OrchestrationRunRecord> result =
            await runtime.StartAsync(id, request.Input, cancellationToken);
        return result.Succeeded
            ? Accepted($"/api/orchestrations/{id}/runs/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
    }

    [HttpGet("{id:guid}/runs")]
    public async Task<IActionResult> Runs(
        Guid id, [FromQuery] int take = 20, CancellationToken cancellationToken = default) =>
        Ok(await runtime.ListAsync(id, Math.Clamp(take, 1, 100), cancellationToken));

    [HttpGet("{id:guid}/runs/{runId:guid}")]
    public async Task<IActionResult> Run(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        return value is null || value.OrchestrationId != id
            ? Error(404, OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.")
            : Ok(value);
    }

    [HttpPost("{id:guid}/runs/{runId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return Error(404, OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        await runtime.CancelAsync(runId, cancellationToken);
        return Accepted(new { runId });
    }

    [HttpGet("{id:guid}/runs/{runId:guid}/details")]
    public async Task<IActionResult> Details(
        Guid id,
        Guid runId,
        CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return Error(404, OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        OrchestrationRunDetails? details = await runtime.GetDetailsAsync(runId, cancellationToken);
        return details is null
            ? Error(404, OrchestrationErrorCodes.RunNotFound, "The orchestration run details were not found.")
            : Ok(details);
    }

    [HttpGet("{id:guid}/runs/{runId:guid}/output")]
    public async Task<IActionResult> Output(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return Error(404, OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        if (value.Status != OrchestrationRunStatus.Completed)
            return Error(409, OrchestrationErrorCodes.RunNotFound, "The orchestration run has not completed.");
        OrchestrationRunDetails? details = await runtime.GetDetailsAsync(runId, cancellationToken);
        return details is null ? NoContent() : Ok(new { output = details.Output, ephemeral = false });
    }

    private IActionResult FromError(OrchestrationError error) => Error(
        error.Code switch
        {
            OrchestrationErrorCodes.NotFound or OrchestrationErrorCodes.RunNotFound => 404,
            OrchestrationErrorCodes.CodeConflict or OrchestrationErrorCodes.RowVersionConflict => 409,
            _ => 400
        }, error.Code, error.Message);

    private IActionResult Error(int status, string code, string detail) =>
        ApiProblemResults.Create(
            HttpContext, status, code, "The orchestration operation could not be completed.", detail);
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
public sealed record StartOrchestrationRunRequest(string Input);

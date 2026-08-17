using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Security;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/evaluations/runs")]
[Authorize(Policy = AgentAuthorizationPolicies.Debug)]
public sealed class RunEvaluationsController(
    RunEvaluationService service,
    ICallerContext caller) : ControllerBase
{
    [HttpPost("{runId}")]
    public async Task<IActionResult> Evaluate(
        string runId,
        [FromBody] EvaluateRunRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(runId, out Guid id) || id == Guid.Empty)
        {
            return FromError(RunEvaluationErrorCodes.SpecificationInvalid, "The run evaluation request is invalid.");
        }

        if (request.AdditionalProperties is { Count: > 0 }
            || !TryStatus(request.ExpectedStatus, out UnifiedRunStatus? expectedStatus))
        {
            return FromError(RunEvaluationErrorCodes.SpecificationInvalid, "The run evaluation request is invalid.");
        }

        try
        {
            RunEvaluationReport? report = await service.EvaluateAsync(
                id,
                caller.TenantId,
                caller.UserId,
                new RunEvaluationSpecification(
                    expectedStatus,
                    request.OutputContains ?? [],
                    request.OutputExcludes ?? [],
                    request.RequiredEventKinds ?? [],
                    request.MaximumToolCalls,
                    request.MaximumDurationMilliseconds),
                cancellationToken);
            return report is null
                ? FromError(RunEvaluationErrorCodes.RunNotFound, "The run was not found.")
                : OperationSuccess(report);
        }
        catch (RunEvaluationException exception)
        {
            return FromError(exception.ErrorCode, exception.Message);
        }
    }

    private static bool TryStatus(string? value, out UnifiedRunStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse(value, true, out UnifiedRunStatus parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }

        status = parsed;
        return true;
    }

    private IActionResult OperationSuccess<T>(T value) => new JsonResult(
        ServiceResult<T>.OprateSuccess(value), AgentJsonSerialization.PascalCase)
    { StatusCode = StatusCodes.Status200OK };

    private IActionResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorCatalog.Resolve(errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)),
            AgentJsonSerialization.PascalCase)
        { StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError };
    }
}

public sealed record EvaluateRunRequest(
    string? ExpectedStatus,
    IReadOnlyList<string>? OutputContains,
    IReadOnlyList<string>? OutputExcludes,
    IReadOnlyList<string>? RequiredEventKinds,
    int? MaximumToolCalls,
    long? MaximumDurationMilliseconds)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

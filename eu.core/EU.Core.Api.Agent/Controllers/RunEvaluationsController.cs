using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Security;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                RunEvaluationErrorCodes.SpecificationInvalid,
                "The run evaluation request is invalid.");
        }

        if (request.AdditionalProperties is { Count: > 0 }
            || !TryStatus(request.ExpectedStatus, out UnifiedRunStatus? expectedStatus))
        {
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                RunEvaluationErrorCodes.SpecificationInvalid,
                "The run evaluation request is invalid.");
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
                ? ApiProblemResults.Create(
                    HttpContext,
                    StatusCodes.Status404NotFound,
                    RunEvaluationErrorCodes.RunNotFound,
                    "The run was not found.")
                : Ok(report);
        }
        catch (RunEvaluationException exception)
        {
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                exception.ErrorCode,
                exception.Message);
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

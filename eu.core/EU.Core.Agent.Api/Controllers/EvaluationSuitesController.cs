using System.Text.Json.Serialization;
using EU.Core.Agent.Api.Security;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/evaluation-suites")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class EvaluationSuitesController(
    EvaluationSuiteLifecycleService lifecycle,
    ICallerContext caller) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        EvaluationSuiteStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(EvaluationSuiteStatus.Active), StringComparison.Ordinal))
                parsedStatus = EvaluationSuiteStatus.Active;
            else if (string.Equals(status, nameof(EvaluationSuiteStatus.Archived), StringComparison.Ordinal))
                parsedStatus = EvaluationSuiteStatus.Archived;
            else
                return Error(400, EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                    "Evaluation suite status must be Active or Archived.");
        }

        return Ok(await lifecycle.ListAsync(caller.TenantId, parsedStatus, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        EvaluationSuiteDefinition? value = await lifecycle.GetAsync(
            id, caller.TenantId, cancellationToken);
        return value is null ? Error(404, EvaluationSuiteErrorCodes.NotFound) : Ok(value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEvaluationSuiteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return Error(400, EvaluationSuiteErrorCodes.DefinitionInvalid);
        }

        EvaluationSuiteOperationResult<EvaluationSuiteDefinition> result =
            await lifecycle.CreateAsync(
                new CreateEvaluationSuiteCommand(
                    caller.TenantId,
                    caller.UserId,
                    request.Code,
                    request.Name,
                    request.Description),
                cancellationToken);
        return result.Succeeded
            ? Created($"/api/evaluation-suites/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<IActionResult> SaveDraft(
        Guid id,
        [FromBody] SaveEvaluationSuiteDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryMapCases(request, out IReadOnlyList<EvaluationCaseDefinition> cases))
        {
            return Error(400, EvaluationSuiteErrorCodes.DefinitionInvalid);
        }

        EvaluationSuiteOperationResult<EvaluationSuiteDefinition> result =
            await lifecycle.SaveDraftAsync(
                new SaveEvaluationSuiteDraftCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision,
                    request.Name,
                    request.Description,
                    cases),
                cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromBody] PublishEvaluationSuiteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return Error(400, EvaluationSuiteErrorCodes.DefinitionInvalid);
        }

        EvaluationSuiteOperationResult<EvaluationSuiteDefinition> result =
            await lifecycle.PublishAsync(
                new PublishEvaluationSuiteCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision),
                cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetEvaluationSuiteArchiveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
            return Error(400, EvaluationSuiteErrorCodes.DefinitionInvalid);

        EvaluationSuiteOperationResult<EvaluationSuiteDefinition> result =
            await lifecycle.SetArchivedAsync(
                new SetEvaluationSuiteArchiveCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision,
                    request.Archived),
                cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    private static bool TryMapCases(
        SaveEvaluationSuiteDraftRequest request,
        out IReadOnlyList<EvaluationCaseDefinition> cases)
    {
        cases = [];
        if (request.AdditionalProperties is { Count: > 0 } || request.Cases is null)
        {
            return false;
        }

        var mapped = new List<EvaluationCaseDefinition>(request.Cases.Count);
        foreach (EvaluationCaseApiRequest value in request.Cases)
        {
            if (value.AdditionalProperties is { Count: > 0 }
                || value.Specification is null
                || value.Specification.AdditionalProperties is { Count: > 0 }
                || !TryStatus(value.Specification.ExpectedStatus, out UnifiedRunStatus? status))
            {
                return false;
            }

            mapped.Add(new EvaluationCaseDefinition(
                value.Id,
                value.Name,
                value.Input,
                value.TargetAgentId,
                value.TargetAgentVersionId,
                new RunEvaluationSpecification(
                    status,
                    value.Specification.OutputContains ?? [],
                    value.Specification.OutputExcludes ?? [],
                    value.Specification.RequiredEventKinds ?? [],
                    value.Specification.MaximumToolCalls,
                    value.Specification.MaximumDurationMilliseconds)));
        }

        cases = mapped;
        return true;
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

    private IActionResult FromError(EvaluationSuiteError error) =>
        Error(error.Code switch
        {
            EvaluationSuiteErrorCodes.NotFound => 404,
            EvaluationSuiteErrorCodes.CodeConflict
                or EvaluationSuiteErrorCodes.RowVersionConflict => 409,
            _ => 400
        }, error.Code, error.Message);

    private IActionResult Error(int status, string code, string? detail = null) =>
        ApiProblemResults.Create(
            HttpContext,
            status,
            code,
            "The evaluation suite operation could not be completed.",
            detail);
}

public sealed record CreateEvaluationSuiteRequest(
    string Code,
    string Name,
    string Description)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed record SaveEvaluationSuiteDraftRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    IReadOnlyList<EvaluationCaseApiRequest>? Cases)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed record EvaluationCaseApiRequest(
    Guid Id,
    string Name,
    string Input,
    Guid TargetAgentId,
    Guid TargetAgentVersionId,
    EvaluateRunRequest? Specification)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed record PublishEvaluationSuiteRequest(long ExpectedLogicalRevision)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed record SetEvaluationSuiteArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

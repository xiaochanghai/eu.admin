using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Services;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/evaluation-suites")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class EvaluationSuitesController(
    EvaluationSuiteLifecycleService lifecycle,
    ICallerContext caller) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<EvaluationSuiteDefinition>>> List(
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
                return FromError<IReadOnlyList<EvaluationSuiteDefinition>>(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                    "Evaluation suite status must be Active or Archived.");
        }

        return ServiceResult<IReadOnlyList<EvaluationSuiteDefinition>>.QuerySuccess(
            await lifecycle.ListAsync(caller.TenantId, parsedStatus, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        EvaluationSuiteDefinition? value = await lifecycle.GetAsync(
            id, caller.TenantId, cancellationToken);
        return value is null
            ? FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.")
            : ServiceResult<EvaluationSuiteDefinition>.QuerySuccess(value);
    }

    [HttpPost]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Create(
        [FromBody] CreateEvaluationSuiteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");
        }

        ServiceResult<EvaluationSuiteDefinition> result =
            await lifecycle.CreateAsync(
                new CreateEvaluationSuiteCommand(
                    caller.TenantId,
                    caller.UserId,
                    request.Code,
                    request.Name,
                    request.Description),
                cancellationToken);
        if (!result.Success) return FromServiceError(result);
        Response.Headers.Location = $"/api/evaluation-suites/{result.Data!.Id}";
        Response.StatusCode = StatusCodes.Status201Created;
        return Success(result.Data);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraft(
        Guid id,
        [FromBody] SaveEvaluationSuiteDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryMapCases(request, out IReadOnlyList<EvaluationCaseDefinition> cases))
        {
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");
        }

        ServiceResult<EvaluationSuiteDefinition> result =
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
        return result.Success ? result : FromServiceError(result);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Publish(
        Guid id,
        [FromBody] PublishEvaluationSuiteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");
        }

        ServiceResult<EvaluationSuiteDefinition> result =
            await lifecycle.PublishAsync(
                new PublishEvaluationSuiteCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision),
                cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> SetArchived(
        Guid id,
        [FromBody] SetEvaluationSuiteArchiveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");

        ServiceResult<EvaluationSuiteDefinition> result =
            await lifecycle.SetArchivedAsync(
                new SetEvaluationSuiteArchiveCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision,
                    request.Archived),
                cancellationToken);
        return result.Success ? result : FromServiceError(result);
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

    private ServiceResult<T> FromServiceError<T>(ServiceResult<T> result)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(
            HttpContext,
            EvaluationSuiteServiceStatusCodes.ToErrorCode(result.Status));
        Response.StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError;
        return result;
    }

    private ServiceResult<T> FromError<T>(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        Response.StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError;
        return ServiceResult<T>.Failure(descriptor.Status, message);
    }
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

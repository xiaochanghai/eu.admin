using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Security;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using EU.Core.IServices;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/evaluation-batches")]
[Authorize(Policy = AgentAuthorizationPolicies.Debug)]
public sealed class EvaluationBatchesController(
    IAgEvaluationBatchExecutionServices service,
    EvaluationBatchComparisonService comparisons,
    ModelJudgeService modelJudge,
    ICallerContext caller) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Run(
        [FromBody] StartEvaluationBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 }
            || request.SuiteId == Guid.Empty
            || request.SuiteVersionId == Guid.Empty)
        {
            return FromError(EvaluationBatchErrorCodes.RequestInvalid, "The evaluation batch request is invalid.");
        }

        EvaluationBatchOperationResult result = await service.RunAsync(
            request.SuiteId,
            request.SuiteVersionId,
            new AgentExecutionIdentity(
                caller.UserId,
                caller.TenantId,
                caller.Permissions,
                caller.CorrelationId),
            cancellationToken);
        return result.Succeeded ? OperationSuccess(result.Value!) : FromError(result.Error!);
    }

    [HttpPost("compare")]
    public async Task<IActionResult> Compare(
        [FromBody] CompareEvaluationBatchesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 }
            || request.Gate is null
            || request.Gate.AdditionalProperties is { Count: > 0 })
        {
            return FromError(EvaluationComparisonErrorCodes.SpecificationInvalid, "The evaluation comparison specification is invalid.");
        }

        EvaluationComparisonOperationResult result = await comparisons.CompareAsync(
            request.BaselineBatchId,
            request.CandidateBatchId,
            caller.TenantId,
            new EvaluationQualityGateSpecification(
                request.Gate.MinimumCandidatePassRate,
                request.Gate.MaximumPassRateRegression,
                request.Gate.MaximumAverageDurationRegressionPercent,
                request.Gate.MaximumToolCallIncreasePerCase,
                request.Gate.RequireNoNewFailures,
                request.Gate.RequireSameCaseSet,
                request.Gate.RequireStableRoutes),
            cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!.Code, result.Error.Message);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid suiteId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (suiteId == Guid.Empty || take is < 1 or > 100)
        {
            return FromError(EvaluationBatchErrorCodes.RequestInvalid, "The evaluation batch request is invalid.");
        }

        return QuerySuccess(await service.ListAsync(
            suiteId, caller.TenantId, take, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        EvaluationBatchRecord? value = await service.GetAsync(
            id, caller.TenantId, cancellationToken);
        return value is null
            ? FromError(EvaluationBatchErrorCodes.BatchNotFound, "The evaluation batch was not found.")
            : QuerySuccess(value);
    }

    [HttpPost("{id:guid}/model-judge")]
    public async Task<IActionResult> RunModelJudge(
        Guid id,
        [FromBody] RunModelJudgeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 }
            || request.Evaluators is null
            || request.MinimumScores is null)
        {
            return FromError(ModelJudgeErrorCodes.RequestInvalid, "The model judge request is invalid.");
        }

        ModelJudgeOperationResult result = await modelJudge.EvaluateAsync(
            id,
            caller.TenantId,
            caller.UserId,
            new ModelJudgeSpecification(
                request.ExplicitlyEnabled,
                request.ModelProfileId,
                request.Evaluators,
                request.MinimumScores),
            cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!.Code, result.Error.Message);
    }

    [HttpGet("{id:guid}/model-judge-reports")]
    public async Task<IActionResult> ListModelJudgeReports(
        Guid id,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || take is < 1 or > 50)
        {
            return FromError(ModelJudgeErrorCodes.RequestInvalid, "The model judge request is invalid.");
        }

        return QuerySuccess(await modelJudge.ListAsync(id, caller.TenantId, take, cancellationToken));
    }

    [HttpGet("{id:guid}/model-judge-reports/{reportId:guid}")]
    public async Task<IActionResult> GetModelJudgeReport(
        Guid id,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        ModelJudgeReport? value = await modelJudge.GetAsync(
            reportId, caller.TenantId, cancellationToken);
        return value is null || value.BatchId != id
            ? FromError(ModelJudgeErrorCodes.BatchNotFound, "The model judge report was not found.")
            : QuerySuccess(value);
    }

    private IActionResult FromError(EvaluationBatchError error) => FromError(error.Code, error.Message);

    private IActionResult QuerySuccess<T>(T value) => new JsonResult(
        ServiceResult<T>.QuerySuccess(value))
    { StatusCode = StatusCodes.Status200OK };

    private IActionResult OperationSuccess<T>(T value) => new JsonResult(
        ServiceResult<T>.OprateSuccess(value))
    { StatusCode = StatusCodes.Status200OK };

    private IActionResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        { StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError };
    }
}

public sealed record StartEvaluationBatchRequest(Guid SuiteId, Guid SuiteVersionId)
{
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed class CompareEvaluationBatchesRequest
{
    public Guid BaselineBatchId { get; init; }

    public Guid CandidateBatchId { get; init; }

    public EvaluationQualityGateApiRequest? Gate { get; init; } = new();

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed class EvaluationQualityGateApiRequest
{
    public decimal MinimumCandidatePassRate { get; init; } = 1m;

    public decimal MaximumPassRateRegression { get; init; }

    public decimal? MaximumAverageDurationRegressionPercent { get; init; }

    public int? MaximumToolCallIncreasePerCase { get; init; }

    public bool RequireNoNewFailures { get; init; } = true;

    public bool RequireSameCaseSet { get; init; } = true;

    public bool RequireStableRoutes { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

public sealed class RunModelJudgeRequest
{
    public bool ExplicitlyEnabled { get; init; }

    public string ModelProfileId { get; init; } = string.Empty;

    public IReadOnlyList<string>? Evaluators { get; init; }

    public IReadOnlyDictionary<string, decimal>? MinimumScores { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

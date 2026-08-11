using System.Text.Json.Serialization;
using EU.Core.Agent.Api.Security;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/evaluation-batches")]
[Authorize(Policy = AgentAuthorizationPolicies.Debug)]
public sealed class EvaluationBatchesController(
    EvaluationBatchService service,
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
            return Error(400, EvaluationBatchErrorCodes.RequestInvalid);
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
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
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
            return Error(400, EvaluationComparisonErrorCodes.SpecificationInvalid);
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
            ? Ok(result.Value)
            : Error(result.Error!.Code switch
            {
                EvaluationComparisonErrorCodes.BatchNotFound => 404,
                EvaluationComparisonErrorCodes.BatchNotTerminal
                    or EvaluationComparisonErrorCodes.SuiteMismatch => 409,
                _ => 400
            }, result.Error.Code, result.Error.Message);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid suiteId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (suiteId == Guid.Empty || take is < 1 or > 100)
        {
            return Error(400, EvaluationBatchErrorCodes.RequestInvalid);
        }

        return Ok(await service.ListAsync(
            suiteId, caller.TenantId, take, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        EvaluationBatchRecord? value = await service.GetAsync(
            id, caller.TenantId, cancellationToken);
        return value is null
            ? Error(404, EvaluationBatchErrorCodes.BatchNotFound)
            : Ok(value);
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
            return Error(400, ModelJudgeErrorCodes.RequestInvalid);
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
            ? Ok(result.Value)
            : Error(result.Error!.Code switch
            {
                ModelJudgeErrorCodes.BatchNotFound => 404,
                ModelJudgeErrorCodes.Disabled
                    or ModelJudgeErrorCodes.BatchNotCompleted => 409,
                ModelJudgeErrorCodes.ExecutionFailed => 502,
                ModelJudgeErrorCodes.PersistenceConflict => 409,
                _ => 400
            }, result.Error.Code, result.Error.Message);
    }

    [HttpGet("{id:guid}/model-judge-reports")]
    public async Task<IActionResult> ListModelJudgeReports(
        Guid id,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || take is < 1 or > 50)
        {
            return Error(400, ModelJudgeErrorCodes.RequestInvalid);
        }

        return Ok(await modelJudge.ListAsync(id, caller.TenantId, take, cancellationToken));
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
            ? Error(404, ModelJudgeErrorCodes.BatchNotFound)
            : Ok(value);
    }

    private IActionResult FromError(EvaluationBatchError error) =>
        Error(error.Code switch
        {
            EvaluationBatchErrorCodes.SuiteNotFound
                or EvaluationBatchErrorCodes.VersionNotFound => 404,
            EvaluationBatchErrorCodes.PersistenceConflict => 409,
            EvaluationBatchErrorCodes.ExecutionFailed => 500,
            _ => 400
        }, error.Code, error.Message);

    private IActionResult Error(int status, string code, string? detail = null) =>
        ApiProblemResults.Create(
            HttpContext,
            status,
            code,
            "The evaluation batch operation could not be completed.",
            detail);
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

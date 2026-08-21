#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.Evaluation;

public static class EvaluationComparisonErrorCodes
{
    public const string BatchNotFound = "EVALUATION_COMPARISON_BATCH_NOT_FOUND";
    public const string BatchNotTerminal = "EVALUATION_COMPARISON_BATCH_NOT_TERMINAL";
    public const string SuiteMismatch = "EVALUATION_COMPARISON_SUITE_MISMATCH";
    public const string SpecificationInvalid = "EVALUATION_COMPARISON_SPECIFICATION_INVALID";
}

public sealed record EvaluationQualityGateSpecification(
    decimal MinimumCandidatePassRate,
    decimal MaximumPassRateRegression,
    decimal? MaximumAverageDurationRegressionPercent,
    int? MaximumToolCallIncreasePerCase,
    bool RequireNoNewFailures,
    bool RequireSameCaseSet,
    bool RequireStableRoutes);

public sealed record EvaluationBatchMetrics(
    int TotalCases,
    int PassedCases,
    int FailedCases,
    decimal PassRate,
    decimal? AverageDurationMilliseconds,
    int TotalToolCalls);

public sealed record EvaluationCaseComparison(
    Guid CaseId,
    string CaseName,
    EvaluationCaseExecutionStatus BaselineStatus,
    EvaluationCaseExecutionStatus CandidateStatus,
    long? BaselineDurationMilliseconds,
    long? CandidateDurationMilliseconds,
    int ToolCallDelta,
    bool RoutesChanged,
    bool EventKindsChanged,
    bool NewFailure);

public sealed record EvaluationGateCheck(
    string Code,
    bool Passed,
    string Expected,
    string Actual);

public sealed record EvaluationBatchComparisonReport(
    Guid BaselineBatchId,
    Guid CandidateBatchId,
    Guid SuiteId,
    Guid BaselineSuiteVersionId,
    Guid CandidateSuiteVersionId,
    EvaluationBatchMetrics Baseline,
    EvaluationBatchMetrics Candidate,
    IReadOnlyList<Guid> AddedCaseIds,
    IReadOnlyList<Guid> RemovedCaseIds,
    IReadOnlyList<EvaluationCaseComparison> Cases,
    IReadOnlyList<EvaluationGateCheck> GateChecks,
    bool GatePassed,
    DateTimeOffset ComparedAtUtc);

public sealed record EvaluationComparisonError(string Code, string Message);

public sealed record EvaluationComparisonOperationResult(
    EvaluationBatchComparisonReport? Value,
    EvaluationComparisonError? Error)
{
    public bool Succeeded => Error is null;

    public static EvaluationComparisonOperationResult Success(
        EvaluationBatchComparisonReport value) => new(value, null);

    public static EvaluationComparisonOperationResult Failure(
        string code,
        string message) => new(null, new EvaluationComparisonError(code, message));
}

#nullable enable

using System.Collections.ObjectModel;
using EU.Core.Model;

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

public interface IEvaluationBatchComparisonService
{
    Task<ServiceResult<EvaluationBatchComparisonReport>> CompareAsync(
        Guid baselineBatchId,
        Guid candidateBatchId,
        string tenantId,
        EvaluationQualityGateSpecification specification,
        CancellationToken cancellationToken = default);
}

public static class EvaluationComparisonServiceStatusCodes
{
    public const int BatchNotFound = 670018;
    public const int BatchNotTerminal = 670019;
    public const int SuiteMismatch = 670020;
    public const int SpecificationInvalid = 670021;

    public static int FromErrorCode(string code) => code switch
    {
        EvaluationComparisonErrorCodes.BatchNotFound => BatchNotFound,
        EvaluationComparisonErrorCodes.BatchNotTerminal => BatchNotTerminal,
        EvaluationComparisonErrorCodes.SuiteMismatch => SuiteMismatch,
        EvaluationComparisonErrorCodes.SpecificationInvalid => SpecificationInvalid,
        _ => 500
    };

    public static string ToErrorCode(int status) => status switch
    {
        BatchNotFound => EvaluationComparisonErrorCodes.BatchNotFound,
        BatchNotTerminal => EvaluationComparisonErrorCodes.BatchNotTerminal,
        SuiteMismatch => EvaluationComparisonErrorCodes.SuiteMismatch,
        SpecificationInvalid => EvaluationComparisonErrorCodes.SpecificationInvalid,
        _ => "INTERNAL_ERROR"
    };
}

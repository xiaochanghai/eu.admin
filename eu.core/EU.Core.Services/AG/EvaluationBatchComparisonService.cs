using System.Collections.ObjectModel;
using EU.Core.IServices.Evaluation;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

public sealed class EvaluationBatchComparisonService(
    IEvaluationBatchRepository batches,
    TimeProvider? timeProvider = null) : BaseServices
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<ServiceResult<EvaluationBatchComparisonReport>> CompareAsync(
        Guid baselineBatchId,
        Guid candidateBatchId,
        string tenantId,
        EvaluationQualityGateSpecification specification,
        CancellationToken cancellationToken = default)
    {
        if (baselineBatchId == Guid.Empty
            || candidateBatchId == Guid.Empty
            || baselineBatchId == candidateBatchId
            || string.IsNullOrWhiteSpace(tenantId)
            || !Valid(specification))
        {
            return Failure(
                EvaluationComparisonErrorCodes.SpecificationInvalid,
                "The evaluation comparison request is invalid.");
        }

        EvaluationBatchRecord? baseline = await batches.GetAsync(
            baselineBatchId, tenantId, cancellationToken);
        EvaluationBatchRecord? candidate = await batches.GetAsync(
            candidateBatchId, tenantId, cancellationToken);
        if (baseline is null || candidate is null)
        {
            return Failure(
                EvaluationComparisonErrorCodes.BatchNotFound,
                "An evaluation batch was not found.");
        }

        if (baseline.Status != EvaluationBatchStatus.Completed
            || candidate.Status != EvaluationBatchStatus.Completed)
        {
            return Failure(
                EvaluationComparisonErrorCodes.BatchNotTerminal,
                "Both evaluation batches must be completed.");
        }

        if (baseline.SuiteId != candidate.SuiteId)
        {
            return Failure(
                EvaluationComparisonErrorCodes.SuiteMismatch,
                "Evaluation batches from different Suites cannot be compared.");
        }

        Dictionary<Guid, EvaluationCaseExecutionRecord> baselineCases =
            baseline.Cases.ToDictionary(value => value.CaseId);
        Dictionary<Guid, EvaluationCaseExecutionRecord> candidateCases =
            candidate.Cases.ToDictionary(value => value.CaseId);
        Guid[] added = candidateCases.Keys.Except(baselineCases.Keys).Order().ToArray();
        Guid[] removed = baselineCases.Keys.Except(candidateCases.Keys).Order().ToArray();
        var comparisons = baselineCases.Keys.Intersect(candidateCases.Keys)
            .Order()
            .Select(id => CompareCase(baselineCases[id], candidateCases[id]))
            .ToArray();
        EvaluationBatchMetrics baselineMetrics = Metrics(baseline.Cases);
        EvaluationBatchMetrics candidateMetrics = Metrics(candidate.Cases);
        IReadOnlyList<EvaluationGateCheck> checks = EvaluateGate(
            specification,
            baselineMetrics,
            candidateMetrics,
            comparisons,
            added,
            removed);
        var report = new EvaluationBatchComparisonReport(
            baseline.Id,
            candidate.Id,
            baseline.SuiteId,
            baseline.SuiteVersionId,
            candidate.SuiteVersionId,
            baselineMetrics,
            candidateMetrics,
            new ReadOnlyCollection<Guid>(added),
            new ReadOnlyCollection<Guid>(removed),
            new ReadOnlyCollection<EvaluationCaseComparison>(comparisons),
            checks,
            checks.All(value => value.Passed),
            _timeProvider.GetUtcNow().ToUniversalTime());
        return Success(report);
    }

    private static EvaluationCaseComparison CompareCase(
        EvaluationCaseExecutionRecord baseline,
        EvaluationCaseExecutionRecord candidate) =>
        new(
            baseline.CaseId,
            candidate.CaseName,
            baseline.Status,
            candidate.Status,
            baseline.DurationMilliseconds,
            candidate.DurationMilliseconds,
            candidate.ToolCallCount - baseline.ToolCallCount,
            !SequenceEqual(baseline.ObservedRoutes, candidate.ObservedRoutes),
            !SequenceEqual(baseline.ObservedEventKinds, candidate.ObservedEventKinds),
            baseline.Status == EvaluationCaseExecutionStatus.Passed
                && candidate.Status != EvaluationCaseExecutionStatus.Passed);

    private static EvaluationBatchMetrics Metrics(
        IReadOnlyList<EvaluationCaseExecutionRecord> cases)
    {
        int passed = cases.Count(value => value.Status == EvaluationCaseExecutionStatus.Passed);
        long[] durations = cases
            .Where(value => value.DurationMilliseconds.HasValue)
            .Select(value => value.DurationMilliseconds!.Value)
            .ToArray();
        return new EvaluationBatchMetrics(
            cases.Count,
            passed,
            cases.Count - passed,
            cases.Count == 0
                ? 0m
                : decimal.Round(passed / (decimal)cases.Count, 4),
            durations.Length == 0 || durations.Length != cases.Count
                ? null
                : decimal.Round(durations.Average(value => (decimal)value), 2),
            cases.Sum(value => value.ToolCallCount));
    }

    private static IReadOnlyList<EvaluationGateCheck> EvaluateGate(
        EvaluationQualityGateSpecification specification,
        EvaluationBatchMetrics baseline,
        EvaluationBatchMetrics candidate,
        IReadOnlyList<EvaluationCaseComparison> cases,
        IReadOnlyList<Guid> added,
        IReadOnlyList<Guid> removed)
    {
        var checks = new List<EvaluationGateCheck>();
        Add(
            checks,
            "minimum-pass-rate",
            candidate.PassRate >= specification.MinimumCandidatePassRate,
            $">= {specification.MinimumCandidatePassRate:P2}",
            candidate.PassRate.ToString("P2"));
        decimal regression = baseline.PassRate - candidate.PassRate;
        Add(
            checks,
            "pass-rate-regression",
            regression <= specification.MaximumPassRateRegression,
            $"<= {specification.MaximumPassRateRegression:P2}",
            regression.ToString("P2"));
        if (specification.RequireNoNewFailures)
        {
            int failures = cases.Count(value => value.NewFailure);
            Add(checks, "new-failures", failures == 0, "0", failures.ToString());
        }

        if (specification.RequireSameCaseSet)
        {
            Add(
                checks,
                "case-set",
                added.Count == 0 && removed.Count == 0,
                "unchanged",
                $"added={added.Count}; removed={removed.Count}");
        }

        if (specification.MaximumAverageDurationRegressionPercent.HasValue)
        {
            decimal? percent = DurationRegressionPercent(
                baseline.AverageDurationMilliseconds,
                candidate.AverageDurationMilliseconds);
            Add(
                checks,
                "average-duration-regression",
                percent.HasValue
                    && percent.Value <= specification.MaximumAverageDurationRegressionPercent.Value,
                $"<= {specification.MaximumAverageDurationRegressionPercent.Value}%",
                percent.HasValue ? $"{percent.Value}%" : "unavailable");
        }

        if (specification.MaximumToolCallIncreasePerCase.HasValue)
        {
            int maximum = cases.Count == 0 ? 0 : cases.Max(value => value.ToolCallDelta);
            Add(
                checks,
                "tool-call-increase",
                maximum <= specification.MaximumToolCallIncreasePerCase.Value,
                $"<= {specification.MaximumToolCallIncreasePerCase.Value}",
                maximum.ToString());
        }

        if (specification.RequireStableRoutes)
        {
            int changed = cases.Count(value => value.RoutesChanged);
            Add(checks, "stable-routes", changed == 0, "0 changed", $"{changed} changed");
        }

        return new ReadOnlyCollection<EvaluationGateCheck>(checks);
    }

    private static decimal? DurationRegressionPercent(decimal? baseline, decimal? candidate)
    {
        if (!baseline.HasValue || !candidate.HasValue || baseline.Value <= 0)
        {
            return null;
        }

        return decimal.Round(
            (candidate.Value - baseline.Value) / baseline.Value * 100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static bool SequenceEqual(
        IReadOnlyList<string>? left,
        IReadOnlyList<string>? right) =>
        (left ?? []).SequenceEqual(right ?? [], StringComparer.Ordinal);

    private static bool Valid(EvaluationQualityGateSpecification? value) =>
        value is not null
        && value.MinimumCandidatePassRate is >= 0m and <= 1m
        && value.MaximumPassRateRegression is >= 0m and <= 1m
        && value.MaximumAverageDurationRegressionPercent is null
            or >= 0m and <= 10_000m
        && value.MaximumToolCallIncreasePerCase is null or >= 0 and <= 1000;

    private static void Add(
        ICollection<EvaluationGateCheck> checks,
        string code,
        bool passed,
        string expected,
        string actual) =>
        checks.Add(new EvaluationGateCheck(code, passed, expected, actual));

    private static ServiceResult<EvaluationBatchComparisonReport> Failure(
        string code,
        string message) => ServiceResult<EvaluationBatchComparisonReport>.Failure(
            EvaluationComparisonServiceStatusCodes.FromErrorCode(code),
            message);
}

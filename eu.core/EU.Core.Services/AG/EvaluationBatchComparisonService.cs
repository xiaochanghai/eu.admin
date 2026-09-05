using System.Collections.ObjectModel;
using EU.Core.IServices.Evaluation;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

// 文件职责：EvaluationBatchComparisonService 职责实现

/// <summary>
/// 比较基线与候选评测批次并执行质量门禁。
/// </summary>
/// <param name="batches">用于读取和持久化评测批次的仓储。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器；为 null 时使用系统时间提供器。</param>
public sealed class EvaluationBatchComparisonService(
    IEvaluationBatchRepository batches,
    TimeProvider? timeProvider = null) : BaseServices, IEvaluationBatchComparisonService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    #region 比较（CompareAsync）
    /// <summary>
    /// 比较（CompareAsync）
    /// </summary>
    /// <param name="baselineBatchId">基线评估批次标识。</param>
    /// <param name="candidateBatchId">待比较的评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="specification">评估规范。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测批次对比报告，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 比较（CompareCase）
    /// <summary>
    /// 比较（CompareCase）
    /// </summary>
    /// <param name="baseline">基线数据。</param>
    /// <param name="candidate">候选数据。</param>
    /// <returns>同一用例的状态、耗时、工具调用差值、路由及事件变化和新增失败判定。</returns>
    private static EvaluationCaseComparison CompareCase(EvaluationCaseExecutionRecord baseline, EvaluationCaseExecutionRecord candidate) =>
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
    #endregion

    #region 处理（Metrics）
    /// <summary>
    /// 处理（Metrics）
    /// </summary>
    /// <param name="cases">评估用例集合。</param>
    /// <returns>批次用例总数、通过率、工具调用总数和平均耗时；任一用例缺失耗时时平均耗时为 null。</returns>
    private static EvaluationBatchMetrics Metrics(IReadOnlyList<EvaluationCaseExecutionRecord> cases)
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
    #endregion

    #region 处理（EvaluateGate）
    /// <summary>
    /// 处理（EvaluateGate）
    /// </summary>
    /// <param name="specification">评估规范。</param>
    /// <param name="baseline">基线数据。</param>
    /// <param name="candidate">候选数据。</param>
    /// <param name="cases">评估用例集合。</param>
    /// <param name="added">新增的数据。</param>
    /// <param name="removed">已移除的数据。</param>
    /// <returns>按质量门禁配置生成的各项检查结果，包含通过标志及预期和实际值。</returns>
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
    #endregion

    #region 处理（DurationRegressionPercent）
    /// <summary>
    /// 处理（DurationRegressionPercent）
    /// </summary>
    /// <param name="baseline">基线数据。</param>
    /// <param name="candidate">候选数据。</param>
    /// <returns>候选相对基线的耗时变化百分比，保留两位小数；缺少耗时或基线不大于零时为 null。</returns>
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
    #endregion

    #region 按顺序比较评测字符串列表（SequenceEqual）
    /// <summary>
    /// 按顺序比较评测字符串列表（SequenceEqual）。
    /// </summary>
    /// <param name="left">待比较的第一组字符串，null 按空列表处理。</param>
    /// <param name="right">待比较的第二组字符串，null 按空列表处理。</param>
    /// <returns>将 null 视为空列表后，数量、顺序及区分大小写的元素值均一致时返回 true，否则返回 false。</returns>
    private static bool SequenceEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right) =>
        (left ?? []).SequenceEqual(right ?? [], StringComparer.Ordinal);
    #endregion

    #region 校验评测质量门禁阈值（Valid）
    /// <summary>
    /// 校验评测质量门禁阈值（Valid）。
    /// </summary>
    /// <param name="value">待校验的评测批次质量门禁规范。</param>
    /// <returns>规范非 null、通过率及回退比例均在 0 至 1，且可选耗时回退百分比在 0 至 10000、可选单用例工具增量在 0 至 1000 时返回 true，否则返回 false。</returns>
    private static bool Valid(EvaluationQualityGateSpecification? value) =>
        value is not null
        && value.MinimumCandidatePassRate is >= 0m and <= 1m
        && value.MaximumPassRateRegression is >= 0m and <= 1m
        && value.MaximumAverageDurationRegressionPercent is null
            or >= 0m and <= 10_000m
        && value.MaximumToolCallIncreasePerCase is null or >= 0 and <= 1000;
    #endregion

    #region 添加（Add）
    /// <summary>
    /// 添加（Add）
    /// </summary>
    /// <param name="checks">评估检查项集合。</param>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="passed">校验或评估是否通过。</param>
    /// <param name="expected">期望匹配的值。</param>
    /// <param name="actual">实际取得的值。</param>
    private static void Add(ICollection<EvaluationGateCheck> checks, string code, bool passed, string expected, string actual) =>
        checks.Add(new EvaluationGateCheck(code, passed, expected, actual));
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<EvaluationBatchComparisonReport> Failure(string code, string message) => ServiceResult<EvaluationBatchComparisonReport>.Failure(
            EvaluationComparisonServiceStatusCodes.FromErrorCode(code),
            message);
    #endregion
}

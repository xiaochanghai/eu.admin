#nullable enable

using System.Collections.ObjectModel;
using EU.Core.Model;

namespace EU.Core.IServices.Evaluation;

/// <summary>
/// 定义评测批次对比错误码。
/// </summary>
public static class EvaluationComparisonErrorCodes
{
    /// <summary>表示 <c>BatchNotFound</c> 场景的错误码。</summary>
    public const string BatchNotFound = "EVALUATION_COMPARISON_BATCH_NOT_FOUND";
    /// <summary>表示 <c>BatchNotTerminal</c> 场景的错误码。</summary>
    public const string BatchNotTerminal = "EVALUATION_COMPARISON_BATCH_NOT_TERMINAL";
    /// <summary>表示 <c>SuiteMismatch</c> 场景的错误码。</summary>
    public const string SuiteMismatch = "EVALUATION_COMPARISON_SUITE_MISMATCH";
    /// <summary>表示 <c>SpecificationInvalid</c> 场景的错误码。</summary>
    public const string SpecificationInvalid = "EVALUATION_COMPARISON_SPECIFICATION_INVALID";
}

/// <summary>
/// 评测批次对比使用的质量门禁规则。
/// </summary>
/// <param name="MinimumCandidatePassRate">候选批次必须达到的最低通过率。</param>
/// <param name="MaximumPassRateRegression">候选批次允许的最大通过率下降值。</param>
/// <param name="MaximumAverageDurationRegressionPercent">候选批次允许的平均耗时最大回退百分比。</param>
/// <param name="MaximumToolCallIncreasePerCase">单个用例允许增加的最大工具调用次数。</param>
/// <param name="RequireNoNewFailures">是否要求候选批次不得新增失败用例。</param>
/// <param name="RequireSameCaseSet">是否要求两个批次包含相同用例集合。</param>
/// <param name="RequireStableRoutes">是否要求执行路由保持不变。</param>
public sealed record EvaluationQualityGateSpecification(
    decimal MinimumCandidatePassRate,
    decimal MaximumPassRateRegression,
    decimal? MaximumAverageDurationRegressionPercent,
    int? MaximumToolCallIncreasePerCase,
    bool RequireNoNewFailures,
    bool RequireSameCaseSet,
    bool RequireStableRoutes);

/// <summary>
/// 评测批次的聚合指标。
/// </summary>
/// <param name="TotalCases">评测用例总数。</param>
/// <param name="PassedCases">通过的评测用例数量。</param>
/// <param name="FailedCases">失败的评测用例数量。</param>
/// <param name="PassRate">评测用例通过率。</param>
/// <param name="AverageDurationMilliseconds">用例平均执行时长，单位为毫秒。</param>
/// <param name="TotalToolCalls">所有用例的工具调用总数。</param>
public sealed record EvaluationBatchMetrics(
    int TotalCases,
    int PassedCases,
    int FailedCases,
    decimal PassRate,
    decimal? AverageDurationMilliseconds,
    int TotalToolCalls);

/// <summary>
/// 同一评测用例在基线和候选批次中的对比结果。
/// </summary>
/// <param name="CaseId">评测用例标识。</param>
/// <param name="CaseName">评测用例名称。</param>
/// <param name="BaselineStatus">基线批次中的用例状态。</param>
/// <param name="CandidateStatus">候选批次中的用例状态。</param>
/// <param name="BaselineDurationMilliseconds">基线批次中的执行耗时，单位为毫秒。</param>
/// <param name="CandidateDurationMilliseconds">候选批次中的执行耗时，单位为毫秒。</param>
/// <param name="ToolCallDelta">候选批次相对基线增加的工具调用次数。</param>
/// <param name="RoutesChanged">执行路由是否发生变化。</param>
/// <param name="EventKindsChanged">运行事件类型集合是否发生变化。</param>
/// <param name="NewFailure">候选批次是否出现新的失败。</param>
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

/// <summary>
/// 单项质量门禁检查结果。
/// </summary>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Passed">检查项或评测是否通过。</param>
/// <param name="Expected">检查项的期望值。</param>
/// <param name="Actual">检查项的实际值。</param>
public sealed record EvaluationGateCheck(
    string Code,
    bool Passed,
    string Expected,
    string Actual);

/// <summary>
/// 基线与候选评测批次的对比报告。
/// </summary>
/// <param name="BaselineBatchId">基线评测批次标识。</param>
/// <param name="CandidateBatchId">候选评测批次标识。</param>
/// <param name="SuiteId">评测套件标识。</param>
/// <param name="BaselineSuiteVersionId">基线批次使用的套件版本标识。</param>
/// <param name="CandidateSuiteVersionId">候选批次使用的套件版本标识。</param>
/// <param name="Baseline">基线批次的聚合指标。</param>
/// <param name="Candidate">候选批次的聚合指标。</param>
/// <param name="AddedCaseIds">候选批次新增的用例标识集合。</param>
/// <param name="RemovedCaseIds">候选批次缺少的用例标识集合。</param>
/// <param name="Cases">评测用例定义、结果或对比集合。</param>
/// <param name="GateChecks">质量门禁检查结果集合。</param>
/// <param name="GatePassed">候选批次是否通过全部质量门禁。</param>
/// <param name="ComparedAtUtc">批次对比完成的 UTC 时间。</param>
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

/// <summary>
/// 定义评测批次对比和质量门禁服务。
/// </summary>
public interface IEvaluationBatchComparisonService
{
    /// <summary>比较基线与候选评测批次并执行质量门禁。</summary>
    Task<ServiceResult<EvaluationBatchComparisonReport>> CompareAsync(
        Guid baselineBatchId,
        Guid candidateBatchId,
        string tenantId,
        EvaluationQualityGateSpecification specification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 将评测对比错误映射为服务状态码。
/// </summary>
public static class EvaluationComparisonServiceStatusCodes
{
    /// <summary>表示 <c>BatchNotFound</c> 场景映射的服务状态码。</summary>
    public const int BatchNotFound = 670018;
    /// <summary>表示 <c>BatchNotTerminal</c> 场景映射的服务状态码。</summary>
    public const int BatchNotTerminal = 670019;
    /// <summary>表示 <c>SuiteMismatch</c> 场景映射的服务状态码。</summary>
    public const int SuiteMismatch = 670020;
    /// <summary>表示 <c>SpecificationInvalid</c> 场景映射的服务状态码。</summary>
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

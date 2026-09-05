#nullable enable

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Evaluation;

/// <summary>
/// 定义模型裁判领域错误码。
/// </summary>
public static class ModelJudgeErrorCodes
{
    /// <summary>表示 <c>Disabled</c> 场景的错误码。</summary>
    public const string Disabled = "MODEL_JUDGE_DISABLED";
    /// <summary>表示 <c>RequestInvalid</c> 场景的错误码。</summary>
    public const string RequestInvalid = "MODEL_JUDGE_REQUEST_INVALID";
    /// <summary>表示 <c>BatchNotFound</c> 场景的错误码。</summary>
    public const string BatchNotFound = "MODEL_JUDGE_BATCH_NOT_FOUND";
    /// <summary>表示 <c>BatchNotCompleted</c> 场景的错误码。</summary>
    public const string BatchNotCompleted = "MODEL_JUDGE_BATCH_NOT_COMPLETED";
    /// <summary>表示 <c>SuiteArchived</c> 场景的错误码。</summary>
    public const string SuiteArchived = "MODEL_JUDGE_SUITE_ARCHIVED";
    /// <summary>表示 <c>RunUnavailable</c> 场景的错误码。</summary>
    public const string RunUnavailable = "MODEL_JUDGE_RUN_UNAVAILABLE";
    /// <summary>表示 <c>ModelUnavailable</c> 场景的错误码。</summary>
    public const string ModelUnavailable = "MODEL_JUDGE_MODEL_UNAVAILABLE";
    /// <summary>表示 <c>ExecutionFailed</c> 场景的错误码。</summary>
    public const string ExecutionFailed = "MODEL_JUDGE_EXECUTION_FAILED";
    /// <summary>表示 <c>PersistenceConflict</c> 场景的错误码。</summary>
    public const string PersistenceConflict = "MODEL_JUDGE_PERSISTENCE_CONFLICT";
}

/// <summary>
/// 定义平台支持的模型裁判评估器名称。
/// </summary>
public static class ModelJudgeEvaluators
{
    /// <summary>相关性模型裁判评估器的标识。</summary>
    public const string Relevance = "Relevance";
    /// <summary>连贯性模型裁判评估器的标识。</summary>
    public const string Coherence = "Coherence";
    /// <summary>模型裁判规则包版本字段的标识。</summary>
    public const string PackageVersion = "10.6.0";
    /// <summary>模型裁判提供程序字段的标识。</summary>
    public const string Provider = "Microsoft.Extensions.AI.Evaluation.Quality";

    /// <summary>平台支持的模型裁判评估器标识集合。</summary>
    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>([Relevance, Coherence], StringComparer.Ordinal);
}

/// <summary>
/// 模型裁判功能的运行策略。
/// </summary>
/// <param name="Enabled">是否启用模型裁判。</param>
/// <param name="MaximumCases">单批允许进行模型裁判的最大用例数。</param>
/// <param name="Timeout">单次模型裁判调用的超时时间。</param>
public sealed record ModelJudgePolicy(
    bool Enabled,
    int MaximumCases,
    TimeSpan Timeout);

/// <summary>
/// 单次模型裁判评测的配置。
/// </summary>
/// <param name="ExplicitlyEnabled">本次评测是否显式启用模型裁判。</param>
/// <param name="ModelProfileId">模型裁判使用的模型配置标识。</param>
/// <param name="Evaluators">需要执行的模型裁判评估器集合。</param>
/// <param name="MinimumScores">各指标要求的最低分数字典。</param>
public sealed record ModelJudgeSpecification(
    bool ExplicitlyEnabled,
    string ModelProfileId,
    IReadOnlyList<string> Evaluators,
    IReadOnlyDictionary<string, decimal> MinimumScores);

/// <summary>
/// 模型裁判引擎返回的原始指标。
/// </summary>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Score">模型裁判或综合评测分数。</param>
/// <param name="DiagnosticCodes">模型裁判返回的诊断码集合。</param>
public sealed record ModelJudgeEngineMetric(
    string Name,
    decimal? Score,
    IReadOnlyList<string> DiagnosticCodes);

/// <summary>
/// 定义调用模型并生成裁判指标的引擎。
/// </summary>
public interface IModelJudgeEngine
{
    #region 执行模型裁判评测。
    /// <summary>执行模型裁判评测。</summary>
    /// <param name="input">执行输入内容。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="modelProfileId">模型配置标识。</param>
    /// <param name="evaluators">评估器集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定模型和评估器对输入输出给出的裁判指标集合。</returns>
    Task<IReadOnlyList<ModelJudgeEngineMetric>> EvaluateAsync(
        string input,
        string output,
        string modelProfileId,
        IReadOnlyList<string> evaluators,
        CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 经过规则判定的模型裁判指标。
/// </summary>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Score">模型裁判或综合评测分数。</param>
/// <param name="MinimumScore">该指标要求的最低分数。</param>
/// <param name="Passed">检查项或评测是否通过。</param>
/// <param name="DiagnosticCodes">模型裁判返回的诊断码集合。</param>
public sealed record ModelJudgeMetric(
    string Name,
    decimal? Score,
    decimal MinimumScore,
    bool Passed,
    IReadOnlyList<string> DiagnosticCodes);

/// <summary>
/// 单个用例的模型裁判结果。
/// </summary>
/// <param name="CaseId">评测用例标识。</param>
/// <param name="CaseName">评测用例名称。</param>
/// <param name="UnifiedRunId">评测关联的统一入口运行标识。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="OutputSha256">输出内容的 SHA-256 摘要。</param>
/// <param name="Metrics">模型裁判指标集合。</param>
public sealed record ModelJudgeCaseResult(
    Guid CaseId,
    string CaseName,
    Guid UnifiedRunId,
    string InputSha256,
    string OutputSha256,
    IReadOnlyList<ModelJudgeMetric> Metrics);

/// <summary>
/// 模型裁判评测报告。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="RequestedBy">发起评测的用户标识。</param>
/// <param name="BatchId">评测批次标识。</param>
/// <param name="SuiteId">评测套件标识。</param>
/// <param name="SuiteVersionId">评测套件版本标识。</param>
/// <param name="SuiteVersionContentSha256">评测套件版本内容的 SHA-256 摘要。</param>
/// <param name="Provider">模型裁判提供程序。</param>
/// <param name="PackageVersion">模型裁判实现或规则包版本。</param>
/// <param name="ModelProfileId">模型裁判使用的模型配置标识。</param>
/// <param name="Evaluators">需要执行的模型裁判评估器集合。</param>
/// <param name="MinimumScores">各指标要求的最低分数字典。</param>
/// <param name="ConfigurationSha256">模型裁判配置的 SHA-256 摘要。</param>
/// <param name="PromptVersion">模型裁判提示词版本。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="AdvisoryPassed">模型裁判建议性门禁是否通过。</param>
/// <param name="Cases">评测用例定义、结果或对比集合。</param>
public sealed record ModelJudgeReport(
    Guid Id,
    string TenantId,
    string RequestedBy,
    Guid BatchId,
    Guid SuiteId,
    Guid SuiteVersionId,
    string SuiteVersionContentSha256,
    string Provider,
    string PackageVersion,
    string ModelProfileId,
    IReadOnlyList<string> Evaluators,
    IReadOnlyDictionary<string, decimal> MinimumScores,
    string ConfigurationSha256,
    string PromptVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    bool AdvisoryPassed,
    IReadOnlyList<ModelJudgeCaseResult> Cases);

/// <summary>
/// 定义模型裁判报告的存储边界。
/// </summary>
public interface IModelJudgeReportRepository
{
    #region 获取模型裁判报告。
    /// <summary>获取模型裁判报告。</summary>
    /// <param name="id">模型裁判报告标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下的模型裁判报告；不存在时为 null。</returns>
    Task<ModelJudgeReport?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 按模型裁判配置摘要获取报告。
    /// <summary>按模型裁判配置摘要获取报告。</summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="configurationSha256">配置内容的 SHA-256 摘要。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户、批次和配置摘要对应的模型裁判报告；不存在时为 null。</returns>
    Task<ModelJudgeReport?> GetByConfigurationAsync(Guid batchId, string tenantId, string configurationSha256, CancellationToken cancellationToken = default);
    #endregion

    #region 查询模型裁判报告列表。
    /// <summary>查询模型裁判报告列表。</summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和批次下受数量限制的模型裁判报告集合。</returns>
    Task<IReadOnlyList<ModelJudgeReport>> ListAsync(Guid batchId, string tenantId, int take, CancellationToken cancellationToken = default);
    #endregion

    #region 创建模型裁判报告及明细（TryCreateAsync）
    /// <summary>
    /// 创建模型裁判报告及明细（TryCreateAsync）。
    /// </summary>
    /// <param name="value">待创建的模型裁判报告及关联明细。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>报告及明细持久化成功时返回 true；报告标识重复，或相同租户、批次和配置摘要的报告已存在时返回 false。</returns>
    Task<bool> TryCreateAsync(ModelJudgeReport value, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义模型裁判评测的应用服务。
/// </summary>
public interface IModelJudgeService
{
    #region 获取模型裁判评测。
    /// <summary>获取模型裁判评测。</summary>
    /// <param name="id">模型裁判报告标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下的模型裁判报告；不存在时为 null。</returns>
    Task<ModelJudgeReport?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询模型裁判评测列表。
    /// <summary>查询模型裁判评测列表。</summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和批次下受数量限制的模型裁判报告集合。</returns>
    Task<IReadOnlyList<ModelJudgeReport>> ListAsync(Guid batchId, string tenantId, int take, CancellationToken cancellationToken = default);
    #endregion

    #region 执行模型裁判评测。
    /// <summary>执行模型裁判评测。</summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="requestedBy">请求发起方标识。</param>
    /// <param name="specification">评估规范。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含模型裁判报告，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<ModelJudgeReport>> EvaluateAsync(
        Guid batchId,
        string tenantId,
        string requestedBy,
        ModelJudgeSpecification specification,
        CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 将模型裁判错误映射为服务状态码。
/// </summary>
public static class ModelJudgeServiceStatusCodes
{
    /// <summary>表示 <c>Disabled</c> 场景映射的服务状态码。</summary>
    public const int Disabled = 670022;
    /// <summary>表示 <c>RequestInvalid</c> 场景映射的服务状态码。</summary>
    public const int RequestInvalid = 670023;
    /// <summary>表示 <c>BatchNotFound</c> 场景映射的服务状态码。</summary>
    public const int BatchNotFound = 670024;
    /// <summary>表示 <c>BatchNotCompleted</c> 场景映射的服务状态码。</summary>
    public const int BatchNotCompleted = 670025;
    /// <summary>表示 <c>SuiteArchived</c> 场景映射的服务状态码。</summary>
    public const int SuiteArchived = 670026;
    /// <summary>表示 <c>RunUnavailable</c> 场景映射的服务状态码。</summary>
    public const int RunUnavailable = 670027;
    /// <summary>表示 <c>ModelUnavailable</c> 场景映射的服务状态码。</summary>
    public const int ModelUnavailable = 670028;
    /// <summary>表示 <c>ExecutionFailed</c> 场景映射的服务状态码。</summary>
    public const int ExecutionFailed = 670029;
    /// <summary>表示 <c>PersistenceConflict</c> 场景映射的服务状态码。</summary>
    public const int PersistenceConflict = 670030;

    #region 转换（FromErrorCode）
    /// <summary>
    /// 转换（FromErrorCode）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <returns>模型裁判错误码对应的服务状态值；未知错误码使用 500。</returns>
    public static int FromErrorCode(string code) => code switch
    {
        ModelJudgeErrorCodes.Disabled => Disabled,
        ModelJudgeErrorCodes.RequestInvalid => RequestInvalid,
        ModelJudgeErrorCodes.BatchNotFound => BatchNotFound,
        ModelJudgeErrorCodes.BatchNotCompleted => BatchNotCompleted,
        ModelJudgeErrorCodes.SuiteArchived => SuiteArchived,
        ModelJudgeErrorCodes.RunUnavailable => RunUnavailable,
        ModelJudgeErrorCodes.ModelUnavailable => ModelUnavailable,
        ModelJudgeErrorCodes.ExecutionFailed => ExecutionFailed,
        ModelJudgeErrorCodes.PersistenceConflict => PersistenceConflict,
        _ => 500
    };
    #endregion

    #region 转换（ToErrorCode）
    /// <summary>
    /// 转换（ToErrorCode）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <returns>服务状态值对应的模型裁判错误码；未知状态使用 INTERNAL_ERROR。</returns>
    public static string ToErrorCode(int status) => status switch
    {
        Disabled => ModelJudgeErrorCodes.Disabled,
        RequestInvalid => ModelJudgeErrorCodes.RequestInvalid,
        BatchNotFound => ModelJudgeErrorCodes.BatchNotFound,
        BatchNotCompleted => ModelJudgeErrorCodes.BatchNotCompleted,
        SuiteArchived => ModelJudgeErrorCodes.SuiteArchived,
        RunUnavailable => ModelJudgeErrorCodes.RunUnavailable,
        ModelUnavailable => ModelJudgeErrorCodes.ModelUnavailable,
        ExecutionFailed => ModelJudgeErrorCodes.ExecutionFailed,
        PersistenceConflict => ModelJudgeErrorCodes.PersistenceConflict,
        _ => "INTERNAL_ERROR"
    };
    #endregion
}

/// <summary>
/// 提供模型裁判契约对象的防御性复制。
/// </summary>
public static class ModelJudgeContractCloner
{
    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="value">本次操作使用的模型裁判报告。</param>
    /// <returns>复制评估器、评分阈值、用例指标及诊断代码后的模型裁判报告副本。</returns>
    public static ModelJudgeReport Clone(ModelJudgeReport value) => value with
    {
        Evaluators = value.Evaluators.ToArray(),
        MinimumScores = new ReadOnlyDictionary<string, decimal>(
            new Dictionary<string, decimal>(value.MinimumScores, StringComparer.Ordinal)),
        Cases = new ReadOnlyCollection<ModelJudgeCaseResult>(value.Cases.Select(item => item with
        {
            Metrics = new ReadOnlyCollection<ModelJudgeMetric>(item.Metrics.Select(metric => metric with
            {
                DiagnosticCodes = metric.DiagnosticCodes.ToArray()
            }).ToArray())
        }).ToArray())
    };
    #endregion

    #region 读取（ReadOnly）
    /// <summary>
    /// 读取（ReadOnly）
    /// </summary>
    /// <param name="values">按原顺序枚举并复制为只读集合的源数据。</param>
    /// <returns>逐个复制报告及其嵌套指标后生成的只读报告集合。</returns>
    public static IReadOnlyList<ModelJudgeReport> ReadOnly(IEnumerable<ModelJudgeReport> values) =>
        new ReadOnlyCollection<ModelJudgeReport>(values.Select(Clone).ToArray());
    #endregion
}

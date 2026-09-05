#nullable enable

using System.Collections.ObjectModel;
using System.Text.Json;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Evaluation;

/// <summary>
/// 评测批次的运行状态。
/// </summary>
public enum EvaluationBatchStatus
{
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>已完成。</summary>
    Completed,
    /// <summary>已取消。</summary>
    Cancelled,
    /// <summary>运行失败。</summary>
    Failed
}

/// <summary>
/// 单个评测用例的执行状态。
/// </summary>
public enum EvaluationCaseExecutionStatus
{
    /// <summary>等待执行。</summary>
    Pending,
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>评测通过。</summary>
    Passed,
    /// <summary>评测未通过或执行失败。</summary>
    Failed,
    /// <summary>已取消。</summary>
    Cancelled
}

/// <summary>
/// 定义评测批次领域错误码。
/// </summary>
public static class EvaluationBatchErrorCodes
{
    /// <summary>表示 <c>RequestInvalid</c> 场景的错误码。</summary>
    public const string RequestInvalid = "EVALUATION_BATCH_REQUEST_INVALID";
    /// <summary>表示 <c>BatchNotFound</c> 场景的错误码。</summary>
    public const string BatchNotFound = "EVALUATION_BATCH_NOT_FOUND";
    /// <summary>表示 <c>SuiteNotFound</c> 场景的错误码。</summary>
    public const string SuiteNotFound = "EVALUATION_BATCH_SUITE_NOT_FOUND";
    /// <summary>表示 <c>VersionNotFound</c> 场景的错误码。</summary>
    public const string VersionNotFound = "EVALUATION_BATCH_VERSION_NOT_FOUND";
    /// <summary>表示 <c>CaseLimitExceeded</c> 场景的错误码。</summary>
    public const string CaseLimitExceeded = "EVALUATION_BATCH_CASE_LIMIT_EXCEEDED";
    /// <summary>表示 <c>TargetUnavailable</c> 场景的错误码。</summary>
    public const string TargetUnavailable = "EVALUATION_BATCH_TARGET_UNAVAILABLE";
    /// <summary>表示 <c>PersistenceConflict</c> 场景的错误码。</summary>
    public const string PersistenceConflict = "EVALUATION_BATCH_PERSISTENCE_CONFLICT";
    /// <summary>表示 <c>ExecutionFailed</c> 场景的错误码。</summary>
    public const string ExecutionFailed = "EVALUATION_BATCH_EXECUTION_FAILED";
    /// <summary>表示 <c>AssertionFailed</c> 场景的错误码。</summary>
    public const string AssertionFailed = "EVALUATION_BATCH_ASSERTION_FAILED";
    /// <summary>表示 <c>Cancelled</c> 场景的错误码。</summary>
    public const string Cancelled = "EVALUATION_BATCH_CANCELLED";
}

/// <summary>
/// 评测批次中的用例执行记录。
/// </summary>
/// <param name="CaseId">评测用例标识。</param>
/// <param name="CaseName">评测用例名称。</param>
/// <param name="TargetAgentId">被评测的目标 Agent 标识。</param>
/// <param name="TargetAgentVersionId">被评测的目标 Agent 版本标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="UnifiedRunId">评测关联的统一入口运行标识。</param>
/// <param name="UnifiedRunStatus">评测关联运行的状态。</param>
/// <param name="Report">运行确定性评测报告。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record EvaluationCaseExecutionRecord(
    Guid CaseId,
    string CaseName,
    Guid TargetAgentId,
    Guid TargetAgentVersionId,
    EvaluationCaseExecutionStatus Status,
    Guid? UnifiedRunId,
    UnifiedRunStatus? UnifiedRunStatus,
    RunEvaluationReport? Report,
    string ErrorCode)
{
    /// <summary>
    /// 执行耗时，单位为毫秒。
    /// </summary>
    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// 工具调用次数。
    /// </summary>
    public int ToolCallCount { get; init; }

    /// <summary>
    /// 评测观察到的事件类型集合。
    /// </summary>
    public IReadOnlyList<string> ObservedEventKinds { get; init; } = [];

    /// <summary>
    /// 评测观察到的执行路由集合。
    /// </summary>
    public IReadOnlyList<string> ObservedRoutes { get; init; } = [];
}

/// <summary>
/// 评测批次记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="RequestedBy">发起评测的用户标识。</param>
/// <param name="SuiteId">评测套件标识。</param>
/// <param name="SuiteVersionId">评测套件版本标识。</param>
/// <param name="SuiteVersionContentSha256">评测套件版本内容的 SHA-256 摘要。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="Cases">评测用例定义、结果或对比集合。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record EvaluationBatchRecord(
    Guid Id,
    string TenantId,
    string RequestedBy,
    Guid SuiteId,
    Guid SuiteVersionId,
    string SuiteVersionContentSha256,
    EvaluationBatchStatus Status,
    long LogicalRevision,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    IReadOnlyList<EvaluationCaseExecutionRecord> Cases,
    string ErrorCode);

/// <summary>
/// 将评测批次错误映射为服务状态码。
/// </summary>
public static class EvaluationBatchServiceStatusCodes
{
    /// <summary>表示 <c>RequestInvalid</c> 场景映射的服务状态码。</summary>
    public const int RequestInvalid = 670008;
    /// <summary>表示 <c>BatchNotFound</c> 场景映射的服务状态码。</summary>
    public const int BatchNotFound = 670009;
    /// <summary>表示 <c>SuiteNotFound</c> 场景映射的服务状态码。</summary>
    public const int SuiteNotFound = 670010;
    /// <summary>表示 <c>VersionNotFound</c> 场景映射的服务状态码。</summary>
    public const int VersionNotFound = 670011;
    /// <summary>表示 <c>CaseLimitExceeded</c> 场景映射的服务状态码。</summary>
    public const int CaseLimitExceeded = 670012;
    /// <summary>表示 <c>TargetUnavailable</c> 场景映射的服务状态码。</summary>
    public const int TargetUnavailable = 670013;
    /// <summary>表示 <c>PersistenceConflict</c> 场景映射的服务状态码。</summary>
    public const int PersistenceConflict = 670014;
    /// <summary>表示 <c>ExecutionFailed</c> 场景映射的服务状态码。</summary>
    public const int ExecutionFailed = 670015;
    /// <summary>表示 <c>AssertionFailed</c> 场景映射的服务状态码。</summary>
    public const int AssertionFailed = 670016;
    /// <summary>表示 <c>Cancelled</c> 场景映射的服务状态码。</summary>
    public const int Cancelled = 670017;

    public static int FromErrorCode(string code) => code switch
    {
        EvaluationBatchErrorCodes.RequestInvalid => RequestInvalid,
        EvaluationBatchErrorCodes.BatchNotFound => BatchNotFound,
        EvaluationBatchErrorCodes.SuiteNotFound => SuiteNotFound,
        EvaluationBatchErrorCodes.VersionNotFound => VersionNotFound,
        EvaluationBatchErrorCodes.CaseLimitExceeded => CaseLimitExceeded,
        EvaluationBatchErrorCodes.TargetUnavailable => TargetUnavailable,
        EvaluationBatchErrorCodes.PersistenceConflict => PersistenceConflict,
        EvaluationBatchErrorCodes.ExecutionFailed => ExecutionFailed,
        EvaluationBatchErrorCodes.AssertionFailed => AssertionFailed,
        EvaluationBatchErrorCodes.Cancelled => Cancelled,
        _ => 500
    };

    public static string ToErrorCode(int status) => status switch
    {
        RequestInvalid => EvaluationBatchErrorCodes.RequestInvalid,
        BatchNotFound => EvaluationBatchErrorCodes.BatchNotFound,
        SuiteNotFound => EvaluationBatchErrorCodes.SuiteNotFound,
        VersionNotFound => EvaluationBatchErrorCodes.VersionNotFound,
        CaseLimitExceeded => EvaluationBatchErrorCodes.CaseLimitExceeded,
        TargetUnavailable => EvaluationBatchErrorCodes.TargetUnavailable,
        PersistenceConflict => EvaluationBatchErrorCodes.PersistenceConflict,
        ExecutionFailed => EvaluationBatchErrorCodes.ExecutionFailed,
        AssertionFailed => EvaluationBatchErrorCodes.AssertionFailed,
        Cancelled => EvaluationBatchErrorCodes.Cancelled,
        _ => "INTERNAL_ERROR"
    };
}

/// <summary>
/// 定义评测批次记录的存储和状态转换边界。
/// </summary>
public interface IEvaluationBatchRepository
{
    /// <summary>获取评测批次记录。</summary>
    Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>查询评测批次记录列表。</summary>
    Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>尝试创建评测批次记录。</summary>
    Task<bool> TryCreateAsync(
        EvaluationBatchRecord value,
        CancellationToken cancellationToken = default);

    /// <summary>按并发条件尝试替换评测批次记录。</summary>
    Task<bool> TryReplaceAsync(
        EvaluationBatchRecord value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义中断评测批次的恢复能力。
/// </summary>
public interface IEvaluationBatchRecovery
{
    /// <summary>恢复或终结中断的评测批次。</summary>
    Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 提供评测批次契约对象的防御性复制。
/// </summary>
public static class EvaluationBatchContractCloner
{
    public static EvaluationBatchRecord Clone(EvaluationBatchRecord value) =>
        value with { Cases = CloneCases(value.Cases) };

    public static IReadOnlyList<EvaluationBatchRecord> ReadOnly(
        IEnumerable<EvaluationBatchRecord> values) =>
        new ReadOnlyCollection<EvaluationBatchRecord>(values.Select(Clone).ToArray());

    public static IReadOnlyList<EvaluationCaseExecutionRecord> CloneCases(
        IEnumerable<EvaluationCaseExecutionRecord> values) =>
        new ReadOnlyCollection<EvaluationCaseExecutionRecord>(values.Select(value =>
            value with
            {
                ObservedEventKinds = (value.ObservedEventKinds ?? []).ToArray(),
                ObservedRoutes = (value.ObservedRoutes ?? []).ToArray(),
                Report = value.Report is null
                    ? null
                    : value.Report with
                    {
                        Checks = new ReadOnlyCollection<RunEvaluationCheck>(
                            value.Report.Checks.Select(check => check with { }).ToArray())
                    }
            }).ToArray());
}

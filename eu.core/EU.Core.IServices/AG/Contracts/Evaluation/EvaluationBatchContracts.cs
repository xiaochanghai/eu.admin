#nullable enable

using System.Collections.ObjectModel;
using System.Text.Json;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Evaluation;

public enum EvaluationBatchStatus
{
    Running,
    Completed,
    Cancelled,
    Failed
}

public enum EvaluationCaseExecutionStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Cancelled
}

public static class EvaluationBatchErrorCodes
{
    public const string RequestInvalid = "EVALUATION_BATCH_REQUEST_INVALID";
    public const string BatchNotFound = "EVALUATION_BATCH_NOT_FOUND";
    public const string SuiteNotFound = "EVALUATION_BATCH_SUITE_NOT_FOUND";
    public const string VersionNotFound = "EVALUATION_BATCH_VERSION_NOT_FOUND";
    public const string CaseLimitExceeded = "EVALUATION_BATCH_CASE_LIMIT_EXCEEDED";
    public const string TargetUnavailable = "EVALUATION_BATCH_TARGET_UNAVAILABLE";
    public const string PersistenceConflict = "EVALUATION_BATCH_PERSISTENCE_CONFLICT";
    public const string ExecutionFailed = "EVALUATION_BATCH_EXECUTION_FAILED";
    public const string AssertionFailed = "EVALUATION_BATCH_ASSERTION_FAILED";
    public const string Cancelled = "EVALUATION_BATCH_CANCELLED";
}

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
    public long? DurationMilliseconds { get; init; }

    public int ToolCallCount { get; init; }

    public IReadOnlyList<string> ObservedEventKinds { get; init; } = [];

    public IReadOnlyList<string> ObservedRoutes { get; init; } = [];
}

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

public static class EvaluationBatchServiceStatusCodes
{
    public const int RequestInvalid = 670008;
    public const int BatchNotFound = 670009;
    public const int SuiteNotFound = 670010;
    public const int VersionNotFound = 670011;
    public const int CaseLimitExceeded = 670012;
    public const int TargetUnavailable = 670013;
    public const int PersistenceConflict = 670014;
    public const int ExecutionFailed = 670015;
    public const int AssertionFailed = 670016;
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

public interface IEvaluationBatchRepository
{
    Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        EvaluationBatchRecord value,
        CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        EvaluationBatchRecord value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);
}

public interface IEvaluationBatchRecovery
{
    Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default);
}

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

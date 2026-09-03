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

public static class ModelJudgeErrorCodes
{
    public const string Disabled = "MODEL_JUDGE_DISABLED";
    public const string RequestInvalid = "MODEL_JUDGE_REQUEST_INVALID";
    public const string BatchNotFound = "MODEL_JUDGE_BATCH_NOT_FOUND";
    public const string BatchNotCompleted = "MODEL_JUDGE_BATCH_NOT_COMPLETED";
    public const string SuiteArchived = "MODEL_JUDGE_SUITE_ARCHIVED";
    public const string RunUnavailable = "MODEL_JUDGE_RUN_UNAVAILABLE";
    public const string ModelUnavailable = "MODEL_JUDGE_MODEL_UNAVAILABLE";
    public const string ExecutionFailed = "MODEL_JUDGE_EXECUTION_FAILED";
    public const string PersistenceConflict = "MODEL_JUDGE_PERSISTENCE_CONFLICT";
}

public static class ModelJudgeEvaluators
{
    public const string Relevance = "Relevance";
    public const string Coherence = "Coherence";
    public const string PackageVersion = "10.6.0";
    public const string Provider = "Microsoft.Extensions.AI.Evaluation.Quality";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>([Relevance, Coherence], StringComparer.Ordinal);
}

public sealed record ModelJudgePolicy(
    bool Enabled,
    int MaximumCases,
    TimeSpan Timeout);

public sealed record ModelJudgeSpecification(
    bool ExplicitlyEnabled,
    string ModelProfileId,
    IReadOnlyList<string> Evaluators,
    IReadOnlyDictionary<string, decimal> MinimumScores);

public sealed record ModelJudgeEngineMetric(
    string Name,
    decimal? Score,
    IReadOnlyList<string> DiagnosticCodes);

public interface IModelJudgeEngine
{
    Task<IReadOnlyList<ModelJudgeEngineMetric>> EvaluateAsync(
        string input,
        string output,
        string modelProfileId,
        IReadOnlyList<string> evaluators,
        CancellationToken cancellationToken = default);
}

public sealed record ModelJudgeMetric(
    string Name,
    decimal? Score,
    decimal MinimumScore,
    bool Passed,
    IReadOnlyList<string> DiagnosticCodes);

public sealed record ModelJudgeCaseResult(
    Guid CaseId,
    string CaseName,
    Guid UnifiedRunId,
    string InputSha256,
    string OutputSha256,
    IReadOnlyList<ModelJudgeMetric> Metrics);

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

public interface IModelJudgeReportRepository
{
    Task<ModelJudgeReport?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<ModelJudgeReport?> GetByConfigurationAsync(
        Guid batchId,
        string tenantId,
        string configurationSha256,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        ModelJudgeReport value,
        CancellationToken cancellationToken = default);
}

public interface IModelJudgeService
{
    Task<ModelJudgeReport?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ModelJudgeReport>> EvaluateAsync(
        Guid batchId,
        string tenantId,
        string requestedBy,
        ModelJudgeSpecification specification,
        CancellationToken cancellationToken = default);
}

public static class ModelJudgeServiceStatusCodes
{
    public const int Disabled = 670022;
    public const int RequestInvalid = 670023;
    public const int BatchNotFound = 670024;
    public const int BatchNotCompleted = 670025;
    public const int SuiteArchived = 670026;
    public const int RunUnavailable = 670027;
    public const int ModelUnavailable = 670028;
    public const int ExecutionFailed = 670029;
    public const int PersistenceConflict = 670030;

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
}

public static class ModelJudgeContractCloner
{
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

    public static IReadOnlyList<ModelJudgeReport> ReadOnly(IEnumerable<ModelJudgeReport> values) =>
        new ReadOnlyCollection<ModelJudgeReport>(values.Select(Clone).ToArray());
}

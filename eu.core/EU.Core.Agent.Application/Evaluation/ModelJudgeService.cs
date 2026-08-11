using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.UnifiedEntry;

namespace EU.Core.Agent.Application.Evaluation;

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

public sealed record ModelJudgeError(string Code, string Message);

public sealed record ModelJudgeOperationResult(
    ModelJudgeReport? Value,
    ModelJudgeError? Error)
{
    public bool Succeeded => Error is null;

    public static ModelJudgeOperationResult Success(ModelJudgeReport value) =>
        new(value, null);

    public static ModelJudgeOperationResult Failure(string code, string message) =>
        new(null, new ModelJudgeError(code, message));
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

public sealed class ModelJudgeService(
    IEvaluationBatchRepository batches,
    IEvaluationSuiteRepository suites,
    IModelJudgeReportRepository reports,
    IUnifiedEntryRepository unifiedRuns,
    IModelProfileReferenceCatalog modelProfiles,
    IModelJudgeEngine engine,
    ModelJudgePolicy policy,
    TimeProvider? timeProvider = null)
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _configurationGates =
        new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task<ModelJudgeReport?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default) =>
        reports.GetAsync(id, tenantId, cancellationToken);

    public Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default) =>
        reports.ListAsync(batchId, tenantId, Math.Clamp(take, 1, 50), cancellationToken);

    public async Task<ModelJudgeOperationResult> EvaluateAsync(
        Guid batchId,
        string tenantId,
        string requestedBy,
        ModelJudgeSpecification specification,
        CancellationToken cancellationToken = default)
    {
        if (!policy.Enabled)
        {
            return Failure(ModelJudgeErrorCodes.Disabled,
                "Model-based evaluation is disabled by Host configuration.");
        }

        if (!ValidRequest(batchId, tenantId, requestedBy, specification))
        {
            return Failure(ModelJudgeErrorCodes.RequestInvalid,
                "The model judge request is invalid or was not explicitly enabled.");
        }

        EvaluationBatchRecord? batch = await batches.GetAsync(
            batchId, tenantId, cancellationToken);
        if (batch is null)
        {
            return Failure(ModelJudgeErrorCodes.BatchNotFound,
                "The evaluation batch was not found.");
        }

        if (batch.Status != EvaluationBatchStatus.Completed)
        {
            return Failure(ModelJudgeErrorCodes.BatchNotCompleted,
                "Only a completed evaluation batch can be judged.");
        }

        EvaluationSuiteDefinition? suite = await suites.GetAsync(
            batch.SuiteId, tenantId, cancellationToken);
        if (suite?.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(ModelJudgeErrorCodes.SuiteArchived,
                "An archived evaluation suite must be restored before a new model-judge report can be created.");
        }

        if (batch.Cases.Count is < 1 || batch.Cases.Count > policy.MaximumCases)
        {
            return Failure(ModelJudgeErrorCodes.RequestInvalid,
                "The batch exceeds the configured model judge case budget.");
        }

        if (!await modelProfiles.ExistsAsync(specification.ModelProfileId, cancellationToken))
        {
            return Failure(ModelJudgeErrorCodes.ModelUnavailable,
                "The selected model profile is not available.");
        }

        string configurationHash = ConfigurationHash(specification);
        string gateKey = $"{tenantId}:{batch.Id:D}:{configurationHash}";
        SemaphoreSlim gate = _configurationGates.GetOrAdd(
            gateKey,
            static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            ModelJudgeReport? existing = await reports.GetByConfigurationAsync(
                batch.Id, tenantId, configurationHash, cancellationToken);
            if (existing is not null)
            {
                return ModelJudgeOperationResult.Success(existing);
            }

            DateTimeOffset started = _timeProvider.GetUtcNow().ToUniversalTime();
            var results = new List<ModelJudgeCaseResult>(batch.Cases.Count);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(policy.Timeout);
            try
            {
            foreach (EvaluationCaseExecutionRecord testCase in batch.Cases)
            {
                if (!testCase.UnifiedRunId.HasValue)
                {
                    return Failure(ModelJudgeErrorCodes.RunUnavailable,
                        "A batch Case does not reference a persisted Unified Run.");
                }

                UnifiedRunDetails? details = await unifiedRuns.GetDetailsForOwnerAsync(
                    testCase.UnifiedRunId.Value,
                    tenantId,
                    batch.RequestedBy,
                    timeout.Token);
                if (details is null
                    || details.EntryRun.Status != UnifiedRunStatus.Completed
                    || string.IsNullOrWhiteSpace(details.EntryRun.Output))
                {
                    return Failure(ModelJudgeErrorCodes.RunUnavailable,
                        "A completed persisted Unified Run with output is required.");
                }

                IReadOnlyList<ModelJudgeEngineMetric> metrics = await engine.EvaluateAsync(
                    details.EntryRun.Input,
                    details.EntryRun.Output,
                    specification.ModelProfileId,
                    specification.Evaluators,
                    timeout.Token);
                ModelJudgeMetric[] frozen = metrics.Select(metric =>
                {
                    decimal threshold = specification.MinimumScores[metric.Name];
                    return new ModelJudgeMetric(
                        metric.Name,
                        metric.Score,
                        threshold,
                        metric.Score.HasValue && metric.Score.Value >= threshold,
                        metric.DiagnosticCodes.Take(16).ToArray());
                }).ToArray();
                if (frozen.Length != specification.Evaluators.Count
                    || frozen.Select(value => value.Name).ToHashSet(StringComparer.Ordinal)
                        .SetEquals(specification.Evaluators) is false)
                {
                    throw new InvalidOperationException("The model judge returned an unexpected metric set.");
                }

                results.Add(new ModelJudgeCaseResult(
                    testCase.CaseId,
                    testCase.CaseName,
                    testCase.UnifiedRunId.Value,
                    details.EntryRun.InputSha256,
                    details.EntryRun.OutputSha256,
                    new ReadOnlyCollection<ModelJudgeMetric>(frozen)));
            }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Failure(ModelJudgeErrorCodes.ExecutionFailed,
                    "The model judge exceeded its configured timeout.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return Failure(ModelJudgeErrorCodes.ExecutionFailed,
                    "The model judge could not complete safely.");
            }

            DateTimeOffset finished = _timeProvider.GetUtcNow().ToUniversalTime();
            var report = new ModelJudgeReport(
            Guid.NewGuid(),
            tenantId,
            requestedBy,
            batch.Id,
            batch.SuiteId,
            batch.SuiteVersionId,
            batch.SuiteVersionContentSha256,
            ModelJudgeEvaluators.Provider,
            ModelJudgeEvaluators.PackageVersion,
            specification.ModelProfileId,
            specification.Evaluators.ToArray(),
            new ReadOnlyDictionary<string, decimal>(
                new Dictionary<string, decimal>(specification.MinimumScores, StringComparer.Ordinal)),
            configurationHash,
            $"builtin-quality-prompts@{ModelJudgeEvaluators.PackageVersion}",
            started,
            finished,
            results.SelectMany(value => value.Metrics).All(value => value.Passed),
            new ReadOnlyCollection<ModelJudgeCaseResult>(results));
            if (!await reports.TryCreateAsync(report, cancellationToken))
            {
                existing = await reports.GetByConfigurationAsync(
                    batch.Id, tenantId, configurationHash, cancellationToken);
                return existing is not null
                    ? ModelJudgeOperationResult.Success(existing)
                    : Failure(ModelJudgeErrorCodes.PersistenceConflict,
                        "The model judge report could not be persisted.");
            }

            return ModelJudgeOperationResult.Success(ModelJudgeContractCloner.Clone(report));
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool ValidRequest(
        Guid batchId,
        string tenantId,
        string requestedBy,
        ModelJudgeSpecification? value)
    {
        if (batchId == Guid.Empty
            || string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(requestedBy)
            || value is null
            || !value.ExplicitlyEnabled
            || string.IsNullOrWhiteSpace(value.ModelProfileId)
            || value.ModelProfileId.Length > 200
            || value.Evaluators is null
            || value.Evaluators.Count is < 1 or > 2
            || value.Evaluators.Distinct(StringComparer.Ordinal).Count() != value.Evaluators.Count
            || value.Evaluators.Any(item => !ModelJudgeEvaluators.Supported.Contains(item))
            || value.MinimumScores is null
            || value.MinimumScores.Count != value.Evaluators.Count)
        {
            return false;
        }

        return value.MinimumScores.All(item =>
            value.Evaluators.Contains(item.Key, StringComparer.Ordinal)
            && item.Value is >= 1m and <= 5m);
    }

    private static string ConfigurationHash(ModelJudgeSpecification value)
    {
        var canonical = new
        {
            provider = ModelJudgeEvaluators.Provider,
            packageVersion = ModelJudgeEvaluators.PackageVersion,
            modelProfileId = value.ModelProfileId.Trim(),
            evaluators = value.Evaluators.Order(StringComparer.Ordinal).ToArray(),
            minimumScores = value.MinimumScores
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
            promptVersion = $"builtin-quality-prompts@{ModelJudgeEvaluators.PackageVersion}"
        };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static ModelJudgeOperationResult Failure(string code, string message) =>
        ModelJudgeOperationResult.Failure(code, message);
}

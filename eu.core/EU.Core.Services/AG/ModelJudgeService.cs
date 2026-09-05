using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices;
using EU.Core.IServices.Agents;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

// 文件职责：ModelJudgeService 职责实现

/// <summary>
/// 组织模型裁判评测并持久化报告。
/// </summary>
/// <param name="batches">用于读取和持久化评测批次的仓储。</param>
/// <param name="suites">用于管理评测套件及用例的服务。</param>
/// <param name="reports">用于读取和持久化模型裁判报告的仓储。</param>
/// <param name="unifiedRuns">用于读取和持久化统一入口会话、运行及事件的仓储。</param>
/// <param name="modelProfiles">用于查询模型配置引用的目录。</param>
/// <param name="engine">用于执行模型裁判评测的引擎。</param>
/// <param name="policy">模型裁判评测使用的执行策略。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器；为 null 时使用系统时间提供器。</param>
public sealed class ModelJudgeService(
    IEvaluationBatchRepository batches,
    IAgEvaluationSuiteServices suites,
    IModelJudgeReportRepository reports,
    IUnifiedEntryRepository unifiedRuns,
    IModelProfileReferenceCatalog modelProfiles,
    IModelJudgeEngine engine,
    ModelJudgePolicy policy,
    TimeProvider? timeProvider = null) : BaseServices, IModelJudgeService
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _configurationGates =
        new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">模型裁判报告标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下的模型裁判报告；不存在时为 null。</returns>
    public Task<ModelJudgeReport?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
        reports.GetAsync(id, tenantId, cancellationToken);
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和批次下的最近模型裁判报告，最多 50 条。</returns>
    public Task<IReadOnlyList<ModelJudgeReport>> ListAsync(Guid batchId, string tenantId, int take, CancellationToken cancellationToken = default) =>
        reports.ListAsync(batchId, tenantId, Math.Clamp(take, 1, 50), cancellationToken);
    #endregion

    #region 处理（EvaluateAsync）
    /// <summary>
    /// 处理（EvaluateAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="requestedBy">请求发起方标识。</param>
    /// <param name="specification">评估规范。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含模型裁判报告，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<ModelJudgeReport>> EvaluateAsync(
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
                return Success(existing);
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
                    ? Success(existing)
                    : Failure(ModelJudgeErrorCodes.PersistenceConflict,
                        "The model judge report could not be persisted.");
            }

            return Success(ModelJudgeContractCloner.Clone(report));
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion

    #region 校验模型裁判请求及评分配置（ValidRequest）
    /// <summary>
    /// 校验模型裁判请求及评分配置（ValidRequest）。
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="requestedBy">请求发起方标识。</param>
    /// <param name="value">待校验的模型裁判规范，包含模型标识、评估器和最低分映射。</param>
    /// <returns>批次和身份有效、显式启用裁判、模型标识合法、评估器为 1 至 2 个不重复的受支持项，且对应最低分均在 1 至 5 时返回 true，否则返回 false。</returns>
    private static bool ValidRequest(Guid batchId, string tenantId, string requestedBy, ModelJudgeSpecification? value)
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
    #endregion

    #region 处理（ConfigurationHash）
    /// <summary>
    /// 处理（ConfigurationHash）
    /// </summary>
    /// <param name="value">本次操作使用的模型裁判配置。</param>
    /// <returns>包含提供方、包版本、模型配置、有序评估器和阈值及提示版本的规范化配置 SHA-256 摘要。</returns>
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
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<ModelJudgeReport> Failure(string code, string message) =>
        ServiceResult<ModelJudgeReport>.Failure(
            ModelJudgeServiceStatusCodes.FromErrorCode(code),
            message);
    #endregion
}

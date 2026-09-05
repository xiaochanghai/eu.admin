using System.Collections.ObjectModel;
using EU.Core.IServices.Evaluation;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgEvaluationModelJudgementServices 职责实现

/// <summary>
/// 提供模型裁判结果的持久化服务。
/// </summary>
public sealed class AgEvaluationModelJudgementServices :
    BaseServices<AgEvaluationModelJudgement>,
    IAgEvaluationModelJudgementServices,
    IModelJudgeReportRepository
{
    #region 构造（AgEvaluationModelJudgementServices）
    /// <summary>
    /// 构造（AgEvaluationModelJudgementServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgEvaluationModelJudgementServices(IBaseRepository<AgEvaluationModelJudgement> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">模型裁判报告标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下的模型裁判报告及用例结果；不存在时为 null。</returns>
    public async Task<ModelJudgeReport?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgEvaluationModelJudgement? judgement = await Db.Queryable<AgEvaluationModelJudgement>()
                .Where(value => value.ID == id && value.TenantId == tenantId && !value.IsDeleted)
                .FirstAsync();
            ModelJudgeReport? result = judgement is null
                ? null
                : (await LoadReportsAsync([judgement], cancellationToken))[0];
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 获取（GetByConfigurationAsync）
    /// <summary>
    /// 获取（GetByConfigurationAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="configurationSha256">配置内容的 SHA-256 摘要。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户、批次及配置摘要对应的模型裁判报告；不存在时为 null。</returns>
    public async Task<ModelJudgeReport?> GetByConfigurationAsync(
        Guid batchId,
        string tenantId,
        string configurationSha256,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgEvaluationModelJudgement? judgement = await Db.Queryable<AgEvaluationModelJudgement>()
                .Where(value =>
                    value.BatchId == batchId &&
                    value.TenantId == tenantId &&
                    value.ConfigurationSha256 == configurationSha256 &&
                    !value.IsDeleted)
                .FirstAsync();
            ModelJudgeReport? result = judgement is null
                ? null
                : (await LoadReportsAsync([judgement], cancellationToken))[0];
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和批次下的模型裁判报告，按开始时间及标识倒序排列，最多 50 条。</returns>
    public async Task<IReadOnlyList<ModelJudgeReport>> ListAsync(Guid batchId, string tenantId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgEvaluationModelJudgement> judgements = await Db.Queryable<AgEvaluationModelJudgement>()
                .Where(value =>
                    value.BatchId == batchId &&
                    value.TenantId == tenantId &&
                    !value.IsDeleted)
                .OrderBy(value => value.StartedAtUtc, OrderByType.Desc)
                .OrderBy(value => value.ID, OrderByType.Desc)
                .Take(Math.Clamp(take, 1, 50))
                .ToListAsync();
            IReadOnlyList<ModelJudgeReport> result = await LoadReportsAsync(
                judgements,
                cancellationToken);
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 创建模型裁判报告及明细（TryCreateAsync）
    /// <summary>
    /// 创建模型裁判报告及明细（TryCreateAsync）。
    /// </summary>
    /// <param name="value">待创建的模型裁判报告及关联明细。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>报告及明细持久化成功时返回 true；报告标识重复，或相同租户、批次和配置摘要的报告已存在时返回 false。</returns>
    public async Task<bool> TryCreateAsync(ModelJudgeReport value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool duplicate = await Db.Queryable<AgEvaluationModelJudgement>()
                .Where(candidate =>
                    candidate.ID == value.Id ||
                    (candidate.TenantId == value.TenantId &&
                     candidate.BatchId == value.BatchId &&
                     candidate.ConfigurationSha256 == value.ConfigurationSha256))
                .AnyAsync();
            if (duplicate)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapEntity(value)).ExecuteCommandAsync();
            await InsertChildrenAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 加载（LoadReportsAsync）
    /// <summary>
    /// 加载（LoadReportsAsync）
    /// </summary>
    /// <param name="judgements">模型评判记录集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保持输入顺序并补齐评估器、阈值、用例指标及诊断代码的模型裁判报告集合。</returns>
    private async Task<IReadOnlyList<ModelJudgeReport>> LoadReportsAsync(
        IReadOnlyList<AgEvaluationModelJudgement> judgements,
        CancellationToken cancellationToken)
    {
        if (judgements.Count == 0)
        {
            return [];
        }

        Guid[] judgementIds = judgements.Select(value => value.ID).ToArray();
        List<AgEvaluationModelJudgementEvaluator> evaluators = await Db
            .Queryable<AgEvaluationModelJudgementEvaluator>()
            .Where(value =>
                value.JudgementId.HasValue &&
                judgementIds.Contains(value.JudgementId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.JudgementId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        List<AgEvaluationModelJudgementMinimumScore> minimumScores = await Db
            .Queryable<AgEvaluationModelJudgementMinimumScore>()
            .Where(value =>
                value.JudgementId.HasValue &&
                judgementIds.Contains(value.JudgementId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.JudgementId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        List<AgEvaluationModelJudgementCase> cases = await Db
            .Queryable<AgEvaluationModelJudgementCase>()
            .Where(value =>
                value.JudgementId.HasValue &&
                judgementIds.Contains(value.JudgementId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.JudgementId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        Guid[] caseIds = cases.Select(value => value.ID).ToArray();
        List<AgEvaluationModelJudgementMetric> metrics = caseIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationModelJudgementMetric>()
                .Where(value =>
                    value.JudgementCaseId.HasValue &&
                    caseIds.Contains(value.JudgementCaseId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.JudgementCaseId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        Guid[] metricIds = metrics.Select(value => value.ID).ToArray();
        List<AgEvaluationModelJudgementDiagnostic> diagnostics = metricIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationModelJudgementDiagnostic>()
                .Where(value =>
                    value.JudgementMetricId.HasValue &&
                    metricIds.Contains(value.JudgementMetricId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.JudgementMetricId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementEvaluator[]> evaluatorsByJudgement =
            evaluators.GroupBy(value => Required(value.JudgementId, "Evaluator.JudgementId"))
                .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementMinimumScore[]> scoresByJudgement =
            minimumScores.GroupBy(value => Required(value.JudgementId, "MinimumScore.JudgementId"))
                .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementCase[]> casesByJudgement =
            cases.GroupBy(value => Required(value.JudgementId, "Case.JudgementId"))
                .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementMetric[]> metricsByCase =
            metrics.GroupBy(value => Required(value.JudgementCaseId, "Metric.JudgementCaseId"))
                .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementDiagnostic[]> diagnosticsByMetric =
            diagnostics.GroupBy(value => Required(value.JudgementMetricId, "Diagnostic.JudgementMetricId"))
                .ToDictionary(group => group.Key, group => group.ToArray());

        return ModelJudgeContractCloner.ReadOnly(judgements.Select(judgement => MapReport(
            judgement,
            evaluatorsByJudgement.GetValueOrDefault(judgement.ID) ?? [],
            scoresByJudgement.GetValueOrDefault(judgement.ID) ?? [],
            casesByJudgement.GetValueOrDefault(judgement.ID) ?? [],
            metricsByCase,
            diagnosticsByMetric)));
    }
    #endregion

    #region 映射（MapReport）
    /// <summary>
    /// 映射（MapReport）
    /// </summary>
    /// <param name="value">本次操作使用的模型裁判报告实体。</param>
    /// <param name="evaluators">评估器集合。</param>
    /// <param name="minimumScores">各评估指标要求的最低分数。</param>
    /// <param name="cases">评估用例集合。</param>
    /// <param name="metricsByCase">按用例分组的评估指标。</param>
    /// <param name="diagnosticsByMetric">按指标分组的诊断数据。</param>
    /// <returns>包含评估器、评分阈值及用例指标的模型裁判报告。</returns>
    private static ModelJudgeReport MapReport(
        AgEvaluationModelJudgement value,
        IReadOnlyList<AgEvaluationModelJudgementEvaluator> evaluators,
        IReadOnlyList<AgEvaluationModelJudgementMinimumScore> minimumScores,
        IReadOnlyList<AgEvaluationModelJudgementCase> cases,
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementMetric[]> metricsByCase,
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementDiagnostic[]> diagnosticsByMetric) =>
        new(
            value.ID,
            Required(value.TenantId, "TenantId"),
            Required(value.RequestedByUserId, "RequestedByUserId"),
            Required(value.BatchId, "BatchId"),
            Required(value.SuiteId, "SuiteId"),
            Required(value.SuiteVersionId, "SuiteVersionId"),
            Required(value.SuiteVersionContentSha256, "SuiteVersionContentSha256"),
            Required(value.Provider, "Provider"),
            Required(value.PackageVersion, "PackageVersion"),
            Required(value.ModelProfileId, "ModelProfileId"),
            Array.AsReadOnly(evaluators
                .OrderBy(item => Required(item.Ordinal, "Evaluator.Ordinal"))
                .ThenBy(item => item.ID)
                .Select(item => Required(item.Name, "Evaluator.Name"))
                .ToArray()),
            new ReadOnlyDictionary<string, decimal>(minimumScores
                .OrderBy(item => Required(item.Ordinal, "MinimumScore.Ordinal"))
                .ThenBy(item => item.ID)
                .ToDictionary(
                    item => Required(item.Name, "MinimumScore.Name"),
                    item => Required(item.Score, "MinimumScore.Score"),
                    StringComparer.Ordinal)),
            Required(value.ConfigurationSha256, "ConfigurationSha256"),
            Required(value.PromptVersion, "PromptVersion"),
            ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")),
            ToOffset(Required(value.FinishedAtUtc, "FinishedAtUtc")),
            Required(value.AdvisoryPassed, "AdvisoryPassed"),
            Array.AsReadOnly(cases
                .OrderBy(item => Required(item.Ordinal, "Case.Ordinal"))
                .ThenBy(item => item.ID)
                .Select(item => MapCase(
                    item,
                    metricsByCase.GetValueOrDefault(item.ID) ?? [],
                    diagnosticsByMetric))
                .ToArray()));
    #endregion

    #region 映射（MapCase）
    /// <summary>
    /// 映射（MapCase）
    /// </summary>
    /// <param name="value">本次操作使用的模型裁判用例实体。</param>
    /// <param name="metrics">评估指标集合。</param>
    /// <param name="diagnosticsByMetric">按指标分组的诊断数据。</param>
    /// <returns>包含各指标分数、通过状态及诊断代码的模型裁判用例结果。</returns>
    private static ModelJudgeCaseResult MapCase(
        AgEvaluationModelJudgementCase value,
        IReadOnlyList<AgEvaluationModelJudgementMetric> metrics,
        IReadOnlyDictionary<Guid, AgEvaluationModelJudgementDiagnostic[]> diagnosticsByMetric) =>
        new(
            Required(value.CaseId, "Case.CaseId"),
            Required(value.CaseName, "Case.CaseName"),
            Required(value.UnifiedRunId, "Case.UnifiedRunId"),
            Required(value.InputSha256, "Case.InputSha256"),
            Required(value.OutputSha256, "Case.OutputSha256"),
            Array.AsReadOnly(metrics
                .OrderBy(item => Required(item.Ordinal, "Metric.Ordinal"))
                .ThenBy(item => item.ID)
                .Select(item => new ModelJudgeMetric(
                    Required(item.Name, "Metric.Name"),
                    item.Score,
                    Required(item.MinimumScore, "Metric.MinimumScore"),
                    Required(item.Passed, "Metric.Passed"),
                    Array.AsReadOnly((diagnosticsByMetric.GetValueOrDefault(item.ID) ?? [])
                        .OrderBy(code => Required(code.Ordinal, "Diagnostic.Ordinal"))
                        .ThenBy(code => code.ID)
                        .Select(code => Required(code.Code, "Diagnostic.Code"))
                        .ToArray())))
                .ToArray()));
    #endregion

    #region 新增（InsertChildrenAsync）
    /// <summary>
    /// 新增（InsertChildrenAsync）
    /// </summary>
    /// <param name="report">评估报告。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertChildrenAsync(ModelJudgeReport report, CancellationToken cancellationToken)
    {
        await InsertEvaluatorsAsync(report);
        await InsertMinimumScoresAsync(report);
        for (int caseOrdinal = 0; caseOrdinal < report.Cases.Count; caseOrdinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ModelJudgeCaseResult value = report.Cases[caseOrdinal];
            Guid caseRowId = Guid.NewGuid();
            await Db.Insertable(new AgEvaluationModelJudgementCase
            {
                ID = caseRowId,
                JudgementId = report.Id,
                Ordinal = caseOrdinal,
                CaseId = value.CaseId,
                CaseName = value.CaseName,
                UnifiedRunId = value.UnifiedRunId,
                InputSha256 = value.InputSha256,
                OutputSha256 = value.OutputSha256,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await InsertMetricsAsync(report.Id, caseRowId, value.Metrics);
        }
    }
    #endregion

    #region 新增（InsertEvaluatorsAsync）
    /// <summary>
    /// 新增（InsertEvaluatorsAsync）
    /// </summary>
    /// <param name="report">评估报告。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertEvaluatorsAsync(ModelJudgeReport report)
    {
        if (report.Evaluators.Count == 0)
        {
            return;
        }

        await Db.Insertable(report.Evaluators.Select((value, ordinal) =>
            new AgEvaluationModelJudgementEvaluator
            {
                ID = Guid.NewGuid(),
                JudgementId = report.Id,
                Ordinal = ordinal,
                Name = value,
                IsDeleted = false,
                IsActive = true
            }).ToList()).ExecuteCommandAsync();
    }
    #endregion

    #region 新增（InsertMinimumScoresAsync）
    /// <summary>
    /// 新增（InsertMinimumScoresAsync）
    /// </summary>
    /// <param name="report">评估报告。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertMinimumScoresAsync(ModelJudgeReport report)
    {
        if (report.MinimumScores.Count == 0)
        {
            return;
        }

        await Db.Insertable(report.MinimumScores.Select((value, ordinal) =>
            new AgEvaluationModelJudgementMinimumScore
            {
                ID = Guid.NewGuid(),
                JudgementId = report.Id,
                Ordinal = ordinal,
                Name = value.Key,
                Score = value.Value,
                IsDeleted = false,
                IsActive = true
            }).ToList()).ExecuteCommandAsync();
    }
    #endregion

    #region 新增（InsertMetricsAsync）
    /// <summary>
    /// 新增（InsertMetricsAsync）
    /// </summary>
    /// <param name="judgementId">模型评判记录标识。</param>
    /// <param name="caseRowId">评估用例行标识。</param>
    /// <param name="metrics">评估指标集合。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertMetricsAsync(Guid judgementId, Guid caseRowId, IReadOnlyList<ModelJudgeMetric> metrics)
    {
        for (int metricOrdinal = 0; metricOrdinal < metrics.Count; metricOrdinal++)
        {
            ModelJudgeMetric value = metrics[metricOrdinal];
            Guid metricRowId = Guid.NewGuid();
            await Db.Insertable(new AgEvaluationModelJudgementMetric
            {
                ID = metricRowId,
                JudgementId = judgementId,
                JudgementCaseId = caseRowId,
                Ordinal = metricOrdinal,
                Name = value.Name,
                Score = value.Score,
                MinimumScore = value.MinimumScore,
                Passed = value.Passed,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            if (value.DiagnosticCodes.Count == 0)
            {
                continue;
            }

            await Db.Insertable(value.DiagnosticCodes.Select((code, ordinal) =>
                new AgEvaluationModelJudgementDiagnostic
                {
                    ID = Guid.NewGuid(),
                    JudgementId = judgementId,
                    JudgementMetricId = metricRowId,
                    Ordinal = ordinal,
                    Code = code,
                    IsDeleted = false,
                    IsActive = true
                }).ToList()).ExecuteCommandAsync();
        }
    }
    #endregion

    #region 映射（MapEntity）
    /// <summary>
    /// 映射（MapEntity）
    /// </summary>
    /// <param name="value">本次操作使用的模型裁判报告。</param>
    /// <returns>由模型裁判报告构造的报告主表实体。</returns>
    private static AgEvaluationModelJudgement MapEntity(ModelJudgeReport value) => new()
    {
        ID = value.Id,
        TenantId = value.TenantId,
        RequestedByUserId = value.RequestedBy,
        BatchId = value.BatchId,
        SuiteId = value.SuiteId,
        SuiteVersionId = value.SuiteVersionId,
        SuiteVersionContentSha256 = value.SuiteVersionContentSha256,
        Provider = value.Provider,
        PackageVersion = value.PackageVersion,
        ModelProfileId = value.ModelProfileId,
        ConfigurationSha256 = value.ConfigurationSha256,
        PromptVersion = value.PromptVersion,
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc.UtcDateTime,
        AdvisoryPassed = value.AdvisoryPassed,
        IsDeleted = false,
        IsActive = true
    };
    #endregion

    #region 转换（ToOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。</returns>
    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Evaluation model judgement field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Evaluation model judgement field '{field}' is missing.");
    #endregion
}

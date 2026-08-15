using System.Collections.ObjectModel;
using EU.Core.Agent.Application.Evaluation;

#nullable enable

namespace EU.Core.Services;

public sealed class AgEvaluationModelJudgementServices :
    BaseServices<AgEvaluationModelJudgement>,
    IAgEvaluationModelJudgementServices,
    IModelJudgeReportRepository
{
    public AgEvaluationModelJudgementServices(IBaseRepository<AgEvaluationModelJudgement> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<ModelJudgeReport?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
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

    public async Task<bool> TryCreateAsync(
        ModelJudgeReport value,
        CancellationToken cancellationToken = default)
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

    private async Task InsertChildrenAsync(
        ModelJudgeReport report,
        CancellationToken cancellationToken)
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

    private async Task InsertMetricsAsync(
        Guid judgementId,
        Guid caseRowId,
        IReadOnlyList<ModelJudgeMetric> metrics)
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

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Evaluation model judgement field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Evaluation model judgement field '{field}' is missing.");
}

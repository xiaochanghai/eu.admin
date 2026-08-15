using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

public sealed class AgEvaluationBatchServices :
    BaseServices<AgEvaluationBatch>,
    IAgEvaluationBatchServices,
    IEvaluationBatchRepository,
    IEvaluationBatchRecovery
{
    private const string EventKindObservation = "EventKind";
    private const string RouteObservation = "Route";

    public AgEvaluationBatchServices(IBaseRepository<AgEvaluationBatch> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgEvaluationBatch? batch = await Db.Queryable<AgEvaluationBatch>()
                .Where(value => value.ID == id && value.TenantId == tenantId && !value.IsDeleted)
                .FirstAsync();
            EvaluationBatchRecord? result = batch is null
                ? null
                : (await LoadBatchesAsync([batch], cancellationToken))[0];
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgEvaluationBatch> batches = await Db.Queryable<AgEvaluationBatch>()
                .Where(value =>
                    value.SuiteId == suiteId &&
                    value.TenantId == tenantId &&
                    !value.IsDeleted)
                .OrderBy(value => value.StartedAtUtc, OrderByType.Desc)
                .OrderBy(value => value.ID, OrderByType.Desc)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();
            IReadOnlyList<EvaluationBatchRecord> result = await LoadBatchesAsync(
                batches,
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
        EvaluationBatchRecord value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            if (await Db.Queryable<AgEvaluationBatch>()
                .Where(candidate => candidate.ID == value.Id)
                .AnyAsync())
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapBatchEntity(value)).ExecuteCommandAsync();
            await InsertCasesAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TryReplaceAsync(
        EvaluationBatchRecord value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgEvaluationBatch entity = MapBatchEntity(value);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(candidate => new
                {
                    candidate.Status,
                    candidate.LogicalRevision,
                    candidate.FinishedAtUtc,
                    candidate.ErrorCode
                })
                .Where(candidate =>
                    candidate.ID == value.Id &&
                    candidate.TenantId == value.TenantId &&
                    candidate.SuiteId == value.SuiteId &&
                    candidate.SuiteVersionId == value.SuiteVersionId &&
                    candidate.Status == EvaluationBatchStatus.Running.ToString() &&
                    candidate.LogicalRevision == expectedLogicalRevision &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await DeleteCasesAsync(value.Id);
            await InsertCasesAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgEvaluationBatch> runningEntities = await Db.Queryable<AgEvaluationBatch>()
            .Where(value =>
                value.Status == EvaluationBatchStatus.Running.ToString() &&
                !value.IsDeleted)
            .OrderBy(value => value.StartedAtUtc)
            .OrderBy(value => value.ID)
            .ToListAsync();
        IReadOnlyList<EvaluationBatchRecord> running = await LoadBatchesAsync(
            runningEntities,
            cancellationToken);
        int recovered = 0;
        foreach (EvaluationBatchRecord value in running)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EvaluationCaseExecutionRecord[] cases = value.Cases.Select(item =>
                item.Status == EvaluationCaseExecutionStatus.Running
                    ? item with
                    {
                        Status = EvaluationCaseExecutionStatus.Failed,
                        ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
                    }
                    : item with { }).ToArray();
            EvaluationBatchRecord updated = value with
            {
                Status = EvaluationBatchStatus.Failed,
                LogicalRevision = value.LogicalRevision + 1,
                FinishedAtUtc = recoveredAtUtc.ToUniversalTime(),
                Cases = EvaluationBatchContractCloner.CloneCases(cases),
                ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
            };
            if (await TryReplaceAsync(updated, value.LogicalRevision, cancellationToken))
            {
                recovered++;
            }
        }

        return recovered;
    }

    private async Task<IReadOnlyList<EvaluationBatchRecord>> LoadBatchesAsync(
        IReadOnlyList<AgEvaluationBatch> batches,
        CancellationToken cancellationToken)
    {
        if (batches.Count == 0)
        {
            return [];
        }

        Guid[] batchIds = batches.Select(value => value.ID).ToArray();
        List<AgEvaluationBatchCase> cases = await Db.Queryable<AgEvaluationBatchCase>()
            .Where(value =>
                value.BatchId.HasValue &&
                batchIds.Contains(value.BatchId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.BatchId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        Guid[] caseRowIds = cases.Select(value => value.ID).ToArray();
        List<AgEvaluationBatchCheck> checks = caseRowIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationBatchCheck>()
                .Where(value =>
                    value.BatchCaseId.HasValue &&
                    caseRowIds.Contains(value.BatchCaseId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.BatchCaseId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        List<AgEvaluationBatchObservation> observations = caseRowIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationBatchObservation>()
                .Where(value =>
                    value.BatchCaseId.HasValue &&
                    caseRowIds.Contains(value.BatchCaseId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.BatchCaseId)
                .OrderBy(value => value.ObservationType)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgEvaluationBatchCheck[]> checksByCase = checks
            .GroupBy(value => Required(value.BatchCaseId, "Check.BatchCaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationBatchObservation[]> observationsByCase = observations
            .GroupBy(value => Required(value.BatchCaseId, "Observation.BatchCaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationBatchCase[]> casesByBatch = cases
            .GroupBy(value => Required(value.BatchId, "Case.BatchId"))
            .ToDictionary(group => group.Key, group => group.ToArray());

        return EvaluationBatchContractCloner.ReadOnly(batches.Select(batch => MapBatch(
            batch,
            casesByBatch.GetValueOrDefault(batch.ID) ?? [],
            checksByCase,
            observationsByCase)));
    }

    private static EvaluationBatchRecord MapBatch(
        AgEvaluationBatch value,
        IReadOnlyList<AgEvaluationBatchCase> cases,
        IReadOnlyDictionary<Guid, AgEvaluationBatchCheck[]> checksByCase,
        IReadOnlyDictionary<Guid, AgEvaluationBatchObservation[]> observationsByCase) =>
        new(
            value.ID,
            Required(value.TenantId, "TenantId"),
            Required(value.RequestedByUserId, "RequestedByUserId"),
            Required(value.SuiteId, "SuiteId"),
            Required(value.SuiteVersionId, "SuiteVersionId"),
            Required(value.SuiteVersionContentSha256, "SuiteVersionContentSha256"),
            ParseEnum<EvaluationBatchStatus>(value.Status, "Status"),
            Required(value.LogicalRevision, "LogicalRevision"),
            ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")),
            value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
            Array.AsReadOnly(cases
                .OrderBy(item => Required(item.Ordinal, "Case.Ordinal"))
                .ThenBy(item => item.ID)
                .Select(item => MapCase(
                    item,
                    checksByCase.GetValueOrDefault(item.ID) ?? [],
                    observationsByCase.GetValueOrDefault(item.ID) ?? []))
                .ToArray()),
            Required(value.ErrorCode, "ErrorCode"));

    private static EvaluationCaseExecutionRecord MapCase(
        AgEvaluationBatchCase value,
        IReadOnlyList<AgEvaluationBatchCheck> checks,
        IReadOnlyList<AgEvaluationBatchObservation> observations)
    {
        RunEvaluationReport? report = value.ReportEvaluatedAtUtc.HasValue
            ? new RunEvaluationReport(
                Required(value.UnifiedRunId, "Case.UnifiedRunId"),
                ToOffset(value.ReportEvaluatedAtUtc.Value),
                Required(value.ReportPassed, "Case.ReportPassed"),
                Required(value.ReportScore, "Case.ReportScore"),
                Required(value.OutputSha256, "Case.OutputSha256"),
                Required(value.OutputUtf8Bytes, "Case.OutputUtf8Bytes"),
                Array.AsReadOnly(checks
                    .OrderBy(item => Required(item.Ordinal, "Check.Ordinal"))
                    .ThenBy(item => item.ID)
                    .Select(item => new RunEvaluationCheck(
                        Required(item.Code, "Check.Code"),
                        Required(item.Passed, "Check.Passed"),
                        Required(item.Expected, "Check.Expected"),
                        Required(item.Actual, "Check.Actual")))
                    .ToArray()))
            : null;
        string[] ObservationValues(string type) => observations
            .Where(item => string.Equals(item.ObservationType, type, StringComparison.Ordinal))
            .OrderBy(item => Required(item.Ordinal, "Observation.Ordinal"))
            .ThenBy(item => item.ID)
            .Select(item => Required(item.Value, "Observation.Value"))
            .ToArray();

        return new EvaluationCaseExecutionRecord(
            Required(value.CaseId, "Case.CaseId"),
            Required(value.CaseName, "Case.CaseName"),
            Required(value.TargetAgentId, "Case.TargetAgentId"),
            Required(value.TargetAgentVersionId, "Case.TargetAgentVersionId"),
            ParseEnum<EvaluationCaseExecutionStatus>(value.Status, "Case.Status"),
            value.UnifiedRunId,
            ParseNullableEnum<UnifiedRunStatus>(value.UnifiedRunStatus, "Case.UnifiedRunStatus"),
            report,
            Required(value.ErrorCode, "Case.ErrorCode"))
        {
            DurationMilliseconds = value.DurationMilliseconds,
            ToolCallCount = Required(value.ToolCallCount, "Case.ToolCallCount"),
            ObservedEventKinds = ObservationValues(EventKindObservation),
            ObservedRoutes = ObservationValues(RouteObservation)
        };
    }

    private async Task InsertCasesAsync(
        EvaluationBatchRecord batch,
        CancellationToken cancellationToken)
    {
        for (int ordinal = 0; ordinal < batch.Cases.Count; ordinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EvaluationCaseExecutionRecord value = batch.Cases[ordinal];
            Guid rowId = Guid.NewGuid();
            await Db.Insertable(new AgEvaluationBatchCase
            {
                ID = rowId,
                BatchId = batch.Id,
                Ordinal = ordinal,
                CaseId = value.CaseId,
                CaseName = value.CaseName,
                TargetAgentId = value.TargetAgentId,
                TargetAgentVersionId = value.TargetAgentVersionId,
                Status = value.Status.ToString(),
                UnifiedRunId = value.UnifiedRunId,
                UnifiedRunStatus = value.UnifiedRunStatus?.ToString() ?? string.Empty,
                ErrorCode = value.ErrorCode,
                DurationMilliseconds = value.DurationMilliseconds,
                ToolCallCount = value.ToolCallCount,
                ReportEvaluatedAtUtc = value.Report?.EvaluatedAtUtc.UtcDateTime,
                ReportPassed = value.Report?.Passed,
                ReportScore = value.Report?.Score,
                OutputSha256 = value.Report?.OutputSha256 ?? string.Empty,
                OutputUtf8Bytes = value.Report?.OutputUtf8Bytes,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            if (value.Report is not null)
            {
                await InsertChecksAsync(batch.Id, rowId, value.Report.Checks);
            }
            await InsertObservationsAsync(
                batch.Id,
                rowId,
                EventKindObservation,
                value.ObservedEventKinds ?? []);
            await InsertObservationsAsync(
                batch.Id,
                rowId,
                RouteObservation,
                value.ObservedRoutes ?? []);
        }
    }

    private async Task InsertChecksAsync(
        Guid batchId,
        Guid caseRowId,
        IReadOnlyList<RunEvaluationCheck> checks)
    {
        if (checks.Count == 0)
        {
            return;
        }

        await Db.Insertable(checks.Select((value, ordinal) => new AgEvaluationBatchCheck
        {
            ID = Guid.NewGuid(),
            BatchId = batchId,
            BatchCaseId = caseRowId,
            Ordinal = ordinal,
            Code = value.Code,
            Passed = value.Passed,
            Expected = value.Expected,
            Actual = value.Actual,
            IsDeleted = false,
            IsActive = true
        }).ToList()).ExecuteCommandAsync();
    }

    private async Task InsertObservationsAsync(
        Guid batchId,
        Guid caseRowId,
        string type,
        IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        await Db.Insertable(values.Select((value, ordinal) => new AgEvaluationBatchObservation
        {
            ID = Guid.NewGuid(),
            BatchId = batchId,
            BatchCaseId = caseRowId,
            ObservationType = type,
            Ordinal = ordinal,
            Value = value,
            IsDeleted = false,
            IsActive = true
        }).ToList()).ExecuteCommandAsync();
    }

    private async Task DeleteCasesAsync(Guid batchId)
    {
        Guid[] caseIds = await Db.Queryable<AgEvaluationBatchCase>()
            .Where(value => value.BatchId == batchId)
            .Select(value => value.ID)
            .ToArrayAsync();
        if (caseIds.Length > 0)
        {
            await Db.Deleteable<AgEvaluationBatchCheck>()
                .Where(value =>
                    value.BatchCaseId.HasValue &&
                    caseIds.Contains(value.BatchCaseId.Value))
                .ExecuteCommandAsync();
            await Db.Deleteable<AgEvaluationBatchObservation>()
                .Where(value =>
                    value.BatchCaseId.HasValue &&
                    caseIds.Contains(value.BatchCaseId.Value))
                .ExecuteCommandAsync();
        }
        await Db.Deleteable<AgEvaluationBatchCase>()
            .Where(value => value.BatchId == batchId)
            .ExecuteCommandAsync();
    }

    private static AgEvaluationBatch MapBatchEntity(EvaluationBatchRecord value) => new()
    {
        ID = value.Id,
        TenantId = value.TenantId,
        RequestedByUserId = value.RequestedBy,
        SuiteId = value.SuiteId,
        SuiteVersionId = value.SuiteVersionId,
        SuiteVersionContentSha256 = value.SuiteVersionContentSha256,
        Status = value.Status.ToString(),
        LogicalRevision = value.LogicalRevision,
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out TEnum result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException(
                $"Evaluation batch field '{field}' contains unsupported value '{value}'.");

    private static TEnum? ParseNullableEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum =>
        string.IsNullOrEmpty(value) ? null : ParseEnum<TEnum>(value, field);

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Evaluation batch field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Evaluation batch field '{field}' is missing.");
}

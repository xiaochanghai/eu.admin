using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgEvaluationBatchServices 职责实现

/// <summary>
/// 提供评测批次记录的持久化服务。
/// </summary>
public sealed class AgEvaluationBatchServices :
    BaseServices<AgEvaluationBatch>,
    IAgEvaluationBatchServices,
    IEvaluationBatchRepository,
    IEvaluationBatchRecovery
{
    private const string EventKindObservation = "EventKind";
    private const string RouteObservation = "Route";

    #region 构造（AgEvaluationBatchServices）
    /// <summary>
    /// 构造（AgEvaluationBatchServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgEvaluationBatchServices(IBaseRepository<AgEvaluationBatch> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">评测批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下包含用例明细的评测批次；不存在时为 null。</returns>
    public async Task<EvaluationBatchRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
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
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和套件下的评测批次，按开始时间及标识倒序排列，最多 100 条。</returns>
    public async Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(Guid suiteId, string tenantId, int take, CancellationToken cancellationToken = default)
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
    #endregion

    #region 创建评测批次及用例（TryCreateAsync）
    /// <summary>
    /// 创建评测批次及用例（TryCreateAsync）。
    /// </summary>
    /// <param name="value">待创建的评测批次，包含初始运行状态及用例记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>批次及用例持久化成功时返回 true；同一批次标识已存在时返回 false。</returns>
    public async Task<bool> TryCreateAsync(EvaluationBatchRecord value, CancellationToken cancellationToken = default)
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
    #endregion

    #region 按修订号替换运行中的评测批次及用例（TryReplaceAsync）
    /// <summary>
    /// 按修订号替换运行中的评测批次及用例（TryReplaceAsync）。
    /// </summary>
    /// <param name="value">替换后的批次及完整用例集合，LogicalRevision 必须为预期修订号加一。</param>
    /// <param name="expectedLogicalRevision">数据库当前应具有的逻辑修订号，不允许为 long.MaxValue。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>批次及用例更新成功时返回 true；修订号递增不合法，或匹配标识、租户、套件及版本的未删除运行中记录未能按预期修订号更新时返回 false。</returns>
    public async Task<bool> TryReplaceAsync(EvaluationBatchRecord value, long expectedLogicalRevision, CancellationToken cancellationToken = default)
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
    #endregion

    #region 恢复（RecoverInterruptedAsync）
    /// <summary>
    /// 恢复（RecoverInterruptedAsync）
    /// </summary>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>通过乐观并发更新成功标记为宿主中断失败的批次数量。</returns>
    public async Task<int> RecoverInterruptedAsync(DateTimeOffset recoveredAtUtc, CancellationToken cancellationToken = default)
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
    #endregion

    #region 加载（LoadBatchesAsync）
    /// <summary>
    /// 加载（LoadBatchesAsync）
    /// </summary>
    /// <param name="batches">评估批次集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保持输入顺序并补齐用例、检查项和观测值的评测批次集合。</returns>
    private async Task<IReadOnlyList<EvaluationBatchRecord>> LoadBatchesAsync(IReadOnlyList<AgEvaluationBatch> batches, CancellationToken cancellationToken)
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
    #endregion

    #region 映射（MapBatch）
    /// <summary>
    /// 映射（MapBatch）
    /// </summary>
    /// <param name="value">本次操作使用的评测批次实体。</param>
    /// <param name="cases">评估用例集合。</param>
    /// <param name="checksByCase">按用例分组的评估检查项。</param>
    /// <param name="observationsByCase">按用例分组的评估观测。</param>
    /// <returns>包含按序排列的用例执行结果的评测批次记录。</returns>
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
    #endregion

    #region 映射（MapCase）
    /// <summary>
    /// 映射（MapCase）
    /// </summary>
    /// <param name="value">本次操作使用的评测批次用例实体。</param>
    /// <param name="checks">评估检查项集合。</param>
    /// <param name="observations">评估观测集合。</param>
    /// <returns>包含可选规则评测报告、耗时及工具调用观测值的用例执行记录。</returns>
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
        #region 处理（ObservationValues）
        string[] ObservationValues(string type) => observations
            .Where(item => string.Equals(item.ObservationType, type, StringComparison.Ordinal))
            .OrderBy(item => Required(item.Ordinal, "Observation.Ordinal"))
            .ThenBy(item => item.ID)
            .Select(item => Required(item.Value, "Observation.Value"))
            .ToArray();
        #endregion

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
    #endregion

    #region 新增（InsertCasesAsync）
    /// <summary>
    /// 新增（InsertCasesAsync）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertCasesAsync(EvaluationBatchRecord batch, CancellationToken cancellationToken)
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
    #endregion

    #region 新增（InsertChecksAsync）
    /// <summary>
    /// 新增（InsertChecksAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="caseRowId">评估用例行标识。</param>
    /// <param name="checks">评估检查项集合。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertChecksAsync(Guid batchId, Guid caseRowId, IReadOnlyList<RunEvaluationCheck> checks)
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
    #endregion

    #region 新增（InsertObservationsAsync）
    /// <summary>
    /// 新增（InsertObservationsAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <param name="caseRowId">评估用例行标识。</param>
    /// <param name="type">目标类型。</param>
    /// <param name="values">指定观测类型下需要按序持久化的观测值。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertObservationsAsync(Guid batchId, Guid caseRowId, string type, IReadOnlyList<string> values)
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
    #endregion

    #region 删除（DeleteCasesAsync）
    /// <summary>
    /// 删除（DeleteCasesAsync）
    /// </summary>
    /// <param name="batchId">评估批次标识。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 映射（MapBatchEntity）
    /// <summary>
    /// 映射（MapBatchEntity）
    /// </summary>
    /// <param name="value">本次操作使用的评测批次记录。</param>
    /// <returns>由评测批次记录构造的持久化实体。</returns>
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
    #endregion

    #region 解析（ParseEnum）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseEnum）。
    /// </summary>
    /// <typeparam name="TEnum">目标枚举类型。</typeparam>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out TEnum result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException(
                $"Evaluation batch field '{field}' contains unsupported value '{value}'.");
    #endregion

    #region 解析（ParseNullableEnum）
    /// <summary>
    /// 解析（ParseNullableEnum）
    /// </summary>
    /// <typeparam name="TEnum">待处理数据的泛型类型。</typeparam>
    /// <param name="value">数据库中存储的可空枚举文本。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>解析后的已定义枚举值；输入为 null 或空字符串时返回 null，无效值抛出 InvalidDataException。</returns>
    private static TEnum? ParseNullableEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum =>
        string.IsNullOrEmpty(value) ? null : ParseEnum<TEnum>(value, field);
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
        value ?? throw new InvalidDataException($"Evaluation batch field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Evaluation batch field '{field}' is missing.");
    #endregion
}

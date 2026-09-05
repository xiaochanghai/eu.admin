using System.Text.Json;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.IServices;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

// 文件职责：EvaluationBatchService 职责实现

/// <summary>
/// 负责创建、执行和恢复评测批次。
/// </summary>
/// <param name="suites">用于管理评测套件及用例的服务。</param>
/// <param name="batches">用于读取和持久化评测批次的仓储。</param>
/// <param name="unifiedEntry">用于准备和执行统一入口运行的服务。</param>
/// <param name="unifiedRuns">用于读取和持久化统一入口会话、运行及事件的仓储。</param>
/// <param name="evaluator">用于按评测规范检查运行结果的服务。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器；为 null 时使用系统时间提供器。</param>
public sealed class EvaluationBatchService(
    IAgEvaluationSuiteServices suites,
    IEvaluationBatchRepository batches,
    UnifiedEntryService unifiedEntry,
    IUnifiedEntryRepository unifiedRuns,
    IRunEvaluationService evaluator,
    TimeProvider? timeProvider = null) : BaseServices, IAgEvaluationBatchExecutionServices
{
    /// <summary>单个评测批次允许的最大用例数量。</summary>
    public const int MaximumCasesPerBatch = 20;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">评测批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下的评测批次及用例结果；不存在时为 null。</returns>
    public Task<EvaluationBatchRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
        batches.GetAsync(id, tenantId, cancellationToken);
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和套件下的最近评测批次，最多 100 条。</returns>
    public Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(Guid suiteId, string tenantId, int take, CancellationToken cancellationToken = default) =>
        batches.ListAsync(suiteId, tenantId, Math.Clamp(take, 1, 100), cancellationToken);
    #endregion

    #region 运行（RunAsync）
    /// <summary>
    /// 运行（RunAsync）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="suiteVersionId">评估套件版本标识。</param>
    /// <param name="identity">当前操作使用的执行身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测批次记录，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<EvaluationBatchRecord>> RunAsync(
        Guid suiteId,
        Guid suiteVersionId,
        AgentExecutionIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        EvaluationSuiteDefinition? suite = await suites.GetAsync(
            suiteId, identity.TenantId, cancellationToken);
        if (suite is null)
        {
            return Failure(EvaluationBatchErrorCodes.SuiteNotFound, "The evaluation suite was not found.");
        }

        if (suite.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(
                EvaluationBatchErrorCodes.RequestInvalid,
                "An archived evaluation suite must be restored before a new batch can run.");
        }

        PublishedEvaluationSuiteVersion? version = suite.PublishedVersions
            .SingleOrDefault(value => value.Id == suiteVersionId);
        if (version is null)
        {
            return Failure(EvaluationBatchErrorCodes.VersionNotFound, "The evaluation suite version was not found.");
        }

        if (version.Cases.Count is < 1 or > MaximumCasesPerBatch)
        {
            return Failure(
                EvaluationBatchErrorCodes.CaseLimitExceeded,
                $"A batch must contain from 1 through {MaximumCasesPerBatch} Cases.");
        }

        foreach ((Guid agentId, Guid versionId) in version.Cases
            .Select(value => (value.TargetAgentId, value.TargetAgentVersionId))
            .Distinct())
        {
            if (!await suites.IsPublishedAsync(agentId, versionId, cancellationToken))
            {
                return Failure(
                    EvaluationBatchErrorCodes.TargetUnavailable,
                    "An evaluation target is no longer published.");
            }
        }

        DateTimeOffset started = _timeProvider.GetUtcNow().ToUniversalTime();
        var batch = new EvaluationBatchRecord(
            Guid.NewGuid(),
            identity.TenantId,
            identity.UserId,
            suite.Id,
            version.Id,
            version.ContentSha256,
            EvaluationBatchStatus.Running,
            0,
            started,
            null,
            ReadOnlyCases(version.Cases.Select(value => new EvaluationCaseExecutionRecord(
                value.Id,
                value.Name,
                value.TargetAgentId,
                value.TargetAgentVersionId,
                EvaluationCaseExecutionStatus.Pending,
                null,
                null,
                null,
                string.Empty))),
            string.Empty);
        if (!await batches.TryCreateAsync(batch, cancellationToken))
        {
            return Failure(EvaluationBatchErrorCodes.PersistenceConflict, "The evaluation batch could not be created.");
        }

        try
        {
            for (int index = 0; index < version.Cases.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EvaluationCaseDefinition testCase = version.Cases[index];
                batch = await ReplaceCaseAsync(
                    batch,
                    index,
                    batch.Cases[index] with { Status = EvaluationCaseExecutionStatus.Running },
                    cancellationToken);

                EvaluationCaseExecutionRecord completed = await ExecuteCaseAsync(
                    testCase, identity, cancellationToken);
                batch = await ReplaceCaseAsync(batch, index, completed, cancellationToken);
            }

            batch = await FinishAsync(
                batch,
                EvaluationBatchStatus.Completed,
                string.Empty,
                cancellationToken);
            return Success(batch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            batch = MarkCurrentCancelled(batch);
            await TryFinishAsync(
                batch,
                EvaluationBatchStatus.Cancelled,
                EvaluationBatchErrorCodes.Cancelled);
            throw;
        }
        catch (EvaluationBatchPersistenceException)
        {
            return Failure(
                EvaluationBatchErrorCodes.PersistenceConflict,
                "The evaluation batch changed while it was running.");
        }
        catch
        {
            batch = MarkCurrentFailed(
                batch,
                EvaluationBatchErrorCodes.ExecutionFailed);
            await TryFinishAsync(
                batch,
                EvaluationBatchStatus.Failed,
                EvaluationBatchErrorCodes.ExecutionFailed);
            return Failure(
                EvaluationBatchErrorCodes.ExecutionFailed,
                "The evaluation batch could not be completed.");
        }
    }
    #endregion

    #region 执行（ExecuteCaseAsync）
    /// <summary>
    /// 执行（ExecuteCaseAsync）
    /// </summary>
    /// <param name="testCase">评估用例。</param>
    /// <param name="identity">当前操作使用的执行身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>固定 Agent 版本执行后的用例评测结果及观测信息；准备或执行失败时携带失败状态和错误码。</returns>
    private async Task<EvaluationCaseExecutionRecord> ExecuteCaseAsync(
        EvaluationCaseDefinition testCase,
        AgentExecutionIdentity identity,
        CancellationToken cancellationToken)
    {
        UnifiedEntryPreparationResult prepared = await unifiedEntry.PrepareEvaluationAsync(
            testCase.Input,
            testCase.TargetAgentId,
            testCase.TargetAgentVersionId,
            identity,
            cancellationToken);
        if (!prepared.Succeeded)
        {
            return Failed(testCase, null, null, prepared.Error!.Code);
        }

        UnifiedEntryContext context = prepared.Context!;
        await foreach (UnifiedRunEvent _ in unifiedEntry
            .StreamAsync(context, cancellationToken)
            .WithCancellation(cancellationToken))
        {
        }

        UnifiedEntryRunRecord? run = await unifiedRuns.GetRunForOwnerAsync(
            context.RunId,
            identity.TenantId,
            identity.UserId,
            cancellationToken);
        if (run is null)
        {
            return Failed(
                testCase,
                context.RunId,
                null,
                EvaluationBatchErrorCodes.ExecutionFailed);
        }

        RunEvaluationReport? report = await evaluator.EvaluateAsync(
            context.RunId,
            identity.TenantId,
            identity.UserId,
            testCase.Specification,
            cancellationToken);
        if (report is null)
        {
            return Failed(
                testCase,
                context.RunId,
                run.Status,
                EvaluationBatchErrorCodes.ExecutionFailed);
        }

        UnifiedRunDetails? details = await unifiedRuns.GetDetailsForOwnerAsync(
            context.RunId,
            identity.TenantId,
            identity.UserId,
            cancellationToken);
        IReadOnlyList<UnifiedRunEventRecord> events =
            await unifiedRuns.ListEventsForOwnerAsync(
                context.RunId,
                identity.TenantId,
                identity.UserId,
                cancellationToken);

        return new EvaluationCaseExecutionRecord(
            testCase.Id,
            testCase.Name,
            testCase.TargetAgentId,
            testCase.TargetAgentVersionId,
            report.Passed
                ? EvaluationCaseExecutionStatus.Passed
                : EvaluationCaseExecutionStatus.Failed,
            context.RunId,
            run.Status,
            report,
            report.Passed
                ? string.Empty
                : string.IsNullOrWhiteSpace(run.ErrorCode)
                    ? EvaluationBatchErrorCodes.AssertionFailed
                    : run.ErrorCode)
        {
            DurationMilliseconds = run.Duration.HasValue
                ? checked((long)Math.Ceiling(run.Duration.Value.TotalMilliseconds))
                : null,
            ToolCallCount = details?.ToolCalls.Count ?? 0,
            ObservedEventKinds = events
                .Select(value => value.Kind)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Take(64)
                .ToArray(),
            ObservedRoutes = events
                .Where(value => string.Equals(
                    value.Kind, "route-selected", StringComparison.Ordinal))
                .Select(value => ReadRoute(value.PayloadJson))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Take(16)
                .ToArray()
        };
    }
    #endregion

    #region 处理（ReplaceCaseAsync）
    /// <summary>
    /// 处理（ReplaceCaseAsync）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <param name="index">当前元素索引。</param>
    /// <param name="value">本次操作使用的评测用例执行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>替换指定用例并递增逻辑版本后的批次；乐观并发更新失败时抛出持久化异常。</returns>
    private async Task<EvaluationBatchRecord> ReplaceCaseAsync(
        EvaluationBatchRecord batch,
        int index,
        EvaluationCaseExecutionRecord value,
        CancellationToken cancellationToken)
    {
        EvaluationCaseExecutionRecord[] cases = batch.Cases.ToArray();
        cases[index] = value;
        EvaluationBatchRecord updated = batch with
        {
            LogicalRevision = batch.LogicalRevision + 1,
            Cases = EvaluationBatchContractCloner.CloneCases(cases)
        };
        if (!await batches.TryReplaceAsync(updated, batch.LogicalRevision, cancellationToken))
        {
            throw new EvaluationBatchPersistenceException();
        }

        return updated;
    }
    #endregion

    #region 处理（FinishAsync）
    /// <summary>
    /// 处理（FinishAsync）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>设置指定终态、完成时间和错误码并递增逻辑版本后的批次；更新冲突时抛出持久化异常。</returns>
    private async Task<EvaluationBatchRecord> FinishAsync(
        EvaluationBatchRecord batch,
        EvaluationBatchStatus status,
        string errorCode,
        CancellationToken cancellationToken)
    {
        EvaluationBatchRecord updated = batch with
        {
            Status = status,
            LogicalRevision = batch.LogicalRevision + 1,
            FinishedAtUtc = _timeProvider.GetUtcNow().ToUniversalTime(),
            ErrorCode = errorCode
        };
        if (!await batches.TryReplaceAsync(updated, batch.LogicalRevision, cancellationToken))
        {
            throw new EvaluationBatchPersistenceException();
        }

        return updated;
    }
    #endregion

    #region 尝试执行（TryFinishAsync）
    /// <summary>
    /// 尝试执行（TryFinishAsync）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task TryFinishAsync(EvaluationBatchRecord batch, EvaluationBatchStatus status, string errorCode)
    {
        try
        {
            await FinishAsync(batch, status, errorCode, CancellationToken.None);
        }
        catch
        {
            // The durable Running record remains evidence of an ambiguous host outcome.
        }
    }
    #endregion

    #region 处理（MarkCurrentCancelled）
    /// <summary>
    /// 处理（MarkCurrentCancelled）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <returns>将首个运行中用例标记为取消的批次副本；不执行持久化。</returns>
    private static EvaluationBatchRecord MarkCurrentCancelled(EvaluationBatchRecord batch)
        => MarkCurrent(
            batch,
            EvaluationCaseExecutionStatus.Cancelled,
            EvaluationBatchErrorCodes.Cancelled);
    #endregion

    #region 处理（MarkCurrentFailed）
    /// <summary>
    /// 处理（MarkCurrentFailed）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>将首个运行中用例标记为失败的批次副本；不执行持久化。</returns>
    private static EvaluationBatchRecord MarkCurrentFailed(EvaluationBatchRecord batch, string errorCode) =>
        MarkCurrent(batch, EvaluationCaseExecutionStatus.Failed, errorCode);
    #endregion

    #region 处理（MarkCurrent）
    /// <summary>
    /// 处理（MarkCurrent）
    /// </summary>
    /// <param name="batch">评估批次。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>将首个运行中用例更新为指定状态的批次副本；没有运行中用例时保留原用例状态。</returns>
    private static EvaluationBatchRecord MarkCurrent(EvaluationBatchRecord batch, EvaluationCaseExecutionStatus status, string errorCode)
    {
        EvaluationCaseExecutionRecord[] cases = batch.Cases.ToArray();
        int current = Array.FindIndex(
            cases,
            value => value.Status == EvaluationCaseExecutionStatus.Running);
        if (current >= 0)
        {
            cases[current] = cases[current] with
            {
                Status = status,
                ErrorCode = errorCode
            };
        }

        return batch with { Cases = EvaluationBatchContractCloner.CloneCases(cases) };
    }
    #endregion

    #region 处理（Failed）
    /// <summary>
    /// 处理（Failed）
    /// </summary>
    /// <param name="value">本次操作使用的评测用例定义。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="runStatus">运行状态。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>包含目标版本、可选运行标识及错误码的失败用例记录，不包含评测报告。</returns>
    private static EvaluationCaseExecutionRecord Failed(EvaluationCaseDefinition value, Guid? runId, UnifiedRunStatus? runStatus, string errorCode) =>
        new(
            value.Id,
            value.Name,
            value.TargetAgentId,
            value.TargetAgentVersionId,
            EvaluationCaseExecutionStatus.Failed,
            runId,
            runStatus,
            null,
            errorCode);
    #endregion

    #region 读取（ReadRoute）
    /// <summary>
    /// 读取（ReadRoute）
    /// </summary>
    /// <param name="payloadJson">载荷的 JSON 文本。</param>
    /// <returns>事件载荷中最多 64 字符的 route 文本；字段缺失、类型不符或 JSON 解析失败时返回空字符串。</returns>
    private static string ReadRoute(string payloadJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty("route", out JsonElement route)
                || route.ValueKind != JsonValueKind.String)
            {
                return string.Empty;
            }

            string value = route.GetString() ?? string.Empty;
            return value[..Math.Min(value.Length, 64)];
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
    #endregion

    #region 读取（ReadOnlyCases）
    /// <summary>
    /// 读取（ReadOnlyCases）
    /// </summary>
    /// <param name="values">需要复制观测数据及评测报告的用例执行记录集合。</param>
    /// <returns>复制用例及其嵌套评测数据后的只读执行记录集合。</returns>
    private static IReadOnlyList<EvaluationCaseExecutionRecord> ReadOnlyCases(IEnumerable<EvaluationCaseExecutionRecord> values) =>
        EvaluationBatchContractCloner.CloneCases(values);
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<EvaluationBatchRecord> Failure(string code, string message) =>
        ServiceResult<EvaluationBatchRecord>.Failure(
            EvaluationBatchServiceStatusCodes.FromErrorCode(code),
            message);
    #endregion

    private sealed class EvaluationBatchPersistenceException : Exception;
}

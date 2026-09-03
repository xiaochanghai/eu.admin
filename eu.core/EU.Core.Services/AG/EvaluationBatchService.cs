using System.Text.Json;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.IServices;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

public sealed class EvaluationBatchService(
    IAgEvaluationSuiteServices suites,
    IEvaluationBatchRepository batches,
    IEvaluationTargetCatalog targets,
    UnifiedEntryService unifiedEntry,
    IUnifiedEntryRepository unifiedRuns,
    RunEvaluationService evaluator,
    TimeProvider? timeProvider = null) : BaseServices, IAgEvaluationBatchExecutionServices
{
    public const int MaximumCasesPerBatch = 20;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default) =>
        batches.GetAsync(id, tenantId, cancellationToken);

    public Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default) =>
        batches.ListAsync(suiteId, tenantId, Math.Clamp(take, 1, 100), cancellationToken);

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
            if (!await targets.IsPublishedAsync(agentId, versionId, cancellationToken))
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

    private async Task TryFinishAsync(
        EvaluationBatchRecord batch,
        EvaluationBatchStatus status,
        string errorCode)
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

    private static EvaluationBatchRecord MarkCurrentCancelled(EvaluationBatchRecord batch)
        => MarkCurrent(
            batch,
            EvaluationCaseExecutionStatus.Cancelled,
            EvaluationBatchErrorCodes.Cancelled);

    private static EvaluationBatchRecord MarkCurrentFailed(
        EvaluationBatchRecord batch,
        string errorCode) =>
        MarkCurrent(batch, EvaluationCaseExecutionStatus.Failed, errorCode);

    private static EvaluationBatchRecord MarkCurrent(
        EvaluationBatchRecord batch,
        EvaluationCaseExecutionStatus status,
        string errorCode)
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

    private static EvaluationCaseExecutionRecord Failed(
        EvaluationCaseDefinition value,
        Guid? runId,
        UnifiedRunStatus? runStatus,
        string errorCode) =>
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

    private static IReadOnlyList<EvaluationCaseExecutionRecord> ReadOnlyCases(
        IEnumerable<EvaluationCaseExecutionRecord> values) =>
        EvaluationBatchContractCloner.CloneCases(values);

    private static ServiceResult<EvaluationBatchRecord> Failure(string code, string message) =>
        ServiceResult<EvaluationBatchRecord>.Failure(
            EvaluationBatchServiceStatusCodes.FromErrorCode(code),
            message);

    private sealed class EvaluationBatchPersistenceException : Exception;
}

using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgEvaluationPersistence_Should
{
    [Fact]
    public async Task Persist_complete_and_recover_evaluation_batches()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgEvaluationBatch),
            typeof(AgEvaluationBatchCase),
            typeof(AgEvaluationBatchCheck),
            typeof(AgEvaluationBatchObservation));
        var service = new AgEvaluationBatchServices(
            fixture.CreateRepository<AgEvaluationBatch>());
        DateTimeOffset startedAt = DateTimeOffset.Parse("2026-08-16T11:00:00Z");
        Guid suiteId = Guid.NewGuid();
        EvaluationBatchRecord running = CreateRunningBatch(suiteId, startedAt);

        Assert.True(await service.TryCreateAsync(running));
        Assert.False(await service.TryCreateAsync(running));
        Assert.Null(await service.GetAsync(running.Id, "tenant-b"));

        EvaluationCaseExecutionRecord runningCase = Assert.Single(running.Cases);
        Guid unifiedRunId = Guid.NewGuid();
        var report = new RunEvaluationReport(
            unifiedRunId,
            startedAt.AddSeconds(2),
            true,
            1m,
            Hash('e'),
            12,
            [new RunEvaluationCheck("status", true, "Completed", "Completed")]);
        EvaluationBatchRecord completed = running with
        {
            Status = EvaluationBatchStatus.Completed,
            LogicalRevision = 1,
            FinishedAtUtc = startedAt.AddSeconds(3),
            Cases =
            [
                runningCase with
                {
                    Status = EvaluationCaseExecutionStatus.Passed,
                    UnifiedRunId = unifiedRunId,
                    UnifiedRunStatus = UnifiedRunStatus.Completed,
                    Report = report,
                    DurationMilliseconds = 123,
                    ToolCallCount = 1,
                    ObservedEventKinds = ["message", "completed"],
                    ObservedRoutes = ["direct"]
                }
            ]
        };

        Assert.True(await service.TryReplaceAsync(completed, 0));
        Assert.False(await service.TryReplaceAsync(completed, 0));
        EvaluationBatchRecord persisted = Assert.IsType<EvaluationBatchRecord>(
            await service.GetAsync(completed.Id, completed.TenantId));
        Assert.Equal(EvaluationBatchStatus.Completed, persisted.Status);
        EvaluationCaseExecutionRecord persistedCase = Assert.Single(persisted.Cases);
        Assert.Equal(EvaluationCaseExecutionStatus.Passed, persistedCase.Status);
        Assert.Equal(123, persistedCase.DurationMilliseconds);
        Assert.Equal(["message", "completed"], persistedCase.ObservedEventKinds);
        Assert.Equal(["direct"], persistedCase.ObservedRoutes);
        Assert.Equal("status", Assert.Single(persistedCase.Report!.Checks).Code);
        Assert.Single(await service.ListAsync(suiteId, completed.TenantId, 10));

        EvaluationBatchRecord interrupted = CreateRunningBatch(suiteId, startedAt.AddMinutes(1));
        Assert.True(await service.TryCreateAsync(interrupted));
        Assert.Equal(1, await service.RecoverInterruptedAsync(startedAt.AddMinutes(2)));
        EvaluationBatchRecord recovered = Assert.IsType<EvaluationBatchRecord>(
            await service.GetAsync(interrupted.Id, interrupted.TenantId));
        Assert.Equal(EvaluationBatchStatus.Failed, recovered.Status);
        Assert.Equal(UnifiedEntryErrorCodes.HostInterrupted, recovered.ErrorCode);
        Assert.Equal(
            EvaluationCaseExecutionStatus.Failed,
            Assert.Single(recovered.Cases).Status);
    }

    [Fact]
    public async Task Persist_model_judge_report_and_enforce_configuration_identity()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgEvaluationModelJudgement),
            typeof(AgEvaluationModelJudgementEvaluator),
            typeof(AgEvaluationModelJudgementMinimumScore),
            typeof(AgEvaluationModelJudgementCase),
            typeof(AgEvaluationModelJudgementMetric),
            typeof(AgEvaluationModelJudgementDiagnostic));
        var service = new AgEvaluationModelJudgementServices(
            fixture.CreateRepository<AgEvaluationModelJudgement>());
        DateTimeOffset startedAt = DateTimeOffset.Parse("2026-08-16T12:00:00Z");
        Guid batchId = Guid.NewGuid();
        string configurationSha256 = Hash('f');
        var report = new ModelJudgeReport(
            Guid.NewGuid(),
            "tenant-a",
            "user-a",
            batchId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Hash('a'),
            ModelJudgeEvaluators.Provider,
            ModelJudgeEvaluators.PackageVersion,
            "model-profile-a",
            [ModelJudgeEvaluators.Relevance, ModelJudgeEvaluators.Coherence],
            new Dictionary<string, decimal>
            {
                [ModelJudgeEvaluators.Relevance] = 0.8m,
                [ModelJudgeEvaluators.Coherence] = 0.7m
            },
            configurationSha256,
            "prompt-v1",
            startedAt,
            startedAt.AddSeconds(2),
            true,
            [
                new ModelJudgeCaseResult(
                    Guid.NewGuid(),
                    "case-a",
                    Guid.NewGuid(),
                    Hash('b'),
                    Hash('c'),
                    [
                        new ModelJudgeMetric(
                            ModelJudgeEvaluators.Relevance,
                            0.9m,
                            0.8m,
                            true,
                            ["grounded"])
                    ])
            ]);

        Assert.True(await service.TryCreateAsync(report));
        Assert.False(await service.TryCreateAsync(report));
        Assert.False(await service.TryCreateAsync(report with { Id = Guid.NewGuid() }));
        Assert.Null(await service.GetAsync(report.Id, "tenant-b"));

        ModelJudgeReport persisted = Assert.IsType<ModelJudgeReport>(
            await service.GetByConfigurationAsync(
                batchId,
                report.TenantId,
                configurationSha256));
        Assert.Equal(report.Id, persisted.Id);
        Assert.Equal(report.Evaluators, persisted.Evaluators);
        Assert.Equal(0.8m, persisted.MinimumScores[ModelJudgeEvaluators.Relevance]);
        ModelJudgeMetric metric = Assert.Single(Assert.Single(persisted.Cases).Metrics);
        Assert.Equal(0.9m, metric.Score);
        Assert.Equal(["grounded"], metric.DiagnosticCodes);
        Assert.Single(await service.ListAsync(batchId, report.TenantId, 10));
    }

    private static EvaluationBatchRecord CreateRunningBatch(
        Guid suiteId,
        DateTimeOffset startedAt) => new(
        Guid.NewGuid(),
        "tenant-a",
        "user-a",
        suiteId,
        Guid.NewGuid(),
        Hash('d'),
        EvaluationBatchStatus.Running,
        0,
        startedAt,
        null,
        [
            new EvaluationCaseExecutionRecord(
                Guid.NewGuid(),
                "case-a",
                Guid.NewGuid(),
                Guid.NewGuid(),
                EvaluationCaseExecutionStatus.Running,
                null,
                UnifiedRunStatus.Running,
                null,
                string.Empty)
            {
                ObservedEventKinds = ["run-started"],
                ObservedRoutes = []
            }
        ],
        string.Empty);

    private static string Hash(char value) => new(value, 64);
}

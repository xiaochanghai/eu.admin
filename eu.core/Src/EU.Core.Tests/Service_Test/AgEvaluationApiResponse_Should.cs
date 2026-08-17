#nullable enable

using System.Collections.Concurrent;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgEvaluationApiResponse_Should
{
    [Fact]
    public async Task Wrap_evaluation_suite_lifecycle_queries_and_mutations()
    {
        var repository = new SuiteRepository([]);
        var lifecycle = new EvaluationSuiteLifecycleService(repository, new TargetCatalog());
        EvaluationSuitesController controller = CreateSuiteController(lifecycle);

        AssertServiceSuccess(await controller.List(null, CancellationToken.None), 200,
            typeof(IReadOnlyList<EvaluationSuiteDefinition>));

        EvaluationSuiteDefinition created = (EvaluationSuiteDefinition)AssertServiceSuccess(
            await controller.Create(new CreateEvaluationSuiteRequest("quality-suite", "Quality", ""), CancellationToken.None),
            201,
            typeof(EvaluationSuiteDefinition));
        Assert.Equal($"/api/evaluation-suites/{created.Id}", controller.Response.Headers.Location);
        AssertServiceSuccess(await controller.Get(created.Id, CancellationToken.None), 200,
            typeof(EvaluationSuiteDefinition));

        var specification = new EvaluateRunRequest("Completed", ["OK"], [], [], 0, 120000);
        EvaluationSuiteDefinition saved = (EvaluationSuiteDefinition)AssertServiceSuccess(
            await controller.SaveDraft(
                created.Id,
                new SaveEvaluationSuiteDraftRequest(
                    created.LogicalRevision,
                    created.Name,
                    created.Description,
                    [new EvaluationCaseApiRequest(Guid.NewGuid(), "case", "input", Guid.NewGuid(), Guid.NewGuid(), specification)]),
                CancellationToken.None),
            200,
            typeof(EvaluationSuiteDefinition));
        EvaluationSuiteDefinition published = (EvaluationSuiteDefinition)AssertServiceSuccess(
            await controller.Publish(created.Id, new PublishEvaluationSuiteRequest(saved.LogicalRevision), CancellationToken.None),
            200,
            typeof(EvaluationSuiteDefinition));
        AssertServiceSuccess(
            await controller.SetArchived(created.Id, new SetEvaluationSuiteArchiveRequest(published.LogicalRevision, true), CancellationToken.None),
            200,
            typeof(EvaluationSuiteDefinition));
    }

    [Fact]
    public async Task Wrap_batch_assertion_failures_as_success_and_preserve_model_judge_reports()
    {
        Guid suiteId = Guid.NewGuid();
        EvaluationBatchRecord batch = CreateBatch(suiteId);
        var batches = new BatchRepository([batch]);
        var reports = new ModelJudgeReportRepository([CreateReport(batch)]);
        EvaluationBatchesController controller = CreateBatchController(batches, reports);

        AssertServiceSuccess(await controller.List(suiteId, 20, CancellationToken.None), 200,
            typeof(IReadOnlyList<EvaluationBatchRecord>));
        EvaluationBatchRecord returned = (EvaluationBatchRecord)AssertServiceSuccess(
            await controller.Get(batch.Id, CancellationToken.None), 200, typeof(EvaluationBatchRecord));
        Assert.Equal(EvaluationCaseExecutionStatus.Failed, returned.Cases.Single().Status);
        Assert.False(returned.Cases.Single().Report!.Passed);

        AssertServiceSuccess(
            await controller.ListModelJudgeReports(batch.Id, 20, CancellationToken.None),
            200,
            typeof(IReadOnlyList<ModelJudgeReport>));
        ModelJudgeReport report = (ModelJudgeReport)AssertServiceSuccess(
            await controller.GetModelJudgeReport(batch.Id, reports.Value.Id, CancellationToken.None),
            200,
            typeof(ModelJudgeReport));
        Assert.Equal("configuration-sha256", report.ConfigurationSha256);
        Assert.Equal("prompt-v1", report.PromptVersion);
        Assert.Single(report.Cases.Single().Metrics);
    }

    [Fact]
    public async Task Return_fixed_evaluation_errors_in_service_envelopes()
    {
        EvaluationSuitesController suites = CreateSuiteController(
            new EvaluationSuiteLifecycleService(new SuiteRepository([]), new TargetCatalog()));
        AssertServiceError(await suites.List("invalid", CancellationToken.None), 409, 670007,
            EvaluationSuiteErrorCodes.LifecycleTransitionInvalid);

        EvaluationBatchesController batches = CreateBatchController(new BatchRepository([]), new ModelJudgeReportRepository([]));
        AssertServiceError(
            await batches.Run(new StartEvaluationBatchRequest(Guid.Empty, Guid.Empty), CancellationToken.None),
            400, 670008, EvaluationBatchErrorCodes.RequestInvalid);
        AssertServiceError(
            await batches.Compare(new CompareEvaluationBatchesRequest { Gate = null }, CancellationToken.None),
            400, 670021, EvaluationComparisonErrorCodes.SpecificationInvalid);
        AssertServiceError(
            await batches.RunModelJudge(Guid.NewGuid(), new RunModelJudgeRequest(), CancellationToken.None),
            400, 670023, ModelJudgeErrorCodes.RequestInvalid);

        var runs = new RunEvaluationsController(null!, new CallerContext()) { ControllerContext = Context() };
        AssertServiceError(
            await runs.Evaluate("invalid", new EvaluateRunRequest("Completed", [], [], [], null, null), CancellationToken.None),
            400, 670031, RunEvaluationErrorCodes.SpecificationInvalid);
    }

    private static EvaluationSuitesController CreateSuiteController(EvaluationSuiteLifecycleService lifecycle) =>
        new(lifecycle, new CallerContext()) { ControllerContext = Context() };

    private static EvaluationBatchesController CreateBatchController(
        IEvaluationBatchRepository batches,
        ModelJudgeReportRepository reports)
    {
        var service = new EvaluationBatchService(null!, batches, null!, null!, null!, null!);
        var comparison = new EvaluationBatchComparisonService(batches);
        var judge = new ModelJudgeService(
            batches, null!, reports, null!, null!, null!,
            new ModelJudgePolicy(false, 20, TimeSpan.FromSeconds(30)));
        return new EvaluationBatchesController(service, comparison, judge, new CallerContext())
        {
            ControllerContext = Context()
        };
    }

    private static ControllerContext Context() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-evaluation-contract",
            RequestServices = new ServiceCollection().BuildServiceProvider()
        }
    };

    private static object AssertServiceSuccess(IActionResult action, int httpStatus, Type expectedDataType)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        object body = Assert.IsAssignableFrom<object>(json.Value);
        Assert.Equal(200, body.GetType().GetProperty("Status")?.GetValue(body));
        Assert.Equal(true, body.GetType().GetProperty("Success")?.GetValue(body));
        object data = Assert.IsAssignableFrom<object>(body.GetType().GetProperty("Data")?.GetValue(body));
        Assert.True(expectedDataType.IsInstanceOfType(data), data.GetType().FullName);
        return data;
    }

    private static void AssertServiceError(IActionResult action, int httpStatus, int businessStatus, string errorCode)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        ServiceResult<AgentApiErrorData> body = Assert.IsType<ServiceResult<AgentApiErrorData>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
        Assert.Equal(errorCode, body.Data.ErrorCode);
        Assert.Equal("trace-evaluation-contract", body.Data.TraceId);
    }

    private static EvaluationBatchRecord CreateBatch(Guid suiteId)
    {
        Guid runId = Guid.NewGuid();
        var report = new RunEvaluationReport(
            runId, DateTimeOffset.UtcNow, false, 0m, "output-sha256", 3,
            [new RunEvaluationCheck("output-contains", false, "present", "absent")]);
        var item = new EvaluationCaseExecutionRecord(
            Guid.NewGuid(), "case", Guid.NewGuid(), Guid.NewGuid(),
            EvaluationCaseExecutionStatus.Failed, runId, UnifiedRunStatus.Completed,
            report, EvaluationBatchErrorCodes.AssertionFailed);
        return new EvaluationBatchRecord(
            Guid.NewGuid(), "tenant", "operator", suiteId, Guid.NewGuid(), "suite-sha256",
            EvaluationBatchStatus.Completed, 3, DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow, [item], "");
    }

    private static ModelJudgeReport CreateReport(EvaluationBatchRecord batch) => new(
        Guid.NewGuid(), "tenant", "operator", batch.Id, batch.SuiteId, batch.SuiteVersionId,
        batch.SuiteVersionContentSha256, "provider", "1.0.0", "model",
        [ModelJudgeEvaluators.Relevance], new Dictionary<string, decimal> { [ModelJudgeEvaluators.Relevance] = 4m },
        "configuration-sha256", "prompt-v1", DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow,
        false,
        [new ModelJudgeCaseResult(
            batch.Cases.Single().CaseId, "case", batch.Cases.Single().UnifiedRunId!.Value,
            "input-sha256", "output-sha256",
            [new ModelJudgeMetric(ModelJudgeEvaluators.Relevance, 3m, 4m, false, ["LOW_SCORE"])])]);

    private sealed class CallerContext : ICallerContext
    {
        public string UserId => "operator";
        public string TenantId => "tenant";
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
        public string CorrelationId => "correlation";
    }

    private sealed class TargetCatalog : IEvaluationTargetCatalog
    {
        public Task<bool> IsPublishedAsync(Guid agentId, Guid agentVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class SuiteRepository(IEnumerable<EvaluationSuiteDefinition> values) : IEvaluationSuiteRepository
    {
        private readonly ConcurrentDictionary<Guid, EvaluationSuiteDefinition> _values =
            new(values.ToDictionary(value => value.Id));

        public Task<EvaluationSuiteDefinition?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.GetValueOrDefault(id));
        public Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvaluationSuiteDefinition>>(_values.Values.ToArray());
        public Task<bool> TryCreateAsync(EvaluationSuiteDefinition value, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryAdd(value.Id, value));
        public Task<bool> TryReplaceAsync(EvaluationSuiteDefinition value, long expectedLogicalRevision, CancellationToken cancellationToken = default)
        {
            if (!_values.TryGetValue(value.Id, out EvaluationSuiteDefinition? current)
                || current.LogicalRevision != expectedLogicalRevision) return Task.FromResult(false);
            return Task.FromResult(_values.TryUpdate(value.Id, value, current));
        }
    }

    private sealed class BatchRepository(IEnumerable<EvaluationBatchRecord> values) : IEvaluationBatchRepository
    {
        private readonly ConcurrentDictionary<Guid, EvaluationBatchRecord> _values =
            new(values.ToDictionary(value => value.Id));
        public Task<EvaluationBatchRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(id, out EvaluationBatchRecord? value) && value.TenantId == tenantId ? value : null);
        public Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(Guid suiteId, string tenantId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvaluationBatchRecord>>(_values.Values
                .Where(value => value.SuiteId == suiteId && value.TenantId == tenantId).Take(take).ToArray());
        public Task<bool> TryCreateAsync(EvaluationBatchRecord value, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryAdd(value.Id, value));
        public Task<bool> TryReplaceAsync(EvaluationBatchRecord value, long expectedLogicalRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class ModelJudgeReportRepository(IEnumerable<ModelJudgeReport> values) : IModelJudgeReportRepository
    {
        private readonly ConcurrentDictionary<Guid, ModelJudgeReport> _values =
            new(values.ToDictionary(value => value.Id));
        public ModelJudgeReport Value => _values.Values.Single();
        public Task<ModelJudgeReport?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(id, out ModelJudgeReport? value) && value.TenantId == tenantId ? value : null);
        public Task<ModelJudgeReport?> GetByConfigurationAsync(Guid batchId, string tenantId, string configurationSha256, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.Values.FirstOrDefault(value => value.BatchId == batchId && value.TenantId == tenantId && value.ConfigurationSha256 == configurationSha256));
        public Task<IReadOnlyList<ModelJudgeReport>> ListAsync(Guid batchId, string tenantId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ModelJudgeReport>>(_values.Values
                .Where(value => value.BatchId == batchId && value.TenantId == tenantId).Take(take).ToArray());
        public Task<bool> TryCreateAsync(ModelJudgeReport value, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryAdd(value.Id, value));
    }
}

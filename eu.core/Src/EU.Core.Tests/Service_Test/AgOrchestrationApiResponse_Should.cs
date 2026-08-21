#nullable enable

using System.Collections.Concurrent;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using EU.Core.Services;

namespace EU.Core.Tests.Service_Test;

public sealed class AgOrchestrationApiResponse_Should
{
    [Fact]
    public async Task Wrap_orchestration_lifecycle_queries_and_mutations()
    {
        AgentDefinition agent = CreateAgent();
        OrchestrationDefinition definition = CreateDefinition(agent.Id);
        var repository = new OrchestrationRepository([definition]);
        var agents = new AgentCatalog([agent]);
        var lifecycle = new OrchestrationLifecycleService(repository, agents);
        OrchestrationsController controller = CreateController(lifecycle, CreateRuntime());

        AssertServiceSuccess(
            await controller.List(null, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(IReadOnlyList<OrchestrationListItem>));
        AssertServiceSuccess(
            await controller.Get(definition.Id, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(OrchestrationDefinition));

        object created = AssertServiceSuccess(
            await controller.Create(
                new CreateOrchestrationRequest("new-flow", "New flow", "Description"),
                CancellationToken.None),
            StatusCodes.Status201Created,
            typeof(OrchestrationDefinition));
        Guid createdId = ((OrchestrationDefinition)created).Id;
        Assert.Equal($"/api/orchestrations/{createdId}", controller.Response.Headers.Location);

        OrchestrationDefinition saved = (OrchestrationDefinition)AssertServiceSuccess(
            await controller.SaveDraft(
                definition.Id,
                new SaveOrchestrationRequest(
                    0,
                    "Updated flow",
                    "Updated description",
                    OrchestrationStatus.Disabled,
                    definition.Draft.StartNodeId,
                    definition.Draft.Nodes,
                    definition.Draft.Edges),
                CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(OrchestrationDefinition));

        OrchestrationDefinition published = (OrchestrationDefinition)AssertServiceSuccess(
            await controller.Publish(
                definition.Id,
                new PublishOrchestrationRequest(saved.LogicalRevision),
                CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(OrchestrationDefinition));

        AssertServiceSuccess(
            await controller.SetArchived(
                definition.Id,
                new SetOrchestrationArchiveRequest(published.LogicalRevision, true),
                CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(OrchestrationDefinition));
    }

    [Fact]
    public async Task Wrap_orchestration_run_queries_mutations_details_and_output()
    {
        Guid orchestrationId = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        OrchestrationDefinition runnable = CreateRunnableDefinition(orchestrationId, versionId);
        var orchestrationRepository = new OrchestrationRepository([runnable]);
        OrchestrationRunRecord completed = CreateCompletedRun(orchestrationId, versionId);
        var runRepository = new OrchestrationRunRepository(
            [completed],
            [new OrchestrationRunDetails(
                completed.Id,
                orchestrationId,
                "input",
                "{\"snake_key\":true}",
                [])]);
        var runtime = new OrchestrationRuntimeService(
            orchestrationRepository,
            runRepository,
            new AgentCatalog([]),
            null!);
        var lifecycle = new OrchestrationLifecycleService(
            new OrchestrationRepository([]),
            new AgentCatalog([]));
        OrchestrationsController controller = CreateController(lifecycle, runtime);

        AssertServiceSuccess(
            await controller.Start(
                orchestrationId,
                new StartOrchestrationRunRequest("run input"),
                CancellationToken.None),
            StatusCodes.Status202Accepted,
            typeof(OrchestrationRunRecord));
        AssertServiceSuccess(
            await controller.Runs(orchestrationId, 20, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(IReadOnlyList<OrchestrationRunRecord>));

        OrchestrationRunRecord run = (OrchestrationRunRecord)AssertServiceSuccess(
            await controller.Run(orchestrationId, completed.Id, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(OrchestrationRunRecord));
        Assert.Equal("ORCHESTRATION_RUN_FAILED", run.ErrorCode);

        AssertServiceSuccess(
            await controller.Cancel(orchestrationId, completed.Id, CancellationToken.None),
            StatusCodes.Status202Accepted);
        AssertServiceSuccess(
            await controller.Details(orchestrationId, completed.Id, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(OrchestrationRunDetails));

        object output = AssertServiceSuccess(
            await controller.Output(orchestrationId, completed.Id, CancellationToken.None),
            StatusCodes.Status200OK);
        Assert.Equal("{\"snake_key\":true}",
            output.GetType().GetProperty("Output")?.GetValue(output));
    }

    [Fact]
    public async Task Return_fixed_orchestration_errors()
    {
        var lifecycle = new OrchestrationLifecycleService(
            new OrchestrationRepository([]),
            new AgentCatalog([]));
        OrchestrationsController controller = CreateController(lifecycle, CreateRuntime());

        AssertServiceError(
            await controller.List("invalid", CancellationToken.None),
            StatusCodes.Status409Conflict,
            650012,
            OrchestrationErrorCodes.LifecycleTransitionInvalid);
        AssertServiceError(
            await controller.Get(Guid.NewGuid(), CancellationToken.None),
            StatusCodes.Status404NotFound,
            650001,
            OrchestrationErrorCodes.NotFound);
        AssertServiceError(
            await controller.Start(
                Guid.NewGuid(),
                new StartOrchestrationRunRequest(""),
                CancellationToken.None),
            StatusCodes.Status400BadRequest,
            650010,
            OrchestrationErrorCodes.RunInputInvalid);
        AssertServiceError(
            await controller.Run(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None),
            StatusCodes.Status404NotFound,
            650009,
            OrchestrationErrorCodes.RunNotFound);
    }

    private static OrchestrationsController CreateController(
        OrchestrationLifecycleService lifecycle,
        OrchestrationRuntimeService runtime)
    {
        var controller = new OrchestrationsController(lifecycle, runtime)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "trace-orchestration-contract",
                    RequestServices = new ServiceCollection().BuildServiceProvider()
                }
            }
        };
        return controller;
    }

    private static OrchestrationRuntimeService CreateRuntime()
    {
        var repository = new OrchestrationRepository([]);
        return new OrchestrationRuntimeService(
            repository,
            new OrchestrationRunRepository([], []),
            new AgentCatalog([]),
            null!);
    }

    private static object AssertServiceSuccess(
        IActionResult action,
        int httpStatus,
        Type? expectedDataType = null)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        object body = Assert.IsAssignableFrom<object>(json.Value);
        Assert.Equal(200, body.GetType().GetProperty("Status")?.GetValue(body));
        Assert.Equal(true, body.GetType().GetProperty("Success")?.GetValue(body));
        object data = Assert.IsAssignableFrom<object>(
            body.GetType().GetProperty("Data")?.GetValue(body));
        if (expectedDataType is not null)
            Assert.True(expectedDataType.IsInstanceOfType(data), data.GetType().FullName);
        return data;
    }

    private static void AssertServiceError(
        IActionResult action,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        ServiceResult<AgentApiErrorData> body =
            Assert.IsType<ServiceResult<AgentApiErrorData>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
        Assert.Equal(errorCode, body.Data.ErrorCode);
        Assert.Equal("trace-orchestration-contract", body.Data.TraceId);
    }

    private static AgentDefinition CreateAgent()
    {
        Guid id = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        var draft = new AgentVersion(
            Guid.NewGuid(), "draft", true, "", "model", AgentOutputMode.Text,
            null, null, null);
        var snapshot = new AgentVersionSnapshot(
            versionId, "flow-agent", "", "model", AgentOutputMode.Text,
            null, [], []);
        var published = new AgentVersion(
            versionId, "1.0.0", false, "", "model", AgentOutputMode.Text,
            null, null, snapshot);
        return new AgentDefinition(
            id, "flow-agent", "Flow Agent", "", AgentRuntimeStatus.Enabled,
            0, draft, [published]);
    }

    private static OrchestrationDefinition CreateDefinition(Guid agentId)
    {
        var node = new OrchestrationNode(
            "node-1", "Node 1", agentId,
            OrchestrationNodeInputMode.InitialInput, "", 0, 30);
        var draft = new OrchestrationVersion(
            Guid.NewGuid(), "0.1.0", true, "node-1", [node], [], null);
        return new OrchestrationDefinition(
            Guid.NewGuid(), "test-flow", "Test flow", "",
            OrchestrationStatus.Enabled, 0, draft, []);
    }

    private static OrchestrationDefinition CreateRunnableDefinition(Guid id, Guid versionId)
    {
        var draft = new OrchestrationVersion(
            Guid.NewGuid(), "0.1.0", true, "", [], [], null);
        var snapshot = new OrchestrationVersionSnapshot(
            versionId, "runnable-flow", "", [], [], []);
        var published = new OrchestrationVersion(
            versionId, "1.0.0", false, "", [], [], snapshot);
        return new OrchestrationDefinition(
            id, "runnable-flow", "Runnable flow", "",
            OrchestrationStatus.Enabled, 0, draft, [published]);
    }

    private static OrchestrationRunRecord CreateCompletedRun(Guid orchestrationId, Guid versionId) =>
        new(
            Guid.NewGuid(), orchestrationId, versionId, "runnable-flow",
            OrchestrationRunStatus.Completed, DateTimeOffset.UtcNow.AddSeconds(-2),
            DateTimeOffset.UtcNow, new string('a', 64),
            "ORCHESTRATION_RUN_FAILED", []);

    private sealed class AgentCatalog(IReadOnlyList<AgentDefinition> values)
        : IAgentDefinitionCatalog
    {
        public Task<AgentDefinition?> GetDefinitionAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(values.FirstOrDefault(value => value.Id == id));

        public Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(
            AgentDefinitionQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(values);
    }

    private sealed class OrchestrationRepository(
        IEnumerable<OrchestrationDefinition> values) : IOrchestrationRepository
    {
        private readonly ConcurrentDictionary<Guid, OrchestrationDefinition> _values =
            new(values.ToDictionary(value => value.Id));

        public Task<OrchestrationDefinition?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.GetValueOrDefault(id));

        public Task<IReadOnlyList<OrchestrationDefinition>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrchestrationDefinition>>(_values.Values.ToArray());

        public Task<bool> TryCreateAsync(
            OrchestrationDefinition value,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryAdd(value.Id, value));

        public Task<bool> TryReplaceAsync(
            OrchestrationDefinition value,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            if (!_values.TryGetValue(value.Id, out OrchestrationDefinition? existing)
                || existing.LogicalRevision != expectedRevision)
                return Task.FromResult(false);
            return Task.FromResult(_values.TryUpdate(value.Id, value, existing));
        }
    }

    private sealed class OrchestrationRunRepository(
        IEnumerable<OrchestrationRunRecord> runs,
        IEnumerable<OrchestrationRunDetails> details) : IOrchestrationRunRepository
    {
        private readonly ConcurrentDictionary<Guid, OrchestrationRunRecord> _runs =
            new(runs.ToDictionary(value => value.Id));
        private readonly ConcurrentDictionary<Guid, OrchestrationRunDetails> _details =
            new(details.ToDictionary(value => value.RunId));

        public Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default)
        {
            _runs[value.Id] = value;
            return Task.CompletedTask;
        }

        public Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_runs.GetValueOrDefault(id));

        public Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
            Guid orchestrationId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrchestrationRunRecord>>(_runs.Values
                .Where(value => value.OrchestrationId == orchestrationId)
                .Take(take)
                .ToArray());

        public Task SaveDetailsAsync(OrchestrationRunDetails value, CancellationToken cancellationToken = default)
        {
            _details[value.RunId] = value;
            return Task.CompletedTask;
        }

        public Task<OrchestrationRunDetails?> GetDetailsAsync(
            Guid runId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_details.GetValueOrDefault(runId));

        public Task<bool> TrySaveRunningDetailsAsync(
            OrchestrationRunDetails value,
            CancellationToken cancellationToken = default)
        {
            bool running = _runs.GetValueOrDefault(value.RunId)?.Status == OrchestrationRunStatus.Running;
            if (running) _details[value.RunId] = value;
            return Task.FromResult(running);
        }

        public Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
            Guid runId,
            OrchestrationRunStatus runStatus,
            OrchestrationNodeRunStatus nodeStatus,
            OrchestrationTerminalTransitionPolicy transitionPolicy,
            DateTimeOffset finishedAtUtc,
            string errorCode,
            OrchestrationRunDetails? detailsIfMissing,
            CancellationToken cancellationToken = default)
        {
            if (!_runs.TryGetValue(runId, out OrchestrationRunRecord? current)
                || current.Status != OrchestrationRunStatus.Running)
                return Task.FromResult(new OrchestrationRunTransitionResult(current, false));
            OrchestrationRunRecord terminal = current with
            {
                Status = runStatus,
                FinishedAtUtc = finishedAtUtc,
                ErrorCode = errorCode
            };
            _runs[runId] = terminal;
            if (detailsIfMissing is not null) _details.TryAdd(runId, detailsIfMissing);
            return Task.FromResult(new OrchestrationRunTransitionResult(terminal, true));
        }

        public Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
            Guid runId,
            DateTimeOffset recoveredAtUtc,
            string errorCode,
            CancellationToken cancellationToken = default) =>
            TryFinalizeRunningAsync(
                runId,
                OrchestrationRunStatus.Failed,
                OrchestrationNodeRunStatus.Failed,
                OrchestrationTerminalTransitionPolicy.TerminalizePending,
                recoveredAtUtc,
                errorCode,
                null,
                cancellationToken);
    }
}

using System.Runtime.CompilerServices;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Orchestration;

public sealed class OrchestrationTests
{
    [Fact]
    public async Task Draft_rejects_cycles_and_publish_freezes_current_agent_version()
    {
        var agentRepository = new InMemoryAgentRepository();
        AgentDefinition agent = await PublishedAgent(agentRepository, "worker");
        var repository = new InMemoryOrchestrationRepository();
        var service = new OrchestrationLifecycleService(repository, agentRepository);
        OrchestrationDefinition flow = Successful(await service.CreateAsync(
            new CreateOrchestrationCommand("review-flow", "Review", "")));
        OrchestrationNode first = Node("first", agent.Id);
        OrchestrationNode second = Node("second", agent.Id);
        OrchestrationOperationResult<OrchestrationDefinition> cyclic = await service.SaveDraftAsync(
            new SaveOrchestrationDraftCommand(
                flow.Id, 0, "Review", "", OrchestrationStatus.Enabled, "first",
                [first, second],
                [
                    new OrchestrationEdge("first", "second", OrchestrationEdgeCondition.Succeeded, "", 0),
                    new OrchestrationEdge("second", "first", OrchestrationEdgeCondition.Succeeded, "", 0)
                ]));
        Assert.Equal(OrchestrationErrorCodes.DefinitionInvalid, cyclic.Error?.Code);

        flow = Successful(await service.SaveDraftAsync(new SaveOrchestrationDraftCommand(
            flow.Id, 0, "Review", "", OrchestrationStatus.Enabled, "first",
            [first, second],
            [new OrchestrationEdge("first", "second", OrchestrationEdgeCondition.Succeeded, "", 0)])));
        flow = Successful(await service.PublishAsync(
            new PublishOrchestrationCommand(flow.Id, flow.LogicalRevision)));

        OrchestrationVersionSnapshot snapshot = Assert.Single(flow.PublishedVersions).Snapshot!;
        Assert.Equal(agent.PublishedVersions.Last().Id, Assert.Single(snapshot.Agents).AgentVersionId);
        Assert.Equal(2, snapshot.Nodes.Count);
    }

    [Fact]
    public async Task Runtime_persists_node_attempts_and_tool_call_content()
    {
        var agentRepository = new InMemoryAgentRepository();
        AgentDefinition agent = await PublishedAgent(agentRepository, "runtime-worker");
        var definitions = new InMemoryOrchestrationRepository();
        var lifecycle = new OrchestrationLifecycleService(definitions, agentRepository);
        OrchestrationDefinition flow = Successful(await lifecycle.CreateAsync(
            new CreateOrchestrationCommand("runtime-flow", "Runtime", "")));
        flow = Successful(await lifecycle.SaveDraftAsync(new SaveOrchestrationDraftCommand(
            flow.Id, 0, "Runtime", "", OrchestrationStatus.Enabled, "collect",
            [
                Node("collect", agent.Id),
                Node("summarize", agent.Id) with
                {
                    InputMode = OrchestrationNodeInputMode.Template,
                    InputTemplate = "Summarize: {{previous}}"
                }
            ],
            [new OrchestrationEdge("collect", "summarize", OrchestrationEdgeCondition.OutputContains, "done", 0)])));
        flow = Successful(await lifecycle.PublishAsync(
            new PublishOrchestrationCommand(flow.Id, flow.LogicalRevision)));

        var runRepository = new InMemoryOrchestrationRunRepository();
        var agentRuntime = new AgentRuntimeService(
            agentRepository, new EmptyToolCatalog(), new ToolCallingEngine(),
            new InMemoryAgentRunAuditRepository(), new JsonSchemaValidator());
        var runtime = new OrchestrationRuntimeService(
            definitions, runRepository, agentRepository, agentRuntime);
        OrchestrationRunRecord started = Successful(await runtime.StartAsync(flow.Id, "sensitive input"));
        OrchestrationRunRecord completed = await WaitForTerminal(runtime, started.Id);

        Assert.Equal(OrchestrationRunStatus.Completed, completed.Status);
        Assert.All(completed.Nodes, node => Assert.Equal(OrchestrationNodeRunStatus.Completed, node.Status));
        Assert.All(completed.Nodes, node => Assert.NotEmpty(node.InputSha256));
        OrchestrationRunDetails details = (await runRepository.GetDetailsAsync(completed.Id))!;
        Assert.Equal("sensitive input", details.Input);
        Assert.Equal("done", details.Output);
        Assert.Equal(["collect", "summarize"], details.Attempts.Select(value => value.NodeId));
        Assert.Equal("Summarize: done", details.Attempts[1].Input);
        OrchestrationToolCallRecord tool = Assert.Single(details.Attempts[0].ToolCalls);
        Assert.Equal("""{"supplierId":"S1"}""", tool.ArgumentsJson);
        Assert.Equal("""{"type":"module","id":"1","moduleCode":"supplier"}""", tool.ResultContent);
        string serialized = System.Text.Json.JsonSerializer.Serialize(completed);
        Assert.DoesNotContain("sensitive input", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("done", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Oversized_tool_result_fails_run_without_truncating_content()
    {
        var agentRepository = new InMemoryAgentRepository();
        AgentDefinition agent = await PublishedAgent(agentRepository, "limited-worker");
        var definitions = new InMemoryOrchestrationRepository();
        var lifecycle = new OrchestrationLifecycleService(definitions, agentRepository);
        OrchestrationDefinition flow = Successful(await lifecycle.CreateAsync(
            new CreateOrchestrationCommand("limited-flow", "Limited", "")));
        flow = Successful(await lifecycle.SaveDraftAsync(new SaveOrchestrationDraftCommand(
            flow.Id, 0, "Limited", "", OrchestrationStatus.Enabled, "query",
            [Node("query", agent.Id)], [])));
        flow = Successful(await lifecycle.PublishAsync(
            new PublishOrchestrationCommand(flow.Id, flow.LogicalRevision)));

        var runRepository = new InMemoryOrchestrationRunRepository();
        var agentRuntime = new AgentRuntimeService(
            agentRepository, new EmptyToolCatalog(), new ToolCallingEngine(),
            new InMemoryAgentRunAuditRepository(), new JsonSchemaValidator());
        var runtime = new OrchestrationRuntimeService(
            definitions,
            runRepository,
            agentRepository,
            agentRuntime,
            new ExecutionPayloadLimits(ToolResultCharacters: 10));

        OrchestrationRunRecord started = Successful(await runtime.StartAsync(flow.Id, "query"));
        OrchestrationRunRecord completed = await WaitForTerminal(runtime, started.Id);

        Assert.Equal(OrchestrationRunStatus.Failed, completed.Status);
        Assert.Equal("ORCHESTRATION_PAYLOAD_LIMIT_EXCEEDED", completed.ErrorCode);
        OrchestrationRunDetails details = (await runRepository.GetDetailsAsync(completed.Id))!;
        Assert.DoesNotContain(
            details.Attempts.SelectMany(value => value.ToolCalls),
            value => value.ResultContent.Length > 10);
    }

    private static OrchestrationNode Node(string id, Guid agentId) =>
        new(id, id, agentId, OrchestrationNodeInputMode.InitialInput, "", 0, 30);

    private static async Task<AgentDefinition> PublishedAgent(InMemoryAgentRepository repository, string code)
    {
        var lifecycle = new AgentLifecycleService(repository);
        AgentDefinition value = AgentSuccessful(await lifecycle.CreateAsync(new CreateAgentCommand(code)));
        value = AgentSuccessful(await lifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            value.Id, value.LogicalRevision, "work", "qwen", AgentOutputMode.Text, null)));
        return AgentSuccessful(await lifecycle.PublishAsync(
            new PublishAgentCommand(value.Id, value.LogicalRevision)));
    }

    private static async Task<OrchestrationRunRecord> WaitForTerminal(
        OrchestrationRuntimeService runtime, Guid runId)
    {
        for (int index = 0; index < 100; index++)
        {
            OrchestrationRunRecord value = (await runtime.GetAsync(runId))!;
            if (value.Status != OrchestrationRunStatus.Running) return value;
            await Task.Delay(10);
        }
        throw new TimeoutException("The orchestration did not finish.");
    }

    private static T Successful<T>(OrchestrationOperationResult<T> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return result.Value!;
    }

    private static T AgentSuccessful<T>(AgentOperationResult<T> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return result.Value!;
    }

    private sealed class EmptyToolCatalog : IPublishedMcpToolCatalog
    {
        public Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublishedMcpToolReference>>([]);
    }

    private sealed class ToolCallingEngine : IAgentRuntimeEngine
    {
        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(
            AgentRunContext context,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            Guid toolVersionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid callId = Guid.NewGuid();
            yield return new AgentRunEvent(
                context.RunId,
                0,
                AgentRunEventKind.ToolStarted,
                DateTimeOffset.UtcNow,
                ToolVersionId: toolVersionId,
                ToolName: "get_supplier",
                ToolCallId: callId)
            {
                ArgumentsJson = """{"supplierId":"S1"}"""
            };
            yield return new AgentRunEvent(
                context.RunId,
                0,
                AgentRunEventKind.ToolSucceeded,
                DateTimeOffset.UtcNow,
                """{"type":"module","id":"1","moduleCode":"supplier"}""",
                toolVersionId,
                "get_supplier",
                ToolCallId: callId);
            yield return new AgentRunEvent(
                context.RunId, 0, AgentRunEventKind.Delta, DateTimeOffset.UtcNow, "done");
        }
    }
}

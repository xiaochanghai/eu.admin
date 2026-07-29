using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Runtime;

public sealed class AgentRuntimeServiceTests
{
    [Fact]
    public async Task Prepare_uses_latest_published_snapshot_and_exact_authorized_tools()
    {
        Guid toolId = Guid.NewGuid();
        AgentDefinition agent = PublishedAgent(
            AgentRuntimeStatus.Enabled,
            [new AgentToolBindingSnapshot(toolId)]);
        var agents = new InMemoryAgentRepository();
        Assert.True(await agents.TryCreateAsync(agent));
        var audit = new InMemoryAgentRunAuditRepository();
        var service = new AgentRuntimeService(
            agents,
            new FixedToolCatalog(Tool(toolId, McpToolRisk.ReadOnly)),
            new FixedEngine(),
            audit,
            new JsonSchemaValidator());

        AgentRunPreparationResult result =
            await service.PrepareAsync(agent.Id, " search ");

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal("search", result.Context!.Input);
        Assert.Equal(toolId, Assert.Single(result.Context.Tools).ToolVersionId);
        AgentRunAuditRecord record =
            Assert.Single(await audit.ListAsync(agent.Id, 20));
        Assert.Equal(AgentRunStatus.Running, record.Status);
        Assert.NotEmpty(record.InputSha256);
    }

    [Fact]
    public async Task Prepare_rejects_disabled_unpublished_and_stale_tool_versions()
    {
        var agents = new InMemoryAgentRepository();
        AgentDefinition disabled = PublishedAgent(AgentRuntimeStatus.Disabled, []);
        AgentDefinition unpublished = UnpublishedAgent(AgentRuntimeStatus.Enabled);
        Guid staleToolId = Guid.NewGuid();
        AgentDefinition stale = PublishedAgent(
            AgentRuntimeStatus.Enabled,
            [new AgentToolBindingSnapshot(staleToolId)]);
        Assert.True(await agents.TryCreateAsync(disabled));
        Assert.True(await agents.TryCreateAsync(unpublished));
        Assert.True(await agents.TryCreateAsync(stale));
        var service = new AgentRuntimeService(
            agents,
            new FixedToolCatalog(),
            new FixedEngine(),
            new InMemoryAgentRunAuditRepository(),
            new JsonSchemaValidator());

        Assert.Equal(
            AgentRunErrorCodes.AgentDisabled,
            (await service.PrepareAsync(disabled.Id, "hello")).Error?.Code);
        Assert.Equal(
            AgentRunErrorCodes.VersionMissing,
            (await service.PrepareAsync(unpublished.Id, "hello")).Error?.Code);
        Assert.Equal(
            AgentRunErrorCodes.ToolUnavailable,
            (await service.PrepareAsync(stale.Id, "hello")).Error?.Code);
    }

    [Fact]
    public async Task Streaming_records_deltas_tool_outcome_and_completed_terminal()
    {
        Guid toolId = Guid.NewGuid();
        AgentDefinition agent = PublishedAgent(
            AgentRuntimeStatus.Enabled,
            [new AgentToolBindingSnapshot(toolId)]);
        var agents = new InMemoryAgentRepository();
        Assert.True(await agents.TryCreateAsync(agent));
        var audit = new InMemoryAgentRunAuditRepository();
        var engine = new FixedEngine(
            new AgentRunEvent(
                Guid.Empty,
                0,
                AgentRunEventKind.ToolStarted,
                DateTimeOffset.UtcNow,
                ToolVersionId: toolId,
                ToolName: "catalog.search")
            {
                ArgumentsJson = """{"supplierId":"S1"}"""
            },
            new AgentRunEvent(
                Guid.Empty,
                0,
                AgentRunEventKind.ToolSucceeded,
                DateTimeOffset.UtcNow,
                ToolVersionId: toolId,
                ToolName: "catalog.search"),
            new AgentRunEvent(
                Guid.Empty,
                0,
                AgentRunEventKind.Delta,
                DateTimeOffset.UtcNow,
                "done"));
        var service = new AgentRuntimeService(
            agents,
            new FixedToolCatalog(Tool(toolId, McpToolRisk.ReadOnly)),
            engine,
            audit,
            new JsonSchemaValidator());
        AgentRunContext context =
            (await service.PrepareAsync(agent.Id, "search")).Context!;

        AgentRunEvent[] events =
            await service.StreamAsync(context).ToArrayAsync();

        Assert.Equal(AgentRunEventKind.Started, events.First().Kind);
        Assert.Equal(AgentRunEventKind.Completed, events.Last().Kind);
        Assert.Equal(
            Enumerable.Range(1, events.Length).Select(value => (long)value),
            events.Select(value => value.Sequence));
        AgentRunAuditRecord record =
            Assert.Single(await audit.ListAsync(agent.Id, 20));
        Assert.Equal(AgentRunStatus.Completed, record.Status);
        Assert.Equal(4, record.OutputCharacters);
        Assert.Equal(1, record.ToolCallCount);
        Assert.Equal(
            AgentRunEventKind.ToolSucceeded,
            Assert.Single(record.ToolCalls).Status);
        Assert.Equal(
            """{"supplierId":"S1"}""",
            events.Single(value => value.Kind == AgentRunEventKind.ToolStarted).ArgumentsJson);
    }

    private static PublishedMcpToolReference Tool(Guid id, McpToolRisk risk) =>
        new(
            Guid.NewGuid(),
            "catalog",
            "Catalog",
            id,
            "catalog.search",
            "Search",
            """{"type":"object"}""",
            risk,
            "hash");

    private static AgentDefinition PublishedAgent(
        AgentRuntimeStatus status,
        IReadOnlyList<AgentToolBindingSnapshot> tools)
    {
        AgentDefinition value = UnpublishedAgent(status);
        AgentVersionSnapshot snapshot = new(
            Guid.NewGuid(),
            value.Code,
            "Help the user.",
            "qwen-safe",
            AgentOutputMode.Text,
            null,
            [],
            tools);
        AgentVersion published = new(
            snapshot.VersionId,
            "1.0.0",
            false,
            snapshot.Instructions,
            snapshot.ModelProfileId,
            snapshot.OutputMode,
            snapshot.OutputJsonSchema,
            null,
            snapshot);
        return value with { PublishedVersions = [published] };
    }

    private static AgentDefinition UnpublishedAgent(AgentRuntimeStatus status)
    {
        Guid id = Guid.NewGuid();
        return new AgentDefinition(
            id,
            $"agent-{id:N}",
            "Agent",
            "",
            status,
            0,
            new AgentVersion(
                Guid.NewGuid(),
                "Draft",
                true,
                "",
                "",
                AgentOutputMode.Text,
                null,
                null,
                null),
            []);
    }

    private sealed class FixedToolCatalog(params PublishedMcpToolReference[] tools)
        : IPublishedMcpToolCatalog
    {
        public Task<bool> ExistsAsync(
            Guid toolVersionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(tools.Any(tool => tool.ToolVersionId == toolVersionId));

        public Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublishedMcpToolReference>>(tools);
    }

    private sealed class FixedEngine(params AgentRunEvent[] events)
        : IAgentRuntimeEngine
    {
        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(
            AgentRunContext context,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            foreach (AgentRunEvent value in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return value;
                await Task.Yield();
            }
        }
    }
}

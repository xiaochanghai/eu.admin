using EU.Core.Agent.Application.Mcp;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgMcpPersistence_Should
{
    [Fact]
    public async Task Persist_sync_classify_and_publish_mcp_tool_versions()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgMcpServerDefinition),
            typeof(AgMcpServerArgument),
            typeof(AgMcpToolVersion));
        var discovery = new StubMcpToolDiscovery(
        [
            new DiscoveredMcpTool(
                "query_business_data",
                "Query business data",
                "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}}}")
        ]);
        var service = new AgMcpServerDefinitionServices(
            fixture.CreateRepository<AgMcpServerDefinition>(),
            discovery);
        string code = $"mcp-{Guid.NewGuid():N}";
        var create = new CreateMcpServerCommand(
            code,
            "Business Query",
            "description",
            McpTransportKind.StreamableHttp,
            "https://localhost/mcp",
            string.Empty,
            ["--tenant", "development"],
            "alias:business-query",
            true);

        McpOperationResult<McpServerDefinition> created = await service.CreateAsync(create);
        Assert.True(created.Succeeded);
        McpServerDefinition initial = Assert.IsType<McpServerDefinition>(created.Value);
        Assert.Equal(McpServerStatus.NotSynced, initial.Status);
        Assert.Equal(["--tenant", "development"], initial.Arguments);
        Assert.False((await service.CreateAsync(create)).Succeeded);

        McpOperationResult<McpServerDefinition> synchronized = await service.SyncAsync(
            new SyncMcpServerCommand(initial.Id, 0));
        Assert.True(synchronized.Succeeded);
        McpServerDefinition synced = Assert.IsType<McpServerDefinition>(synchronized.Value);
        Assert.Equal(McpServerStatus.Healthy, synced.Status);
        McpToolVersion discovered = Assert.Single(synced.ToolVersions);
        Assert.Equal(McpToolRisk.Unknown, discovered.Risk);
        Assert.False(await service.ExistsAsync(discovered.Id));
        Assert.Empty(await ((IPublishedMcpToolCatalog)service).ListAsync());

        McpOperationResult<McpServerDefinition> classified = await service.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                initial.Id,
                discovered.Id,
                synced.LogicalRevision,
                McpToolRisk.ReadOnly));
        Assert.True(classified.Succeeded);
        McpServerDefinition ready = Assert.IsType<McpServerDefinition>(classified.Value);
        Assert.Equal(2, ready.ToolVersions.Count);
        Guid classifiedId = Assert.Single(ready.CurrentToolVersionIds);
        Assert.NotEqual(discovered.Id, classifiedId);
        Assert.True(await service.ExistsAsync(classifiedId));

        PublishedMcpToolReference published = Assert.Single(
            await ((IPublishedMcpToolCatalog)service).ListAsync());
        Assert.Equal(classifiedId, published.ToolVersionId);
        Assert.Equal("query_business_data", published.ToolName);
        Assert.Equal(McpToolRisk.ReadOnly, published.Risk);
        McpServerDefinition reloaded = Assert.IsType<McpServerDefinition>(
            await service.GetAsync(initial.Id));
        Assert.Equal(ready.LogicalRevision, reloaded.LogicalRevision);
        Assert.Equal(2, reloaded.ToolVersions.Count);
        Assert.Single(await service.ListAsync(new McpServerQuery("Business")));

        Assert.False((await service.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                initial.Id,
                classifiedId,
                synced.LogicalRevision,
                McpToolRisk.Mutating))).Succeeded);
    }

    private sealed class StubMcpToolDiscovery(
        IReadOnlyList<DiscoveredMcpTool> tools) : IMcpToolDiscovery
    {
        public Task<IReadOnlyList<DiscoveredMcpTool>> DiscoverAsync(
            McpServerDefinition server,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(tools);
        }
    }
}

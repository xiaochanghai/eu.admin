using EU.Core.IServices.Mcp;
using EU.Core.Model;
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

        ServiceResult<McpServerDefinition> created = await service.CreateAsync(create);
        Assert.True(created.Success);
        McpServerDefinition initial = Assert.IsType<McpServerDefinition>(created.Data);
        Assert.Equal(McpServerStatus.NotSynced, initial.Status);
        Assert.Equal(["--tenant", "development"], initial.Arguments);
        Assert.False((await service.CreateAsync(create)).Success);

        ServiceResult<McpServerDefinition> synchronized = await service.SyncAsync(
            new SyncMcpServerCommand(initial.Id, 0));
        Assert.True(synchronized.Success);
        McpServerDefinition synced = Assert.IsType<McpServerDefinition>(synchronized.Data);
        Assert.Equal(McpServerStatus.Healthy, synced.Status);
        McpToolVersion discovered = Assert.Single(synced.ToolVersions);
        Assert.Equal(McpToolRisk.Unknown, discovered.Risk);
        Assert.False(await service.ExistsAsync(discovered.Id));
        Assert.Empty(await ((IPublishedMcpToolCatalog)service).ListAsync());

        ServiceResult<McpServerDefinition> classified = await service.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                initial.Id,
                discovered.Id,
                synced.LogicalRevision,
                McpToolRisk.ReadOnly));
        Assert.True(classified.Success);
        McpServerDefinition ready = Assert.IsType<McpServerDefinition>(classified.Data);
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
                McpToolRisk.Mutating))).Success);
    }

    [Fact]
    public async Task Block_disable_while_an_enabled_agent_references_a_current_tool()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgMcpServerDefinition),
            typeof(AgMcpServerArgument),
            typeof(AgMcpToolVersion),
            typeof(AgAgentDefinition),
            typeof(AgAgentVersion),
            typeof(AgAgentVersionBinding),
            typeof(AgAgentVersionSnapshot));
        var service = new AgMcpServerDefinitionServices(
            fixture.CreateRepository<AgMcpServerDefinition>(),
            new StubMcpToolDiscovery(
            [
                new DiscoveredMcpTool(
                    "query_business_data",
                    "Query business data",
                    "{\"type\":\"object\"}")
            ]));
        McpServerDefinition created = Assert.IsType<McpServerDefinition>((await service.CreateAsync(
            new CreateMcpServerCommand(
                $"mcp-{Guid.NewGuid():N}",
                "Referenced MCP",
                string.Empty,
                McpTransportKind.StreamableHttp,
                "https://localhost/mcp",
                string.Empty,
                [],
                string.Empty,
                true))).Data);
        McpServerDefinition synced = Assert.IsType<McpServerDefinition>((await service.SyncAsync(
            new SyncMcpServerCommand(created.Id, 0))).Data);
        McpToolVersion discovered = Assert.Single(synced.ToolVersions);
        McpServerDefinition classified = Assert.IsType<McpServerDefinition>((await service.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                created.Id,
                discovered.Id,
                synced.LogicalRevision,
                McpToolRisk.ReadOnly))).Data);
        Guid toolVersionId = Assert.Single(classified.CurrentToolVersionIds);
        Guid agentId = Guid.NewGuid();
        Guid agentVersionId = Guid.NewGuid();
        await fixture.Db.Insertable(new AgAgentDefinition
        {
            ID = agentId,
            Code = "mcp-consumer",
            Name = "MCP Consumer",
            Description = string.Empty,
            RuntimeStatus = "Enabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();
        await fixture.Db.Insertable(new AgAgentVersion
        {
            ID = agentVersionId,
            AgentId = agentId,
            Ordinal = 1,
            Label = "1.0.0",
            IsDraft = false,
            Instructions = string.Empty,
            ModelProfileId = string.Empty,
            OutputMode = "Text"
        }).ExecuteCommandAsync();
        await fixture.Db.Insertable(new AgAgentVersionSnapshot
        {
            ID = Guid.NewGuid(),
            VersionId = agentVersionId,
            SnapshotVersionId = agentVersionId,
            AgentCode = "mcp-consumer",
            AgentName = "MCP Consumer",
            AgentDescription = string.Empty,
            Instructions = string.Empty,
            ModelProfileId = string.Empty,
            OutputMode = "Text"
        }).ExecuteCommandAsync();
        await fixture.Db.Insertable(new AgAgentVersionBinding
        {
            ID = Guid.NewGuid(),
            VersionId = agentVersionId,
            Scope = "Snapshot",
            BindingType = "Tool",
            Ordinal = 0,
            ReferenceId = toolVersionId,
            ReferenceCode = "query_business_data",
            ReferenceName = "Query business data",
            ReferenceDescription = string.Empty
        }).ExecuteCommandAsync();
        var disable = new UpdateMcpServerCommand(
            classified.Id,
            classified.LogicalRevision,
            classified.Name,
            classified.Description,
            classified.Transport,
            classified.Endpoint,
            classified.Command,
            classified.Arguments,
            classified.CredentialAlias,
            false);

        ServiceResult<McpServerDefinition> blocked = await service.UpdateAsync(disable);

        Assert.False(blocked.Success);
        Assert.Equal(McpServiceStatusCodes.DisableBlocked, blocked.Status);
        Assert.Contains("mcp-consumer", blocked.Message);

        await fixture.Db.Updateable<AgAgentDefinition>()
            .SetColumns(value => value.RuntimeStatus == "Disabled")
            .Where(value => value.ID == agentId)
            .ExecuteCommandAsync();
        ServiceResult<McpServerDefinition> disabled = await service.UpdateAsync(disable);
        Assert.True(disabled.Success);
        Assert.False(disabled.Data.Enabled);
        Assert.Equal(McpServerStatus.Disabled, disabled.Data.Status);
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

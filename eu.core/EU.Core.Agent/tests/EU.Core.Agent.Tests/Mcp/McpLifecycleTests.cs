using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Mcp;

public sealed class McpLifecycleTests
{
    [Fact]
    public async Task Discovery_classification_agent_binding_and_publish_freeze_tool_version()
    {
        var mcpRepository = new InMemoryMcpServerRepository();
        var discovery = new FixedDiscovery(
            new DiscoveredMcpTool(
                "catalog.search",
                "Search the catalog",
                "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}}}"));
        var mcp = new McpLifecycleService(mcpRepository, discovery);

        McpServerDefinition server = Successful(await mcp.CreateAsync(
            new CreateMcpServerCommand(
                "Catalog Tools",
                "Catalog Tools",
                "Read-only catalog integration",
                McpTransportKind.StreamableHttp,
                "https://mcp.example.test/mcp",
                "",
                [],
                "",
                true)));
        server = Successful(await mcp.SyncAsync(
            new SyncMcpServerCommand(server.Id, server.LogicalRevision)));
        McpToolVersion unknown = Assert.Single(server.ToolVersions);
        Assert.Equal(McpToolRisk.Unknown, unknown.Risk);
        Assert.Empty(await mcpRepository.ListAsync());

        server = Successful(await mcp.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                server.Id,
                unknown.Id,
                server.LogicalRevision,
                McpToolRisk.ReadOnly)));
        PublishedMcpToolReference reference =
            Assert.Single(await mcpRepository.ListAsync());
        Assert.Equal(McpToolRisk.ReadOnly, reference.Risk);
        Assert.NotEqual(unknown.Id, reference.ToolVersionId);

        var agents = new InMemoryAgentRepository();
        var agentLifecycle = new AgentLifecycleService(
            agents,
            toolVersions: mcpRepository);
        AgentDefinition agent = Successful(await agentLifecycle.CreateAsync(
            new CreateAgentCommand("catalog-agent")));
        agent = Successful(await agentLifecycle.SaveDraftAsync(
            new SaveAgentDraftCommand(
                agent.Id,
                agent.LogicalRevision,
                "Search the catalog.",
                "qwen",
                AgentOutputMode.Text,
                null,
                ToolVersionIds: [reference.ToolVersionId])));
        agent = Successful(await agentLifecycle.PublishAsync(
            new PublishAgentCommand(agent.Id, agent.LogicalRevision)));

        AgentToolBindingSnapshot binding =
            Assert.Single(Assert.Single(agent.PublishedVersions).Snapshot!.Tools);
        Assert.Equal(reference.ToolVersionId, binding.ToolVersionId);

        server = Successful(await mcp.SyncAsync(
            new SyncMcpServerCommand(server.Id, server.LogicalRevision)));
        Assert.Equal(
            reference.ToolVersionId,
            Assert.Single(server.CurrentToolVersionIds));
    }

    [Fact]
    public async Task Configuration_rejects_embedded_credentials_and_plain_credential_values()
    {
        var lifecycle = new McpLifecycleService(
            new InMemoryMcpServerRepository(),
            new FixedDiscovery());

        McpOperationResult<McpServerDefinition> endpoint = await lifecycle.CreateAsync(
            new CreateMcpServerCommand(
                "unsafe",
                "",
                "",
                McpTransportKind.StreamableHttp,
                "https://user:password@mcp.example.test/mcp",
                "",
                [],
                "",
                true));
        McpOperationResult<McpServerDefinition> credential = await lifecycle.CreateAsync(
            new CreateMcpServerCommand(
                "unsafe-credential",
                "",
                "",
                McpTransportKind.StreamableHttp,
                "https://mcp.example.test/mcp",
                "",
                [],
                "plain-secret",
                true));

        Assert.Equal(McpErrorCodes.ConfigurationInvalid, endpoint.Error?.Code);
        Assert.Equal(McpErrorCodes.ConfigurationInvalid, credential.Error?.Code);
    }

    [Theory]
    [InlineData("http://127.0.0.1:3001/mcp")]
    [InlineData("http://mcp.internal.test/mcp")]
    [InlineData("https://mcp.example.test/mcp")]
    public async Task Configuration_accepts_absolute_http_and_https_endpoints(string endpoint)
    {
        var lifecycle = new McpLifecycleService(
            new InMemoryMcpServerRepository(),
            new FixedDiscovery());

        McpOperationResult<McpServerDefinition> result = await lifecycle.CreateAsync(
            new CreateMcpServerCommand(
                $"server-{Guid.NewGuid():N}",
                "",
                "",
                McpTransportKind.StreamableHttp,
                endpoint,
                "",
                [],
                "",
                true));

        Assert.True(result.Succeeded, result.Error?.Message);
    }

    [Fact]
    public async Task Agent_package_round_trip_preserves_classified_tool_version_reference()
    {
        var mcpRepository = new InMemoryMcpServerRepository();
        var mcp = new McpLifecycleService(
            mcpRepository,
            new FixedDiscovery(new DiscoveredMcpTool(
                "catalog.search",
                "Search",
                "{\"type\":\"object\",\"properties\":{}}")));
        McpServerDefinition server = Successful(await mcp.CreateAsync(
            new CreateMcpServerCommand(
                "portable-tools",
                "Portable",
                "",
                McpTransportKind.StreamableHttp,
                "https://mcp.example.test/mcp",
                "",
                [],
                "",
                true)));
        server = Successful(await mcp.SyncAsync(
            new SyncMcpServerCommand(server.Id, server.LogicalRevision)));
        server = Successful(await mcp.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                server.Id,
                Assert.Single(server.CurrentToolVersionIds),
                server.LogicalRevision,
                McpToolRisk.ReadOnly)));
        Guid toolVersionId = Assert.Single(server.CurrentToolVersionIds);
        var profiles = new PublicModelProfileCatalog(["qwen"]);

        var sourceRepository = new InMemoryAgentRepository();
        var sourceLifecycle = new AgentLifecycleService(
            sourceRepository,
            toolVersions: mcpRepository);
        AgentDefinition source = Successful(await sourceLifecycle.CreateAsync(
            new CreateAgentCommand("portable-mcp-agent")));
        source = Successful(await sourceLifecycle.SaveDraftAsync(
            new SaveAgentDraftCommand(
                source.Id,
                source.LogicalRevision,
                "Use tools.",
                "qwen",
                AgentOutputMode.Text,
                null,
                ToolVersionIds: [toolVersionId])));
        var sourcePackages = new AgentPackageService(
            sourceRepository,
            sourceLifecycle,
            profiles,
            toolVersions: mcpRepository);
        string package = Successful(await sourcePackages.ExportAsync(source.Id));

        var targetRepository = new InMemoryAgentRepository();
        var targetLifecycle = new AgentLifecycleService(
            targetRepository,
            toolVersions: mcpRepository);
        var targetPackages = new AgentPackageService(
            targetRepository,
            targetLifecycle,
            profiles,
            toolVersions: mcpRepository);
        AgentDefinition imported = Successful(await targetPackages.ImportAsync(package));

        Assert.Equal(toolVersionId, Assert.Single(imported.Draft.ToolVersionIds));
    }

    private static T Successful<T>(McpOperationResult<T> result) =>
        result.Succeeded ? result.Value! : throw new Xunit.Sdk.XunitException(result.Error?.Message);

    private static T Successful<T>(AgentOperationResult<T> result) =>
        result.Succeeded ? result.Value! : throw new Xunit.Sdk.XunitException(result.Error?.Message);

    private sealed class FixedDiscovery(params DiscoveredMcpTool[] tools) : IMcpToolDiscovery
    {
        public Task<IReadOnlyList<DiscoveredMcpTool>> DiscoverAsync(
            McpServerDefinition server,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<DiscoveredMcpTool>>(tools);
        }
    }
}

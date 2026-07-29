using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Mcp;

public sealed class SqliteMcpServerRepositoryTests
{
    [Fact]
    public async Task Repository_persists_server_and_immutable_tool_history()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"eu-core-agent-mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string database = Path.Combine(directory, "mcp.db");
        try
        {
            var first = new SqliteMcpServerRepository(database);
            var lifecycle = new McpLifecycleService(
                first,
                new FixedDiscovery());
            McpServerDefinition server = (await lifecycle.CreateAsync(
                new CreateMcpServerCommand(
                    "sqlite-mcp",
                    "SQLite MCP",
                    "",
                    McpTransportKind.StreamableHttp,
                    "https://mcp.example.test/mcp",
                    "",
                    [],
                    "",
                    true))).Value!;
            server = (await lifecycle.SyncAsync(
                new SyncMcpServerCommand(server.Id, server.LogicalRevision))).Value!;
            server = (await lifecycle.ClassifyToolAsync(
                new ClassifyMcpToolCommand(
                    server.Id,
                    Assert.Single(server.CurrentToolVersionIds),
                    server.LogicalRevision,
                    McpToolRisk.ReadOnly))).Value!;

            var reopened = new SqliteMcpServerRepository(database);
            McpServerDefinition? restoredValue =
                await reopened.GetByIdAsync(server.Id);
            Assert.NotNull(restoredValue);
            McpServerDefinition restored = restoredValue;

            Assert.Equal(server.LogicalRevision, restored.LogicalRevision);
            Assert.Equal(2, restored.ToolVersions.Count);
            Assert.True(await reopened.ExistsAsync(
                Assert.Single(restored.CurrentToolVersionIds)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FixedDiscovery : IMcpToolDiscovery
    {
        public Task<IReadOnlyList<DiscoveredMcpTool>> DiscoverAsync(
            McpServerDefinition server,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoveredMcpTool>>(
            [
                new(
                    "catalog.search",
                    "Search",
                    "{\"type\":\"object\",\"properties\":{}}")
            ]);
    }
}

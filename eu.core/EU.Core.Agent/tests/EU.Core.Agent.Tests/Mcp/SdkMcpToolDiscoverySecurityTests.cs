using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Infrastructure.Mcp;
using Xunit;

namespace EU.Core.Agent.Tests.Mcp;

public sealed class SdkMcpToolDiscoverySecurityTests
{
    [Fact]
    public async Task Http_discovery_rejects_an_unspecified_network_target()
    {
        var discovery = new SdkMcpToolDiscovery(new McpDiscoverySettings(
            [],
            [80],
            [],
            false,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1)));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => discovery.DiscoverAsync(Server(
                McpTransportKind.StreamableHttp,
                "http://0.0.0.0/mcp")));

        Assert.Contains("invalid network", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stdio_discovery_is_denied_when_feature_is_disabled()
    {
        var discovery = new SdkMcpToolDiscovery(new McpDiscoverySettings(
            [],
            [443],
            ["npx"],
            false,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1)));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => discovery.DiscoverAsync(Server(McpTransportKind.Stdio, "", "npx")));
    }

    private static McpServerDefinition Server(
        McpTransportKind transport,
        string endpoint,
        string command = "") =>
        new(
            Guid.NewGuid(),
            "test-mcp",
            "Test",
            "",
            transport,
            endpoint,
            command,
            [],
            "",
            true,
            0,
            McpServerStatus.NotSynced,
            "",
            null,
            [],
            []);
}

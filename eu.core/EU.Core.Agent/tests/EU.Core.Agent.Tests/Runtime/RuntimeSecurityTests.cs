using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Agent.Runtime;
using Xunit;

namespace EU.Core.Agent.Tests.Runtime;

public sealed class RuntimeSecurityTests
{
    [Theory]
    [InlineData(McpToolRisk.Unknown)]
    [InlineData(McpToolRisk.Mutating)]
    [InlineData(McpToolRisk.HighRisk)]
    public async Task Non_readonly_tools_are_blocked_before_repository_or_network(
        McpToolRisk risk)
    {
        var discovery = new SdkMcpToolDiscovery(new McpDiscoverySettings(
            [],
            [443],
            [],
            false,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1)));
        var invoker = new SdkMcpRuntimeToolInvoker(
            new ThrowingRepository(),
            discovery,
            TimeSpan.FromSeconds(1));

        McpRuntimeToolResult result = await invoker.InvokeAsync(
            Guid.NewGuid(),
            risk,
            new Dictionary<string, object?>());

        Assert.True(result.Blocked);
        Assert.Equal(AgentRunErrorCodes.ToolBlocked, result.ErrorCode);
    }

    [Fact]
    public async Task Model_credential_resolver_reads_only_supported_secret_names()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"agent-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory, ".env"),
                """
                UNRELATED_SECRET=must-not-be-used
                AGENT_MODEL_API_KEY=runtime-key
                """);
            var resolver =
                new EnvironmentAndDotEnvModelCredentialResolver(directory);

            Assert.Equal(
                "runtime-key",
                await resolver.ResolveAsync("alias:development-agent-model"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class ThrowingRepository : IMcpServerRepository
    {
        public Task<McpServerDefinition?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new Xunit.Sdk.XunitException("Repository must not be accessed.");

        public Task<IReadOnlyList<McpServerDefinition>> ListAsync(
            McpServerQuery query,
            CancellationToken cancellationToken = default) =>
            throw new Xunit.Sdk.XunitException("Repository must not be accessed.");

        public Task<bool> TryCreateAsync(
            McpServerDefinition definition,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryReplaceAsync(
            McpServerDefinition definition,
            long expectedLogicalRevision,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

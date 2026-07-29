using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using EU.Core.Agent.Application.Mcp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EU.Core.Agent.Tests.Api;

public sealed class McpApiTests
{
    [Fact]
    public async Task Controller_api_manages_discovery_risk_and_agent_tool_snapshot()
    {
        using TestHost host = CreateHost();
        using HttpResponseMessage created = await host.Client.PostAsJsonAsync(
            "/api/mcp/servers",
            new
            {
                code = "catalog-tools",
                name = "Catalog Tools",
                description = "Search",
                transport = "StreamableHttp",
                endpoint = "https://mcp.example.test/mcp",
                command = "",
                arguments = Array.Empty<string>(),
                credentialAlias = "",
                enabled = true
            });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        JsonElement server = await ReadJson(created);
        string serverId = server.GetProperty("id").GetString()!;

        using HttpResponseMessage synced = await host.Client.PostAsJsonAsync(
            $"/api/mcp/servers/{serverId}/sync",
            new { expectedLogicalRevision = 0 });
        Assert.Equal(HttpStatusCode.OK, synced.StatusCode);
        server = await ReadJson(synced);
        string unknownId = Assert.Single(
            server.GetProperty("currentToolVersionIds").EnumerateArray()).GetString()!;

        using HttpResponseMessage classified = await host.Client.PutAsJsonAsync(
            $"/api/mcp/servers/{serverId}/tools/{unknownId}/risk",
            new { expectedLogicalRevision = 1, risk = "ReadOnly" });
        Assert.Equal(HttpStatusCode.OK, classified.StatusCode);
        server = await ReadJson(classified);
        string toolVersionId = Assert.Single(
            server.GetProperty("currentToolVersionIds").EnumerateArray()).GetString()!;

        using HttpResponseMessage catalog = await host.Client.GetAsync(
            "/api/mcp/tool-versions");
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        Assert.Equal(
            toolVersionId,
            Assert.Single((await ReadJson(catalog)).EnumerateArray())
                .GetProperty("toolVersionId").GetString());

        using HttpResponseMessage agentCreated = await host.Client.PostAsJsonAsync(
            "/api/agents",
            new { code = "mcp-agent", name = "MCP Agent", description = "" });
        JsonElement agent = await ReadJson(agentCreated);
        string agentId = agent.GetProperty("id").GetString()!;
        using HttpResponseMessage saved = await host.Client.PutAsJsonAsync(
            $"/api/agents/{agentId}/draft",
            new
            {
                expectedLogicalRevision = 0,
                name = "MCP Agent",
                description = "",
                instructions = "Use the catalog.",
                modelProfileId = "qwen-safe",
                outputMode = "Text",
                outputJsonSchema = (string?)null,
                skillVersionIds = Array.Empty<string>(),
                toolVersionIds = new[] { toolVersionId }
            });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        using HttpResponseMessage published = await host.Client.PostAsJsonAsync(
            $"/api/agents/{agentId}/publish",
            new { expectedLogicalRevision = 1 });
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        JsonElement snapshot = Assert.Single(
                (await ReadJson(published)).GetProperty("publishedVersions").EnumerateArray())
            .GetProperty("snapshot");
        Assert.Equal(
            toolVersionId,
            Assert.Single(snapshot.GetProperty("tools").EnumerateArray())
                .GetProperty("toolVersionId").GetString());
    }

    [Fact]
    public async Task Run_endpoint_does_not_exist()
    {
        using TestHost host = CreateHost();
        using HttpResponseMessage response = await host.Client.PostAsJsonAsync(
            $"/api/mcp/servers/{Guid.NewGuid()}/run",
            new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static TestHost CreateHost()
    {
        var variables = new EnvironmentVariableScope(
            ("AgentPlatform__ServiceName", "agent-mcp-api"),
            ("AgentPlatform__ModelEndpoint", "https://model.example.test/v1"),
            ("AgentPlatform__ModelCredentialAlias", "alias:development-agent-model"),
            ("AgentStorage__Provider", "InMemory"),
            ("AgentControl__ModelProfileIds__0", "qwen-safe"));
        Type entryPoint = Assembly.Load("EU.Core.Agent.Api").GetType("Program")!;
        Type factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);
        dynamic factory = Activator.CreateInstance(factoryType)!;
        factory = factory.WithWebHostBuilder((Action<IWebHostBuilder>)(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMcpToolDiscovery>();
                services.AddSingleton<IMcpToolDiscovery, FixedDiscovery>();
            })));
        HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return new TestHost(client, (IDisposable)factory, variables);
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
                    "Search the catalog",
                    "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}}}")
            ]);
    }

    private sealed record TestHost(
        HttpClient Client,
        IDisposable Factory,
        EnvironmentVariableScope Variables) : IDisposable
    {
        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
            Variables.Dispose();
        }
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _original;

        public EnvironmentVariableScope(params (string Name, string? Value)[] values)
        {
            _original = values.ToDictionary(
                value => value.Name,
                value => Environment.GetEnvironmentVariable(value.Name),
                StringComparer.Ordinal);
            foreach ((string name, string? value) in values)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach ((string name, string? value) in _original)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}

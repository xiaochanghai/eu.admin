using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EU.Core.Agent.Tests.Api;

public sealed class SkillApiTests
{
    [Fact]
    public async Task Skill_files_publish_catalog_and_agent_binding_work_end_to_end()
    {
        using TestHost host = CreateHost();

        using HttpResponseMessage create = await host.Client.PostAsJsonAsync("/api/skills", new
        {
            code = "employee-handbook",
            name = "Employee Handbook",
            description = "Answers employee questions",
            category = "HR"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        JsonElement created = await ReadJson(create);
        Guid skillId = created.GetProperty("id").GetGuid();

        JsonElement files = await host.Client.GetFromJsonAsync<JsonElement>(
            $"/api/skills/{skillId}/files");
        Assert.Equal("SKILL.md", Assert.Single(files.EnumerateArray()).GetProperty("path").GetString());

        JsonElement skillFile = await host.Client.GetFromJsonAsync<JsonElement>(
            $"/api/skills/{skillId}/files/content?path=SKILL.md");
        Assert.Contains(
            "name: employee-handbook",
            skillFile.GetProperty("content").GetString(),
            StringComparison.Ordinal);

        using HttpResponseMessage save = await host.Client.PutAsJsonAsync(
            $"/api/skills/{skillId}/files/content",
            new
            {
                expectedDraftRevision = 0,
                path = "references/pay.md",
                content = "# Pay"
            });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        Assert.Equal(1, (await ReadJson(save)).GetProperty("draftRevision").GetInt64());

        using HttpResponseMessage publish = await host.Client.PostAsJsonAsync(
            $"/api/skills/{skillId}/publish",
            new { expectedDraftRevision = 1, versionLabel = "1.0.0" });
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        JsonElement published = await ReadJson(publish);
        JsonElement version = Assert.Single(
            published.GetProperty("publishedVersions").EnumerateArray());
        Guid versionId = version.GetProperty("id").GetGuid();
        Assert.Equal(64, version.GetProperty("manifestSha256").GetString()?.Length);

        JsonElement catalog = await host.Client.GetFromJsonAsync<JsonElement>(
            "/api/skill-versions");
        Assert.Equal(versionId, Assert.Single(catalog.EnumerateArray()).GetProperty("versionId").GetGuid());

        using HttpResponseMessage agentCreate = await host.Client.PostAsJsonAsync("/api/agents", new
        {
            code = "hr-agent",
            name = "HR Agent",
            description = "HR support"
        });
        Guid agentId = (await ReadJson(agentCreate)).GetProperty("id").GetGuid();
        using HttpResponseMessage agentSave = await host.Client.PutAsJsonAsync(
            $"/api/agents/{agentId}/draft",
            new
            {
                expectedLogicalRevision = 0,
                name = "HR Agent",
                description = "HR support",
                instructions = "Answer HR questions.",
                modelProfileId = "qwen-safe",
                outputMode = "Text",
                outputJsonSchema = (string?)null,
                skillVersionIds = new[] { versionId }
            });
        Assert.Equal(HttpStatusCode.OK, agentSave.StatusCode);
        using HttpResponseMessage agentPublish = await host.Client.PostAsJsonAsync(
            $"/api/agents/{agentId}/publish",
            new { expectedLogicalRevision = 1 });
        JsonElement agent = await ReadJson(agentPublish);
        JsonElement agentVersion = Assert.Single(
            agent.GetProperty("publishedVersions").EnumerateArray());
        Assert.Equal(
            versionId,
            Assert.Single(
                agentVersion.GetProperty("snapshot").GetProperty("skills").EnumerateArray())
                .GetProperty("skillVersionId")
                .GetGuid());
    }

    [Fact]
    public async Task Skill_api_rejects_stale_revision_path_escape_and_script_execution()
    {
        using TestHost host = CreateHost();
        using HttpResponseMessage create = await host.Client.PostAsJsonAsync("/api/skills", new
        {
            code = "safe-skill",
            name = "Safe",
            description = "Safe",
            category = "General"
        });
        Guid id = (await ReadJson(create)).GetProperty("id").GetGuid();

        using HttpResponseMessage escape = await host.Client.PutAsJsonAsync(
            $"/api/skills/{id}/files/content",
            new { expectedDraftRevision = 0, path = "../outside.md", content = "blocked" });
        Assert.Equal(HttpStatusCode.BadRequest, escape.StatusCode);
        Assert.Equal("SKILL_PATH_INVALID", (await ReadJson(escape)).GetProperty("errorCode").GetString());

        using HttpResponseMessage script = await host.Client.PutAsJsonAsync(
            $"/api/skills/{id}/files/content",
            new { expectedDraftRevision = 0, path = "scripts/stored.py", content = "print('stored')" });
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);

        using HttpResponseMessage stale = await host.Client.PutAsJsonAsync(
            $"/api/skills/{id}/files/content",
            new { expectedDraftRevision = 0, path = "references/stale.md", content = "stale" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(
            "SKILL_DRAFT_REVISION_CONFLICT",
            (await ReadJson(stale)).GetProperty("errorCode").GetString());

        using HttpResponseMessage run = await host.Client.PostAsJsonAsync(
            $"/api/skills/{id}/run",
            new { });
        Assert.Equal(HttpStatusCode.NotFound, run.StatusCode);
    }

    private static TestHost CreateHost()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"eu-core-agent-skill-api-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var variables = new EnvironmentVariableScope(
            ("AgentPlatform__ServiceName", "agent-api"),
            ("AgentPlatform__ModelEndpoint", "https://model.example.test/v1"),
            ("AgentPlatform__ModelCredentialAlias", "alias:development-agent-model"),
            ("AgentPlatform__ModelApiKey", null),
            ("AgentStorage__Provider", "InMemory"),
            ("AgentStorage__SkillRootPath", root),
            ("AgentControl__ModelProfileIds__0", "qwen-safe"));
        try
        {
            Type entryPoint = Assembly.Load("EU.Core.Agent.Api").GetType("Program")
                ?? throw new InvalidOperationException("The API entry point was not found.");
            Type factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);
            IDisposable factory = (IDisposable)(Activator.CreateInstance(factoryType)
                ?? throw new InvalidOperationException("The API test host could not be created."));
            MethodInfo createClient = factoryType.GetMethod(
                "CreateClient",
                [typeof(WebApplicationFactoryClientOptions)])
                ?? throw new InvalidOperationException("The API test host has no client factory.");
            HttpClient client = (HttpClient)createClient.Invoke(
                factory,
                [new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }])!;
            return new TestHost(client, factory, variables, root);
        }
        catch
        {
            variables.Dispose();
            Directory.Delete(root, recursive: true);
            throw;
        }
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private sealed class TestHost(
        HttpClient client,
        IDisposable factory,
        EnvironmentVariableScope variables,
        string root) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public void Dispose()
        {
            Client.Dispose();
            factory.Dispose();
            variables.Dispose();
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
            }
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues;

        public EnvironmentVariableScope(params (string Name, string? Value)[] values)
        {
            _originalValues = values.ToDictionary(
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
            foreach ((string name, string? value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}

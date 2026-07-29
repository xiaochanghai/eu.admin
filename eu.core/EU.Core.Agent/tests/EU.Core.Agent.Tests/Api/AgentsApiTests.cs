using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EU.Core.Agent.Tests.Api;

public sealed class AgentsApiTests
{
    [Fact]
    public async Task Empty_create_detail_and_separate_list_requests_share_the_singleton_store()
    {
        using TestHost host = CreateHost();

        using HttpResponseMessage empty = await host.Client.GetAsync("/api/agents");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal(0, (await ReadJson(empty)).GetArrayLength());

        using HttpResponseMessage created = await host.Client.PostAsJsonAsync("/api/agents", new
        {
            code = "  Support_Bot ",
            name = "Support <img src=x onerror=alert(1)>",
            description = "</script><script>alert('description')</script>"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        JsonElement createdAgent = await ReadJson(created);
        string id = createdAgent.GetProperty("id").GetString()!;
        Assert.Equal("support-bot", createdAgent.GetProperty("code").GetString());
        Assert.Equal("Support <img src=x onerror=alert(1)>", createdAgent.GetProperty("name").GetString());
        Assert.Equal("</script><script>alert('description')</script>", createdAgent.GetProperty("description").GetString());
        Assert.Equal("Enabled", createdAgent.GetProperty("runtimeStatus").GetString());
        Assert.Equal("0.1.0", createdAgent.GetProperty("draft").GetProperty("label").GetString());

        using HttpResponseMessage detail = await host.Client.GetAsync($"/api/agents/{id}");
        using HttpResponseMessage list = await host.Client.GetAsync("/api/agents?search=SUPPORT&status=Enabled");

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal("Support <img src=x onerror=alert(1)>", (await ReadJson(detail)).GetProperty("name").GetString());
        JsonElement listed = Assert.Single((await ReadJson(list)).EnumerateArray());
        Assert.Equal(id, listed.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Save_publish_status_and_version_history_follow_revision_and_string_enum_contracts()
    {
        using TestHost host = CreateHost();
        string id = await CreateAgent(host.Client, "publisher", "Publisher", "Publishes");

        using HttpResponseMessage saved = await host.Client.PutAsJsonAsync($"/api/agents/{id}/draft", new
        {
            expectedLogicalRevision = 0,
            name = "Publisher v2",
            description = "Updated responsibility",
            instructions = "Return safe JSON.",
            modelProfileId = "qwen-safe",
            outputMode = "Structured",
            outputJsonSchema = "{\"required\":[\"answer\"],\"properties\":{\"answer\":{\"type\":\"string\"}},\"type\":\"object\"}"
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(1, (await ReadJson(saved)).GetProperty("logicalRevision").GetInt64());

        using HttpResponseMessage published = await host.Client.PostAsJsonAsync($"/api/agents/{id}/publish", new
        {
            expectedLogicalRevision = 1
        });
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        JsonElement publishedAgent = await ReadJson(published);
        Assert.Equal(2, publishedAgent.GetProperty("logicalRevision").GetInt64());
        JsonElement version = Assert.Single(publishedAgent.GetProperty("publishedVersions").EnumerateArray());
        Assert.Equal("1.0.0", version.GetProperty("label").GetString());
        Assert.Equal(0, version.GetProperty("snapshot").GetProperty("skills").GetArrayLength());
        Assert.Equal(0, version.GetProperty("snapshot").GetProperty("tools").GetArrayLength());

        using HttpResponseMessage disabled = await host.Client.PutAsJsonAsync($"/api/agents/{id}/status", new
        {
            expectedLogicalRevision = 2,
            runtimeStatus = "Disabled"
        });
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("Disabled", (await ReadJson(disabled)).GetProperty("runtimeStatus").GetString());

        using HttpResponseMessage editedAgain = await host.Client.PutAsJsonAsync($"/api/agents/{id}/draft", new
        {
            expectedLogicalRevision = 3,
            name = "Publisher v3",
            description = "Second published responsibility",
            instructions = "Return safe JSON version two.",
            modelProfileId = "structured-safe",
            outputMode = "Structured",
            outputJsonSchema = "{\"type\":\"object\",\"properties\":{\"answer\":{\"type\":\"string\"}},\"required\":[\"answer\"]}"
        });
        Assert.Equal(HttpStatusCode.OK, editedAgain.StatusCode);
        using HttpResponseMessage publishedAgain = await host.Client.PostAsJsonAsync($"/api/agents/{id}/publish", new
        {
            expectedLogicalRevision = 4
        });
        Assert.Equal(HttpStatusCode.OK, publishedAgain.StatusCode);

        using HttpResponseMessage detail = await host.Client.GetAsync($"/api/agents/{id}");
        JsonElement current = await ReadJson(detail);
        Assert.Equal("Publisher v3", current.GetProperty("name").GetString());
        Assert.Equal("Server", current.GetProperty("deploymentTarget").GetString());
        Assert.Equal("EU.Core.Agent.Api", current.GetProperty("host").GetString());
        Assert.Equal(
            new string?[] { "1.0.0", "2.0.0" },
            current.GetProperty("publishedVersions").EnumerateArray()
                .Select(item => item.GetProperty("label").GetString())
                .ToArray());
    }

    [Fact]
    public async Task Typed_failures_are_safe_problem_details_with_error_code_and_correlation()
    {
        using TestHost host = CreateHost();
        string id = await CreateAgent(host.Client, "conflict", "Conflict", "");

        using HttpRequestMessage staleRequest = new(HttpMethod.Put, $"/api/agents/{id}/draft")
        {
            Content = JsonContent.Create(new
            {
                expectedLogicalRevision = 99,
                name = "unsaved hostile </script>",
                description = "",
                instructions = "",
                modelProfileId = "",
                outputMode = "Text",
                outputJsonSchema = (string?)null
            })
        };
        staleRequest.Headers.Add("X-Correlation-ID", "agent-api-stale-42");
        using HttpResponseMessage stale = await host.Client.SendAsync(staleRequest);

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.StartsWith("application/problem+json", stale.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        JsonElement staleProblem = await ReadJson(stale);
        Assert.Equal("AGENT_ROW_VERSION_CONFLICT", staleProblem.GetProperty("errorCode").GetString());
        Assert.Equal("agent-api-stale-42", staleProblem.GetProperty("traceId").GetString());
        Assert.DoesNotContain("unsaved hostile", staleProblem.GetRawText(), StringComparison.Ordinal);

        using HttpResponseMessage missing = await host.Client.GetAsync($"/api/agents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("AGENT_NOT_FOUND", (await ReadJson(missing)).GetProperty("errorCode").GetString());

        using HttpResponseMessage duplicate = await host.Client.PostAsJsonAsync("/api/agents", new
        {
            code = "conflict",
            name = "Duplicate",
            description = ""
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("AGENT_CODE_CONFLICT", (await ReadJson(duplicate)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Invalid_enum_stale_publish_and_invalid_structured_schema_map_to_bounded_400_or_409_problems()
    {
        using TestHost host = CreateHost();
        string id = await CreateAgent(host.Client, "validation", "Validation", "");

        using HttpResponseMessage invalidEnum = await host.Client.PutAsJsonAsync($"/api/agents/{id}/status", new
        {
            expectedLogicalRevision = 0,
            runtimeStatus = "Canary"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEnum.StatusCode);
        Assert.Equal("REQUEST_INVALID", (await ReadJson(invalidEnum)).GetProperty("errorCode").GetString());

        using HttpResponseMessage numericEnum = await host.Client.PutAsync(
            $"/api/agents/{id}/status",
            new StringContent(
                "{\"expectedLogicalRevision\":0,\"runtimeStatus\":0}",
                Encoding.UTF8,
                "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, numericEnum.StatusCode);
        Assert.Equal("REQUEST_INVALID", (await ReadJson(numericEnum)).GetProperty("errorCode").GetString());

        using HttpResponseMessage unknownProfile = await host.Client.PutAsJsonAsync($"/api/agents/{id}/draft", new
        {
            expectedLogicalRevision = 0,
            name = "Validation",
            description = "",
            instructions = "Answer.",
            modelProfileId = "not-configured",
            outputMode = "Text",
            outputJsonSchema = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknownProfile.StatusCode);
        Assert.Equal("AGENT_REFERENCE_MISSING", (await ReadJson(unknownProfile)).GetProperty("errorCode").GetString());

        using HttpResponseMessage invalidSchemaSave = await host.Client.PutAsJsonAsync($"/api/agents/{id}/draft", new
        {
            expectedLogicalRevision = 0,
            name = "Validation",
            description = "",
            instructions = "Answer.",
            modelProfileId = "qwen-safe",
            outputMode = "Structured",
            outputJsonSchema = "{\"type\":\"object\",\"required\":[\"missing\"],\"properties\":{}}"
        });
        Assert.Equal(HttpStatusCode.OK, invalidSchemaSave.StatusCode);

        using HttpResponseMessage invalidPublish = await host.Client.PostAsJsonAsync($"/api/agents/{id}/publish", new
        {
            expectedLogicalRevision = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPublish.StatusCode);
        JsonElement invalidPublishProblem = await ReadJson(invalidPublish);
        Assert.Equal("OUTPUT_SCHEMA_INVALID", invalidPublishProblem.GetProperty("errorCode").GetString());
        Assert.Equal(
            "Schema required must contain unique known property names.",
            invalidPublishProblem.GetProperty("detail").GetString());

        using HttpResponseMessage stalePublish = await host.Client.PostAsJsonAsync($"/api/agents/{id}/publish", new
        {
            expectedLogicalRevision = 0
        });
        Assert.Equal(HttpStatusCode.Conflict, stalePublish.StatusCode);
        Assert.Equal("AGENT_ROW_VERSION_CONFLICT", (await ReadJson(stalePublish)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Export_and_import_use_json_download_round_trip_and_do_not_overwrite()
    {
        using TestHost source = CreateHost();
        string id = await CreateAgent(source.Client, "portable", "Portable", "Round trip");
        using HttpResponseMessage saved = await source.Client.PutAsJsonAsync($"/api/agents/{id}/draft", new
        {
            expectedLogicalRevision = 0,
            name = "Portable",
            description = "Round trip",
            instructions = "Portable instructions",
            modelProfileId = "qwen-safe",
            outputMode = "Text",
            outputJsonSchema = (string?)null
        });
        saved.EnsureSuccessStatusCode();

        using HttpResponseMessage exported = await source.Client.GetAsync($"/api/agents/{id}/export");
        Assert.Equal(HttpStatusCode.OK, exported.StatusCode);
        Assert.Equal("application/json", exported.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", exported.Content.Headers.ContentDisposition?.DispositionType);
        string package = await exported.Content.ReadAsStringAsync();
        Assert.Contains("\"format\":\"eu.core.agent-package\"", package, StringComparison.Ordinal);

        using TestHost target = CreateHost();
        using HttpResponseMessage imported = await target.Client.PostAsync(
            "/api/agents/import",
            new StringContent(package, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, imported.StatusCode);
        Assert.Equal("portable", (await ReadJson(imported)).GetProperty("code").GetString());

        using HttpResponseMessage conflict = await target.Client.PostAsync(
            "/api/agents/import",
            new StringContent(package, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("AGENT_CODE_CONFLICT", (await ReadJson(conflict)).GetProperty("errorCode").GetString());

        using HttpResponseMessage wrongContentType = await target.Client.PostAsync(
            "/api/agents/import",
            new StringContent(package, Encoding.UTF8, "text/plain"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, wrongContentType.StatusCode);
    }

    [Fact]
    public async Task Capabilities_expose_public_allowlist_and_p4_mcp_feature_fact()
    {
        using TestHost host = CreateHost();

        using HttpResponseMessage response = await host.Client.GetAsync("/api/platform/capabilities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement capabilities = await ReadJson(response);
        Assert.Equal("memory", capabilities.GetProperty("storageMode").GetString());
        Assert.True(capabilities.GetProperty("volatile").GetBoolean());
        Assert.Equal("Server", capabilities.GetProperty("deployment").GetProperty("target").GetString());
        Assert.Equal("EU.Core.Agent.Api", capabilities.GetProperty("deployment").GetProperty("host").GetString());
        Assert.Equal(
            new string?[] { "qwen-safe", "structured-safe" },
            capabilities.GetProperty("modelProfileIds").EnumerateArray().Select(value => value.GetString()).ToArray());
        JsonElement features = capabilities.GetProperty("features");
        Assert.True(features.GetProperty("agentControl").GetBoolean());
        Assert.True(features.GetProperty("runtime").GetBoolean());
        Assert.True(features.GetProperty("skills").GetBoolean());
        Assert.True(features.GetProperty("mcp").GetBoolean());
        Assert.True(features.GetProperty("knowledge").GetBoolean());
        Assert.True(features.GetProperty("orchestration").GetBoolean());
        Assert.False(features.GetProperty("schedules").GetBoolean());
        string json = capabilities.GetRawText();
        Assert.DoesNotContain("endpoint", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("alias:production-credential")]
    [InlineData("C:\\private\\model.json")]
    [InlineData("/etc/private/model.json")]
    [InlineData("https://models.example.test/v1")]
    [InlineData("sk-live-value")]
    [InlineData("Bearer-live-value")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9")]
    [InlineData("password-model")]
    [InlineData("apiKey-model")]
    [InlineData("token-model")]
    [InlineData("connection-model")]
    [InlineData("org//model")]
    [InlineData("org/../model")]
    public void Unsafe_model_profile_configuration_stops_the_host_without_echoing_the_value(string value)
    {
        Exception? exception = Record.Exception(() =>
        {
            using TestHost host = CreateHost([value]);
            _ = host.Client;
        });

        Assert.NotNull(exception);
        Assert.DoesNotContain(value, exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("DELETE", "/api/agents/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/agents/00000000-0000-0000-0000-000000000001/run")]
    [InlineData("PUT", "/api/agents/00000000-0000-0000-0000-000000000001/skills")]
    [InlineData("POST", "/api/schedules")]
    public async Task Delete_run_and_deferred_write_routes_do_not_exist(string method, string path)
    {
        using TestHost host = CreateHost();
        using HttpRequestMessage request = new(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new { })
        };

        using HttpResponseMessage response = await host.Client.SendAsync(request);

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Agent_and_import_request_bodies_are_size_bounded_without_unbounded_buffering()
    {
        using TestHost host = CreateHost();
        string oversized = new('x', 140_000);

        using HttpResponseMessage create = await host.Client.PostAsJsonAsync("/api/agents", new
        {
            code = "oversized",
            name = oversized,
            description = ""
        });
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, create.StatusCode);
        Assert.Equal("REQUEST_BODY_TOO_LARGE", (await ReadJson(create)).GetProperty("errorCode").GetString());

        using HttpResponseMessage import = await host.Client.PostAsync(
            "/api/agents/import",
            new StringContent($"{{\"payload\":\"{oversized}\"}}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, import.StatusCode);
        Assert.Equal("REQUEST_BODY_TOO_LARGE", (await ReadJson(import)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Unknown_or_understated_length_streaming_body_is_stopped_by_the_counting_stream()
    {
        using TestHost host = CreateHost();
        string json = JsonSerializer.Serialize(new
        {
            code = "streamed-oversized",
            name = new string('x', 140_000),
            description = ""
        });
        using var content = new UnknownLengthJsonContent(json);
        Assert.Null(content.Headers.ContentLength);
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/agents")
        {
            Content = content
        };

        using HttpResponseMessage response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("REQUEST_BODY_TOO_LARGE", (await ReadJson(response)).GetProperty("errorCode").GetString());

        using var understatedContent = new UnknownLengthJsonContent(json);
        understatedContent.Headers.ContentLength = 1;
        using HttpRequestMessage understatedRequest = new(HttpMethod.Post, "/api/agents")
        {
            Content = understatedContent
        };
        using HttpResponseMessage understatedResponse = await host.Client.SendAsync(understatedRequest);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, understatedResponse.StatusCode);
        Assert.Equal(
            "REQUEST_BODY_TOO_LARGE",
            (await ReadJson(understatedResponse)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Run_routes_reject_missing_or_unpublished_agents_before_model_access()
    {
        using TestHost host = CreateHost();
        using HttpResponseMessage missing = await host.Client.PostAsJsonAsync(
            $"/api/agents/{Guid.NewGuid()}/runs",
            new { input = "hello" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(
            "AGENT_NOT_FOUND",
            (await ReadJson(missing)).GetProperty("errorCode").GetString());

        string id = await CreateAgent(
            host.Client,
            $"runtime-{Guid.NewGuid():N}",
            "Runtime",
            "");
        using HttpResponseMessage unpublished = await host.Client.PostAsJsonAsync(
            $"/api/agents/{id}/runs",
            new { input = "hello" });
        Assert.Equal(HttpStatusCode.BadRequest, unpublished.StatusCode);
        Assert.Equal(
            "AGENT_PUBLISHED_VERSION_MISSING",
            (await ReadJson(unpublished)).GetProperty("errorCode").GetString());

        JsonElement history = await ReadJson(
            await host.Client.GetAsync($"/api/agents/{id}/runs"));
        Assert.Equal(0, history.GetArrayLength());
    }

    [Fact]
    public async Task Published_run_streams_started_and_sanitized_failed_terminal_and_audits_it()
    {
        using TestHost host = CreateHost();
        string id = await CreateAgent(
            host.Client,
            $"runtime-stream-{Guid.NewGuid():N}",
            "Runtime stream",
            "");
        JsonElement created = await ReadJson(
            await host.Client.GetAsync($"/api/agents/{id}"));
        using HttpResponseMessage savedResponse = await host.Client.PutAsJsonAsync(
            $"/api/agents/{id}/draft",
            new
            {
                expectedLogicalRevision =
                    created.GetProperty("logicalRevision").GetInt64(),
                name = "Runtime stream",
                description = "",
                instructions = "Answer the user.",
                modelProfileId = "qwen-safe",
                outputMode = "Text",
                outputJsonSchema = (string?)null,
                skillVersionIds = Array.Empty<Guid>(),
                toolVersionIds = Array.Empty<Guid>()
            });
        savedResponse.EnsureSuccessStatusCode();
        JsonElement saved = await ReadJson(savedResponse);
        using HttpResponseMessage publishedResponse = await host.Client.PostAsJsonAsync(
            $"/api/agents/{id}/publish",
            new
            {
                expectedLogicalRevision =
                    saved.GetProperty("logicalRevision").GetInt64()
            });
        publishedResponse.EnsureSuccessStatusCode();

        using HttpResponseMessage run = await host.Client.PostAsJsonAsync(
            $"/api/agents/{id}/runs",
            new { input = "hello" });
        string stream = await run.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, run.StatusCode);
        Assert.Equal("text/event-stream", run.Content.Headers.ContentType?.MediaType);
        Assert.Contains("event: started", stream, StringComparison.Ordinal);
        Assert.Contains("event: failed", stream, StringComparison.Ordinal);
        Assert.Contains("MODEL_CREDENTIAL_MISSING", stream, StringComparison.Ordinal);
        Assert.DoesNotContain("api key", stream, StringComparison.OrdinalIgnoreCase);

        JsonElement history = await ReadJson(
            await host.Client.GetAsync($"/api/agents/{id}/runs"));
        JsonElement audit = Assert.Single(history.EnumerateArray());
        Assert.Equal("Failed", audit.GetProperty("status").GetString());
        Assert.Equal(
            "MODEL_CREDENTIAL_MISSING",
            audit.GetProperty("errorCode").GetString());
        Assert.False(audit.TryGetProperty("input", out _));
        Assert.False(audit.TryGetProperty("output", out _));
    }

    private static async Task<string> CreateAgent(HttpClient client, string code, string name, string description)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/agents", new { code, name, description });
        response.EnsureSuccessStatusCode();
        return (await ReadJson(response)).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static TestHost CreateHost(string[]? modelProfileIds = null)
    {
        modelProfileIds ??= ["qwen-safe", "structured-safe"];
        var configuredValues = new List<(string Name, string? Value)>
        {
            ("AgentPlatform__ServiceName", "agent-api"),
            ("AgentPlatform__ModelEndpoint", "https://model.example.test/v1"),
            ("AgentPlatform__ModelCredentialAlias", "alias:development-agent-model"),
            ("AgentPlatform__ModelApiKey", null),
            ("AGENT_MODEL_API_KEY", null),
            ("AgentStorage__Provider", "InMemory")
        };
        for (int index = 0; index < modelProfileIds.Length; index++)
        {
            configuredValues.Add(($"AgentControl__ModelProfileIds__{index}", modelProfileIds[index]));
        }

        EnvironmentVariableScope variables = new(configuredValues.ToArray());
        try
        {
            Type entryPoint = Assembly.Load("EU.Core.Agent.Api").GetType("Program")
                ?? throw new InvalidOperationException("The API entry point was not found.");
            Type factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);
            IDisposable factory = (IDisposable)(Activator.CreateInstance(factoryType)
                ?? throw new InvalidOperationException("The API test host could not be created."));
            MethodInfo createClient = factoryType.GetMethod("CreateClient", [typeof(WebApplicationFactoryClientOptions)])
                ?? throw new InvalidOperationException("The API test host has no client factory.");
            HttpClient client = (HttpClient)createClient.Invoke(factory, [new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }])!;
            return new TestHost(client, factory, variables);
        }
        catch
        {
            variables.Dispose();
            throw;
        }
    }

    private sealed class TestHost(HttpClient client, IDisposable factory, EnvironmentVariableScope variables) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public void Dispose()
        {
            Client.Dispose();
            factory.Dispose();
            variables.Dispose();
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

    private sealed class UnknownLengthJsonContent : HttpContent
    {
        private readonly byte[] _payload;

        public UnknownLengthJsonContent(string json)
        {
            _payload = Encoding.UTF8.GetBytes(json);
            Headers.TryAddWithoutValidation("Content-Type", "application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_payload).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}

using System.Reflection;
using System.Text.Json;
using EU.Core.Agent.Api.Errors;
using EU.Core.Agent.Api.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace EU.Core.Agent.Tests;

public sealed class OperationsFixRoundOneTests
{
    [Fact]
    public void Host_accepts_the_dedicated_runtime_model_credential_name()
    {
        using EnvironmentVariableScope scope = ValidOptions(
            ("AGENT_MODEL_API_KEY", "runtime-secret-value"));

        using var client = CreateApiClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void Host_rejects_model_endpoint_that_contains_user_info_without_echoing_it()
    {
        const string endpoint = "https://model-user:model-password@example.test/v1";
        using EnvironmentVariableScope scope = ValidOptions(("AgentPlatform__ModelEndpoint", endpoint));

        Exception exception = Assert.ThrowsAny<Exception>(() => CreateApiClient());

        Assert.Contains("ModelEndpoint", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(endpoint, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("model-password", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_rejects_connection_string_shaped_configuration_outside_the_options_section_without_echoing_it()
    {
        const string connection = "Server=not-a-real-server;Integrated Security=true";
        using EnvironmentVariableScope scope = ValidOptions(("Database__Connection", connection));

        Exception exception = Assert.ThrowsAny<Exception>(() => CreateApiClient());

        Assert.Contains("Configuration", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(connection, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Log_redaction_walks_nested_poco_values_and_masks_all_sensitive_members()
    {
        var source = new StructuredRequest
        {
            RequestId = "request-73",
            Credentials = new StructuredCredentials
            {
                ApiKey = "poco-api-key-456",
                Token = "poco-token-456",
                ConnectionString = "Server=poco-host;Password=poco-password",
            },
        };

        object? redacted = LogRedactionPolicy.Redact(source);
        string serialized = JsonSerializer.Serialize(redacted);

        Assert.Contains("request-73", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("poco-api-key-456", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("poco-token-456", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("poco-password", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void Serilog_pipeline_masks_nested_structured_properties_in_the_emitted_event()
    {
        var sink = new CapturingSink();
        object enricher = CreateInstance("EU.Core.Agent.Api.Observability.LogRedactionEnricher");
        using Logger logger = new LoggerConfiguration()
            .Enrich.With((ILogEventEnricher)enricher)
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.ForContext("request", new StructuredRequest
        {
            RequestId = "request-74",
            Credentials = new StructuredCredentials { Authorization = "Bearer emitted-secret-456" },
        }, destructureObjects: true).Information("Captured structured request");

        LogEvent emitted = Assert.Single(sink.Events);
        string serialized = emitted.Properties["request"].ToString();

        Assert.Contains("request-74", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("emitted-secret-456", serialized, StringComparison.Ordinal);
        Assert.Contains(LogRedactionPolicy.RedactedValue, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_redaction_masks_secret_and_general_credential_names_in_a_nested_poco_without_masking_ordinary_fields()
    {
        var source = new StructuredRequest
        {
            RequestId = "ordinary-request-75",
            Credentials = new StructuredCredentials
            {
                DisplayName = "ordinary-provider",
                ClientSecret = "client-secret-should-not-leak",
                Credential = "general-credential-should-not-leak",
                McpCredential = "mcp-credential-should-not-leak",
            },
        };

        object? redacted = LogRedactionPolicy.Redact(source);
        string serialized = JsonSerializer.Serialize(redacted);

        Assert.Contains("ordinary-request-75", serialized, StringComparison.Ordinal);
        Assert.Contains("ordinary-provider", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("client-secret-should-not-leak", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("general-credential-should-not-leak", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("mcp-credential-should-not-leak", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void Serilog_pipeline_masks_secret_and_general_credential_names_in_a_nested_structured_event()
    {
        var sink = new CapturingSink();
        object enricher = CreateInstance("EU.Core.Agent.Api.Observability.LogRedactionEnricher");
        using Logger logger = new LoggerConfiguration()
            .Enrich.With((ILogEventEnricher)enricher)
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.ForContext("request", new StructuredRequest
        {
            RequestId = "ordinary-request-76",
            Credentials = new StructuredCredentials
            {
                DisplayName = "ordinary-mcp-provider",
                ClientSecret = "event-client-secret",
                Credential = "event-general-credential",
                McpCredential = "event-mcp-credential",
            },
        }, destructureObjects: true).Information("Captured credential-bearing request");

        LogEvent emitted = Assert.Single(sink.Events);
        string serialized = emitted.Properties["request"].ToString();

        Assert.Contains("ordinary-request-76", serialized, StringComparison.Ordinal);
        Assert.Contains("ordinary-mcp-provider", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("event-client-secret", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("event-general-credential", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("event-mcp-credential", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Http_problem_details_keeps_the_selected_correlation_header_after_the_response_is_cleared()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        WebApplication app = builder.Build();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ProblemDetailsMiddleware>();
        app.Run(_ => throw new InvalidOperationException("test fault"));

        await app.StartAsync();
        try
        {
            using HttpClient client = app.GetTestClient();
            using HttpRequestMessage request = new(HttpMethod.Get, "/");
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, "problem-500-request");

            using HttpResponseMessage response = await client.SendAsync(request);

            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out IEnumerable<string>? values));
            Assert.Equal("problem-500-request", Assert.Single(values));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static EnvironmentVariableScope ValidOptions(params (string Name, string? Value)[] overrides)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["AgentPlatform__ServiceName"] = "agent-api",
            ["AgentPlatform__ModelEndpoint"] = "https://model.example.test/v1",
            ["AgentPlatform__ModelCredentialAlias"] = "alias:development-agent-model",
            ["AgentStorage__Provider"] = "InMemory",
            ["AgentPlatform__ModelApiKey"] = null,
            ["Database__Connection"] = null,
        };
        foreach ((string name, string? value) in overrides)
        {
            values[name] = value;
        }

        return new EnvironmentVariableScope(values.Select(value => (value.Key, value.Value)).ToArray());
    }

    private static ApiClient CreateApiClient()
    {
        Type entryPoint = Assembly.Load("EU.Core.Agent.Api").GetType("Program")
            ?? throw new InvalidOperationException("The API entry point was not found.");
        Type factoryType = typeof(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<>)
            .MakeGenericType(entryPoint);
        object factory = Activator.CreateInstance(factoryType)
            ?? throw new InvalidOperationException("The API test host could not be created.");
        MethodInfo createClient = factoryType.GetMethod("CreateClient", [typeof(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions)])
            ?? throw new InvalidOperationException("The API test host has no client factory.");
        HttpClient client = (HttpClient)createClient.Invoke(factory, [new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()])!;
        return new ApiClient(client, (IDisposable)factory);
    }

    private static object CreateInstance(string typeName)
    {
        Type type = typeof(LogRedactionPolicy).Assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Expected type '{typeName}' was not found.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not create '{typeName}'.");
    }

    private sealed class StructuredRequest
    {
        public string RequestId { get; init; } = string.Empty;

        public StructuredCredentials Credentials { get; init; } = new();
    }

    private sealed class StructuredCredentials
    {
        public string? DisplayName { get; init; }

        public string? ApiKey { get; init; }

        public string? Token { get; init; }

        public string? Authorization { get; init; }

        public string? ConnectionString { get; init; }

        public string? ClientSecret { get; init; }

        public string? Credential { get; init; }

        public string? McpCredential { get; init; }
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> originalValues;

        public EnvironmentVariableScope(params (string Name, string? Value)[] values)
        {
            originalValues = values.ToDictionary(value => value.Name, value => Environment.GetEnvironmentVariable(value.Name), StringComparer.Ordinal);
            foreach ((string name, string? value) in values)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach ((string name, string? value) in originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }

    private sealed class ApiClient : IDisposable
    {
        private readonly IDisposable factory;

        public ApiClient(HttpClient client, IDisposable factory)
        {
            Client = client;
            this.factory = factory;
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            factory.Dispose();
        }
    }
}

using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace EU.Core.Agent.Tests;

public sealed class OperationsFoundationTests
{
    private const string ServiceNameKey = "AgentPlatform__ServiceName";
    private const string ModelEndpointKey = "AgentPlatform__ModelEndpoint";
    private const string CredentialAliasKey = "AgentPlatform__ModelCredentialAlias";

    [Fact]
    public void Host_rejects_missing_required_non_secret_options_without_disclosing_environment_values()
    {
        using EnvironmentVariableScope scope = new(
            (ServiceNameKey, null),
            (ModelEndpointKey, null),
            (CredentialAliasKey, null));

        Exception exception = Assert.ThrowsAny<Exception>(() => CreateApiClient());

        Assert.Contains("AgentPlatform", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("AGENT_MODEL_API_KEY", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Host_rejects_raw_secret_configuration_without_echoing_the_secret()
    {
        const string rawSecret = "not-a-secret-to-ship-123";
        using EnvironmentVariableScope scope = new(
            (ServiceNameKey, "agent-api"),
            (ModelEndpointKey, "https://model.example.test/v1"),
            (CredentialAliasKey, "alias:development-agent-model"),
            ("AgentPlatform__ModelApiKey", rawSecret));

        Exception exception = Assert.ThrowsAny<Exception>(() => CreateApiClient());

        Assert.Contains("credential alias", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawSecret, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_rejects_a_secret_shaped_value_even_when_it_is_mislabelled_as_a_credential_alias()
    {
        const string rawSecret = "sk-live-example-secret-321";
        using EnvironmentVariableScope scope = new(
            (ServiceNameKey, "agent-api"),
            (ModelEndpointKey, "https://model.example.test/v1"),
            (CredentialAliasKey, rawSecret));

        Exception exception = Assert.ThrowsAny<Exception>(() => CreateApiClient());

        Assert.Contains("credential alias", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawSecret, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Problem_details_middleware_returns_safe_rfc_style_json_with_a_trace_identifier()
    {
        const string secret = "exception-secret-456";
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => throw new InvalidOperationException($"failure: {secret}");

        object middleware = CreateInstance(
            "EU.Core.Agent.Api.Errors.ProblemDetailsMiddleware",
            next,
            NullLoggerFactory.Instance);

        await InvokeAsync(middleware, context);
        context.Response.Body.Position = 0;
        using JsonDocument response = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(StatusCodes.Status500InternalServerError, response.RootElement.GetProperty("status").GetInt32());
        Assert.True(response.RootElement.TryGetProperty("traceId", out JsonElement traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        Assert.DoesNotContain(secret, response.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Correlation_middleware_returns_a_safe_incoming_id_and_replaces_an_unsafe_one()
    {
        DefaultHttpContext accepted = new();
        accepted.Request.Headers["X-Correlation-ID"] = "request-42_A";
        object middleware = CreateInstance(
            "EU.Core.Agent.Api.Observability.CorrelationIdMiddleware",
            (RequestDelegate)(_ => Task.CompletedTask),
            NullLoggerFactory.Instance);

        await InvokeAsync(middleware, accepted);

        Assert.Equal("request-42_A", accepted.Response.Headers["X-Correlation-ID"].ToString());

        DefaultHttpContext rejected = new();
        rejected.Request.Headers["X-Correlation-ID"] = new string('x', 129);
        await InvokeAsync(middleware, rejected);

        string generated = rejected.Response.Headers["X-Correlation-ID"].ToString();
        Assert.NotEmpty(generated);
        Assert.NotEqual(new string('x', 129), generated);
        Assert.InRange(generated.Length, 1, 128);
    }

    [Fact]
    public void Log_redaction_masks_nested_sensitive_fields_without_altering_non_sensitive_values()
    {
        var source = new Dictionary<string, object?>
        {
            ["requestId"] = "request-42",
            ["authorization"] = "Bearer log-secret-789",
            ["nested"] = new Dictionary<string, object?>
            {
                ["credentialAlias"] = "alias:development-agent-model",
                ["password"] = "password-secret",
            },
        };

        object? redacted = InvokeStatic("EU.Core.Agent.Api.Observability.LogRedactionPolicy", "Redact", source);
        string serialized = JsonSerializer.Serialize(redacted);

        Assert.Contains("request-42", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("log-secret-789", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("alias:development-agent-model", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("password-secret", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Replica_health_check_reports_a_single_replica_without_database_data()
    {
        object healthCheck = CreateInstance("EU.Core.Agent.Api.Health.ReplicaModeHealthCheck");
        MethodInfo check = healthCheck.GetType().GetMethod("CheckHealthAsync")
            ?? throw new InvalidOperationException("Health check method was not found.");
        Task<HealthCheckResult> task = (Task<HealthCheckResult>)check.Invoke(healthCheck, [new HealthCheckContext(), CancellationToken.None])!;

        HealthCheckResult result = await task;

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("single", result.Data["replicaMode"]);
        Assert.DoesNotContain(result.Data.Keys, key => key.Contains("database", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Host_serves_only_safe_service_metadata_and_single_replica_health_without_database_configuration()
    {
        using EnvironmentVariableScope scope = ValidOptions();
        using ApiClient client = CreateApiClient();

        HttpResponseMessage root = await client.Client.GetAsync("/");
        HttpResponseMessage health = await client.Client.GetAsync("/health");
        string healthJson = await health.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, root.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, health.StatusCode);
        Assert.StartsWith("application/json", health.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"replicaMode\":\"single\"", healthJson, StringComparison.Ordinal);
        Assert.DoesNotContain("database", healthJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Checked_in_configuration_template_uses_a_credential_alias_and_has_no_raw_secret_or_database_setting()
    {
        string template = File.ReadAllText(Path.Combine(SolutionRoot(), ".env.example"));

        Assert.Contains("AgentPlatform__ModelCredentialAlias=alias:", template, StringComparison.Ordinal);
        Assert.DoesNotContain("API_KEY", template, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AUTHORIZATION", template, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PASSWORD", template, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CONNECTION_STRING", template, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Api_composition_contains_no_database_startup_or_migration_path()
    {
        string apiRoot = Path.Combine(SolutionRoot(), "src", "EU.Core.Agent.Api");
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("AddDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UseSql", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlSugar", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate", source, StringComparison.Ordinal);
    }

    private static EnvironmentVariableScope ValidOptions()
    {
        return new EnvironmentVariableScope(
            (ServiceNameKey, "agent-api"),
            (ModelEndpointKey, "https://model.example.test/v1"),
            (CredentialAliasKey, "alias:development-agent-model"),
            ("AgentStorage__Provider", "InMemory"),
            ("AgentPlatform__ModelApiKey", null),
            ("AgentPlatform__ConnectionString", null));
    }

    private static ApiClient CreateApiClient()
    {
        Assembly apiAssembly = Assembly.Load("EU.Core.Agent.Api");
        Type entryPoint = apiAssembly.GetType("Program")
            ?? throw new InvalidOperationException("The API entry point was not found.");
        Type factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);
        object factory = Activator.CreateInstance(factoryType)
            ?? throw new InvalidOperationException("The API test host could not be created.");
        MethodInfo createClient = factoryType.GetMethod("CreateClient", [typeof(WebApplicationFactoryClientOptions)])
            ?? throw new InvalidOperationException("The API test host has no client factory.");
        HttpClient client = (HttpClient)createClient.Invoke(factory, [new WebApplicationFactoryClientOptions()])!;
        return new ApiClient(client, (IDisposable)factory);
    }

    private static object CreateInstance(string typeName, params object[] arguments)
    {
        Type type = ApiAssembly().GetType(typeName)
            ?? throw new InvalidOperationException($"Expected type '{typeName}' was not found.");
        return Activator.CreateInstance(type, arguments)
            ?? throw new InvalidOperationException($"Could not create '{typeName}'.");
    }

    private static object? InvokeStatic(string typeName, string methodName, object argument)
    {
        Type type = ApiAssembly().GetType(typeName)
            ?? throw new InvalidOperationException($"Expected type '{typeName}' was not found.");
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Expected method '{typeName}.{methodName}' was not found.");
        return method.Invoke(null, [argument]);
    }

    private static Task InvokeAsync(object middleware, HttpContext context)
    {
        MethodInfo method = middleware.GetType().GetMethod("InvokeAsync")
            ?? throw new InvalidOperationException($"Middleware '{middleware.GetType().FullName}' has no InvokeAsync method.");
        return (Task)method.Invoke(middleware, [context])!;
    }

    private static Assembly ApiAssembly() => Assembly.Load("EU.Core.Agent.Api");

    private static string SolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the solution root.");
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

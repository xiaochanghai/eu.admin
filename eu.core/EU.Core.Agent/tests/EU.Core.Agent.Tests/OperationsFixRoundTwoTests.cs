using System.Reflection;
using EU.Core.Agent.Api.Observability;
using Microsoft.AspNetCore.Mvc.Testing;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace EU.Core.Agent.Tests;

public sealed class OperationsFixRoundTwoTests
{
    [Fact]
    public void Serilog_redaction_truncates_deep_sequence_and_structure_values_before_recursion_can_grow_unbounded()
    {
        LogEventPropertyValue value = new ScalarValue("deep-leaf");
        for (int depth = 0; depth < 32; depth++)
        {
            value = new SequenceValue([value]);
        }

        value = new StructureValue([new LogEventProperty("nested", value)], "DeepPayload");
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplate(Array.Empty<MessageTemplateToken>()),
            [new LogEventProperty("payload", value)]);

        new LogRedactionEnricher().Enrich(logEvent, null!);
        string rendered = logEvent.Properties["payload"].ToString();

        Assert.Contains("[TRUNCATED]", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("deep-leaf", rendered, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ghp_ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")]
    [InlineData("5f4dcc3b5aa765d61d8327deb882cf99aabbccddeeff00112233445566778899")]
    [InlineData("YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXowMTIzNDU2Nzg5QUJDREVGR0g")]
    [InlineData("sk-live-already-rejected-secret")]
    public void Host_rejects_ambiguous_or_secret_shaped_credential_aliases_without_echoing_them(string value)
    {
        using EnvironmentVariableScope scope = ValidOptions(("AgentPlatform__ModelCredentialAlias", value));

        Exception exception = Assert.ThrowsAny<Exception>(() => CreateApiClient());

        Assert.Contains("credential alias", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(value, exception.ToString(), StringComparison.Ordinal);
    }

    private static EnvironmentVariableScope ValidOptions(params (string Name, string? Value)[] overrides)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["AgentPlatform__ServiceName"] = "agent-api",
            ["AgentPlatform__ModelEndpoint"] = "https://model.example.test/v1",
            ["AgentPlatform__ModelCredentialAlias"] = "alias:development-agent-model",
            ["AgentStorage__Provider"] = "InMemory",
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
        Type factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);
        object factory = Activator.CreateInstance(factoryType)
            ?? throw new InvalidOperationException("The API test host could not be created.");
        MethodInfo createClient = factoryType.GetMethod("CreateClient", [typeof(WebApplicationFactoryClientOptions)])
            ?? throw new InvalidOperationException("The API test host has no client factory.");
        HttpClient client = (HttpClient)createClient.Invoke(factory, [new WebApplicationFactoryClientOptions()])!;
        return new ApiClient(client, (IDisposable)factory);
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

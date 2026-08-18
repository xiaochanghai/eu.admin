#nullable enable

using EU.Core.Api.Agent.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgentPlatformOptionsValidator_Should
{
    private static readonly AgentPlatformOptions ValidOptions = new()
    {
        ServiceName = "agent-api",
        ModelEndpoint = "https://models.example.test/v1",
        ModelCredentialAlias = "alias:test-model"
    };

    [Fact]
    public void Allow_shared_audience_authentication_settings()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Audience:Secret"] = "shared-jwt-signing-secret",
            ["Audience:SecretFile"] = "C:\\secrets\\audience.key"
        });

        ValidateOptionsResult result =
            new AgentPlatformOptionsValidator(configuration).Validate(null, ValidOptions);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Other:Secret")]
    [InlineData("Other:Token")]
    [InlineData("Other:Password")]
    public void Continue_rejecting_unapproved_sensitive_settings(string key)
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [key] = "credential-value"
        });

        ValidateOptionsResult result =
            new AgentPlatformOptionsValidator(configuration).Validate(null, ValidOptions);

        Assert.True(result.Failed);
        Assert.Contains(key, result.FailureMessage);
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}

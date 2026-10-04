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
        ServiceName = "agent-api"
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

    [Fact]
    public void Allow_shared_redis_connection_setting()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] =
                "redis.internal.test:6379,password=<credential-placeholder>"
        });

        ValidateOptionsResult result =
            new AgentPlatformOptionsValidator(configuration).Validate(null, ValidOptions);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Other:Secret")]
    [InlineData("Other:Token")]
    [InlineData("Other:Password")]
    [InlineData("Other:ConnectionString")]
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

    [Theory]
    [InlineData("UnifiedEntry:TokenBudgetWarningPercent", "80")]
    [InlineData("AgentExecution:TokenBudgetWarningPercent", "80")]
    [InlineData("AgentExecution:MaximumRunTotalTokens", "100000")]
    [InlineData("AgentUserTokenQuota:RequestReservationTokens", "1000")]
    [InlineData("BusinessQueryForwarding:TokenLifetimeSeconds", "60")]
    [InlineData("RateLimit:TokensPerMinute", "60000")]
    public void Allow_non_secret_token_accounting_settings(string key, string value)
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [key] = value
        });

        ValidateOptionsResult result =
            new AgentPlatformOptionsValidator(configuration).Validate(null, ValidOptions);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Other:AccessToken")]
    [InlineData("Other:ApiToken")]
    [InlineData("Other:TokenValue")]
    [InlineData("Other:TokenCredential")]
    [InlineData("Other:AccessTokenSetting")]
    public void Continue_rejecting_named_token_credentials(string key)
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [key] = "opaque-credential-value"
        });

        ValidateOptionsResult result =
            new AgentPlatformOptionsValidator(configuration).Validate(null, ValidOptions);

        Assert.True(result.Failed);
        Assert.Contains(key, result.FailureMessage);
    }

    [Fact]
    public void Current_agent_appsettings_passes_sensitive_configuration_validation()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EU.Core.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(directory.FullName, "EU.Core.Api.Agent", "appsettings.json"), optional: false)
            .Build();

        ValidateOptionsResult result =
            new AgentPlatformOptionsValidator(configuration).Validate(null, ValidOptions);

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}

using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentDeploymentOptions
{
    public const string SectionName = "AgentDeployment";

    public int ShutdownTimeoutSeconds { get; init; } = 30;

    public bool MetricsEnabled { get; init; }
}

public sealed class AgentDeploymentOptionsValidator
    : IValidateOptions<AgentDeploymentOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        AgentDeploymentOptions options) =>
        options.ShutdownTimeoutSeconds is >= 5 and <= 120
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "AgentDeployment:ShutdownTimeoutSeconds must be from 5 through 120.");
}

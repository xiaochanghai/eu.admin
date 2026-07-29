using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class AgentMcpOptions
{
    public const string SectionName = "AgentMcp";

    public IReadOnlyList<string> AllowedHosts { get; init; } = Array.Empty<string>();

    public IReadOnlyList<int> AllowedPorts { get; init; } = [443];

    public IReadOnlyList<string> AllowedStdioCommands { get; init; } = Array.Empty<string>();

    public bool EnableStdio { get; init; }

    public int ConnectionTimeoutSeconds { get; init; } = 15;

    public int DiscoveryTimeoutSeconds { get; init; } = 15;
}

public sealed class AgentMcpOptionsValidator : IValidateOptions<AgentMcpOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentMcpOptions options)
    {
        if (options.ConnectionTimeoutSeconds is < 1 or > 120 ||
            options.DiscoveryTimeoutSeconds is < 1 or > 120)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp timeouts must be from 1 through 120 seconds.");
        }

        if (options.AllowedPorts.Count == 0 ||
            options.AllowedPorts.Any(port => port is < 1 or > 65535))
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:AllowedPorts must contain valid TCP ports.");
        }

        if (options.AllowedHosts.Any(host =>
                string.IsNullOrWhiteSpace(host) ||
                host.Contains('/') ||
                host.Contains('@')))
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:AllowedHosts contains an invalid host.");
        }

        if (options.EnableStdio && options.AllowedStdioCommands.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:AllowedStdioCommands is required when stdio is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}

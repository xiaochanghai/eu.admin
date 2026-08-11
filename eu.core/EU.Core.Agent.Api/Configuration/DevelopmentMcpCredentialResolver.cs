using EU.Core.Agent.Infrastructure.Mcp;
using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class DevelopmentMcpCredentialResolver(
    IOptions<AgentMcpOptions> options,
    IHostEnvironment environment) : IMcpCredentialResolver
{
    private readonly EnvironmentMcpCredentialResolver _environment = new();

    public ValueTask<string?> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgentMcpOptions configuration = options.Value;
        if (environment.IsDevelopment()
            && !string.IsNullOrEmpty(configuration.DevelopmentCredential)
            && string.Equals(
                credentialAlias,
                configuration.DevelopmentCredentialAlias,
                StringComparison.Ordinal))
        {
            return ValueTask.FromResult<string?>(configuration.DevelopmentCredential);
        }

        return _environment.ResolveAsync(credentialAlias, cancellationToken);
    }
}

using EU.Core.Api.MCP.Services.BusinessQuery.Execution;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed class EnvironmentBusinessDataSourceCredentialResolver(
    IOptions<BusinessQueryOptions> options) : IBusinessDataSourceCredentialResolver
{
    public ValueTask<string> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BusinessQueryOptions configuration = options.Value;
        if (!string.Equals(
                credentialAlias,
                configuration.CredentialAlias,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The credential alias is unavailable.");
        }

        string value = Environment.GetEnvironmentVariable(
            configuration.CredentialEnvironmentVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("The credential alias is unavailable.");
        }

        return ValueTask.FromResult(value);
    }
}

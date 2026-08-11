using System.Security.Cryptography;
using EU.Core.Agent.Infrastructure.Mcp;
using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class DevelopmentBusinessQuerySigningKeyResolver(
    IOptions<BusinessQueryForwardingOptions> options,
    IHostEnvironment environment) : IBusinessQuerySigningKeyResolver
{
    private readonly EnvironmentBusinessQuerySigningKeyResolver _environment = new();

    public ValueTask<byte[]> ResolveAsync(
        string alias,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BusinessQueryForwardingOptions configuration = options.Value;
        if (!environment.IsDevelopment()
            || string.IsNullOrEmpty(configuration.DevelopmentSigningKey))
        {
            return _environment.ResolveAsync(alias, cancellationToken);
        }

        if (!string.Equals(alias, configuration.SigningKeyAlias, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The Business Query signing key alias is unavailable.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configuration.DevelopmentSigningKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "The Business Query signing key alias is unavailable.");
        }

        if (key.Length is < 32 or > 64)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new InvalidOperationException(
                "The Business Query signing key alias is unavailable.");
        }

        return ValueTask.FromResult(key);
    }
}

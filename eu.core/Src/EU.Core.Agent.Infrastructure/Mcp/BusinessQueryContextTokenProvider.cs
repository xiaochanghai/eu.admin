using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.Agent.Infrastructure.Mcp;

public interface IBusinessQuerySigningKeyResolver
{
    ValueTask<byte[]> ResolveAsync(
        string alias,
        CancellationToken cancellationToken = default);
}

public sealed class EnvironmentBusinessQuerySigningKeyResolver
    : IBusinessQuerySigningKeyResolver
{
    public ValueTask<byte[]> ResolveAsync(
        string alias,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string suffix = alias["alias:".Length..]
            .ToUpperInvariant()
            .Replace('-', '_')
            .Replace('.', '_');
        string encoded = Environment.GetEnvironmentVariable(
            $"AGENT_BUSINESS_QUERY_SIGNING_KEY_{suffix}") ?? string.Empty;
        byte[] key;
        try
        {
            key = Convert.FromBase64String(encoded);
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

public sealed class BusinessQueryContextTokenProvider(
    IBusinessQuerySigningKeyResolver keyResolver,
    TimeProvider timeProvider) : IBusinessQueryContextTokenProvider
{
    public const string MetadataKey = "eu.core.agent/executionContext";

    public async ValueTask<string> CreateAsync(
        McpInvocationContext invocationContext,
        BusinessQueryToolPolicy policy,
        PublishedMcpToolReference tool,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(tool);
        if (!string.Equals(tool.ServerCode, policy.ServerCode, StringComparison.Ordinal)
            || !string.Equals(tool.ToolName, policy.ToolName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Business Query tool binding is invalid.");
        }

        byte[] key = await keyResolver.ResolveAsync(
            policy.SigningKeyAlias,
            cancellationToken);
        try
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            long issuedAt = now.ToUnixTimeSeconds();
            string keyId = Convert.ToHexStringLower(SHA256.HashData(key))[..16];
            string header = Base64Url(WriteJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("alg", "HS256");
                writer.WriteString("typ", "EU-BQ-CTX");
                writer.WriteNumber("ver", 1);
                writer.WriteString("kid", keyId);
                writer.WriteEndObject();
            }));
            string[] permissions = invocationContext.Identity.Permissions
                .Where(value => value.StartsWith("business.", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (permissions.Length == 0)
            {
                throw new InvalidOperationException(
                    "The caller has no Business Query permission to forward.");
            }

            string payload = Base64Url(WriteJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("iss", policy.Issuer);
                writer.WriteString("aud", policy.Audience);
                writer.WriteString("sub", invocationContext.Identity.UserId);
                writer.WriteString("tenant", invocationContext.Identity.TenantId);
                writer.WritePropertyName("permissions");
                writer.WriteStartArray();
                foreach (string permission in permissions)
                {
                    writer.WriteStringValue(permission);
                }
                writer.WriteEndArray();
                writer.WriteString("correlation", invocationContext.Identity.CorrelationId);
                writer.WriteString("agentRun", invocationContext.AgentRunId);
                writer.WriteString("toolVersion", tool.ToolVersionId);
                writer.WriteString("server", policy.ServerCode);
                writer.WriteString("tool", policy.ToolName);
                writer.WriteNumber("catalogRevision", policy.CatalogRevision);
                writer.WriteString("catalogHash", policy.CatalogHash);
                writer.WriteString("toolSchemaHash", policy.ToolSchemaHash);
                writer.WriteNumber("iat", issuedAt);
                writer.WriteNumber("nbf", issuedAt);
                writer.WriteNumber("exp", issuedAt + (long)policy.TokenLifetime.TotalSeconds);
                writer.WriteString("jti", Base64Url(RandomNumberGenerator.GetBytes(24)));
                writer.WriteEndObject();
            }));
            string signingInput = $"{header}.{payload}";
            byte[] signature = HMACSHA256.HashData(
                key,
                Encoding.ASCII.GetBytes(signingInput));
            return $"{signingInput}.{Base64Url(signature)}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] WriteJson(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return stream.ToArray();
    }

    private static string Base64Url(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

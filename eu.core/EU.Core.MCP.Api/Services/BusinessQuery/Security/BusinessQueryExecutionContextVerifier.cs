using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed record BusinessQueryExecutionContextValidation(
    BusinessQueryExecutionContext? Context,
    string ErrorCode)
{
    public bool Succeeded => Context is not null;
}

public sealed class BusinessQueryExecutionContextKeyResolver(
    IOptions<BusinessQueryOptions> options,
    IHostEnvironment environment)
{
    public IReadOnlyList<byte[]> ResolveVerificationKeys()
    {
        BusinessQueryOptions configuration = options.Value;
        string[] aliases = string.IsNullOrEmpty(
            configuration.PreviousExecutionContextSigningKeyAlias)
                ? [configuration.ExecutionContextSigningKeyAlias]
                : [
                    configuration.ExecutionContextSigningKeyAlias,
                    configuration.PreviousExecutionContextSigningKeyAlias
                ];
        var keys = new List<byte[]>(aliases.Length);
        try
        {
            foreach (string alias in aliases)
            {
                string suffix = alias["alias:".Length..]
                    .ToUpperInvariant()
                    .Replace('-', '_')
                    .Replace('.', '_');
                string encoded = environment.IsDevelopment()
                    && string.Equals(
                        alias,
                        configuration.ExecutionContextSigningKeyAlias,
                        StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(
                        configuration.DevelopmentExecutionContextSigningKey)
                            ? configuration.DevelopmentExecutionContextSigningKey
                            : Environment.GetEnvironmentVariable(
                                $"AGENT_BUSINESS_QUERY_SIGNING_KEY_{suffix}")
                                ?? string.Empty;
                byte[] key = Convert.FromBase64String(encoded);
                if (key.Length is < 32 or > 64)
                {
                    CryptographicOperations.ZeroMemory(key);
                    throw new InvalidOperationException();
                }

                keys.Add(key);
            }

            return keys;
        }
        catch
        {
            foreach (byte[] key in keys)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            throw new InvalidOperationException(
                "The Business Query execution-context key is unavailable.");
        }
    }
}

public sealed partial class BusinessQueryExecutionContextVerifier(
    IOptions<BusinessQueryOptions> options,
    BusinessQueryToolDefinition toolDefinition,
    BusinessQueryExecutionContextKeyResolver keyResolver,
    SqliteBusinessQueryReplayRepository replayRepository,
    BusinessQueryExecutionContextAccessor accessor,
    IBusinessQueryAuditRepository auditRepository,
    TimeProvider timeProvider)
{
    public const string MetadataKey = "eu.core.agent/executionContext";
    private const string InvalidCode = "BUSINESS_QUERY_EXECUTION_CONTEXT_INVALID";

    public McpRequestFilter<CallToolRequestParams, CallToolResult> CreateFilter() =>
        next => async (context, cancellationToken) =>
        {
            string token = ReadToken(context.Params?.Meta);
            BusinessQueryExecutionContextValidation validation =
                await ValidateAsync(token, context.Params?.Name ?? string.Empty, cancellationToken);
            if (!validation.Succeeded)
            {
                return await RejectAsync(validation.ErrorCode);
            }

            using IDisposable scope = accessor.Enter(validation.Context!);
            return await next(context, cancellationToken);
        };

    public async Task<BusinessQueryExecutionContextValidation> ValidateAsync(
        string token,
        string requestedToolName,
        CancellationToken cancellationToken)
    {
        BusinessQueryOptions configuration = options.Value;
        if (!string.Equals(requestedToolName, toolDefinition.Name, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(token)
            || token.Length > 8192)
        {
            return Failure();
        }

        string[] parts = token.Split('.');
        if (parts.Length != 3
            || parts.Any(value => !Base64UrlPattern().IsMatch(value)))
        {
            return Failure();
        }

        byte[] headerBytes;
        byte[] payloadBytes;
        byte[] suppliedSignature;
        try
        {
            headerBytes = DecodeCanonical(parts[0]);
            payloadBytes = DecodeCanonical(parts[1]);
            suppliedSignature = DecodeCanonical(parts[2]);
        }
        catch
        {
            return Failure();
        }

        if (suppliedSignature.Length != 32
            || !TryParseHeader(headerBytes, out string keyId))
        {
            return Failure();
        }

        IReadOnlyList<byte[]> keys;
        try
        {
            keys = keyResolver.ResolveVerificationKeys();
        }
        catch
        {
            return new(null, "BUSINESS_QUERY_EXECUTION_CONTEXT_UNAVAILABLE");
        }

        bool signatureValid = false;
        try
        {
            byte[] signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
            foreach (byte[] key in keys)
            {
                string candidateId = Convert.ToHexStringLower(SHA256.HashData(key))[..16];
                byte[] signature = HMACSHA256.HashData(key, signingInput);
                signatureValid |= string.Equals(candidateId, keyId, StringComparison.Ordinal)
                    && CryptographicOperations.FixedTimeEquals(signature, suppliedSignature);
            }
        }
        finally
        {
            foreach (byte[] key in keys)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        if (!signatureValid
            || !TryParsePayload(payloadBytes, out TokenPayload? payload)
            || !BindingsAreValid(payload!, configuration))
        {
            return Failure();
        }

        TokenPayload validatedPayload = payload!;

        if (!await replayRepository.TryRegisterAsync(
                validatedPayload.Jti,
                DateTimeOffset.FromUnixTimeSeconds(validatedPayload.ExpiresAt),
                cancellationToken))
        {
            return new(null, "BUSINESS_QUERY_EXECUTION_CONTEXT_REPLAYED");
        }

        return new(
            new BusinessQueryExecutionContext(
                validatedPayload.UserId,
                validatedPayload.TenantId,
                validatedPayload.Permissions,
                validatedPayload.CorrelationId,
                validatedPayload.AgentRunId,
                validatedPayload.ToolVersionId,
                validatedPayload.Jti),
            string.Empty);
    }

    private bool BindingsAreValid(TokenPayload payload, BusinessQueryOptions configuration)
    {
        long now = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        long skew = configuration.ExecutionContextClockSkewSeconds;
        return string.Equals(payload.Issuer, configuration.ExecutionContextIssuer, StringComparison.Ordinal)
            && string.Equals(payload.Audience, configuration.ExecutionContextAudience, StringComparison.Ordinal)
            && string.Equals(payload.TenantId, configuration.TenantId, StringComparison.Ordinal)
            && string.Equals(payload.ServerCode, configuration.ServerCode, StringComparison.Ordinal)
            && string.Equals(payload.ToolName, toolDefinition.Name, StringComparison.Ordinal)
            && payload.CatalogRevision == toolDefinition.CatalogRevision
            && string.Equals(payload.CatalogHash, toolDefinition.CatalogHash, StringComparison.Ordinal)
            && string.Equals(payload.ToolSchemaHash, toolDefinition.ToolVersionHash, StringComparison.Ordinal)
            && payload.IssuedAt <= now + skew
            && payload.NotBefore >= payload.IssuedAt
            && payload.NotBefore <= now + skew
            && payload.ExpiresAt > payload.NotBefore
            && payload.ExpiresAt - payload.IssuedAt <= 60
            && now <= payload.ExpiresAt + skew
            && payload.AgentRunId != Guid.Empty
            && payload.ToolVersionId != Guid.Empty
            && SafeIdentity(payload.UserId)
            && SafeIdentity(payload.CorrelationId)
            && payload.Permissions.Length is > 0 and <= 128
            && payload.Permissions.SequenceEqual(
                payload.Permissions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            && payload.Permissions.All(value =>
                value.StartsWith("business.", StringComparison.Ordinal)
                && PermissionPattern().IsMatch(value));
    }

    public async Task<CallToolResult> RejectAsync(string errorCode)
    {
        try
        {
            await auditRepository.WriteSecurityRejectionAsync(
                new BusinessQuerySecurityAuditRecord(
                    Guid.NewGuid(),
                    "execution-context",
                    "rejected",
                    errorCode,
                    timeProvider.GetUtcNow()),
                CancellationToken.None);
        }
        catch
        {
            errorCode = "BUSINESS_QUERY_AUDIT_UNAVAILABLE";
        }

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = errorCode }]
        };
    }

    private static string ReadToken(JsonObject? meta)
    {
        try
        {
            return meta?[MetadataKey]?.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryParseHeader(byte[] json, out string keyId)
    {
        keyId = string.Empty;
        try
        {
            using JsonDocument document = ParseStrictObject(json, 4);
            JsonElement root = document.RootElement;
            if (!ExactProperties(root, "alg", "typ", "ver", "kid")
                || root.GetProperty("alg").GetString() != "HS256"
                || root.GetProperty("typ").GetString() != "EU-BQ-CTX"
                || root.GetProperty("ver").GetInt32() != 1)
            {
                return false;
            }

            keyId = root.GetProperty("kid").GetString() ?? string.Empty;
            return KeyIdPattern().IsMatch(keyId);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParsePayload(byte[] json, out TokenPayload? payload)
    {
        payload = null;
        try
        {
            using JsonDocument document = ParseStrictObject(json, 18);
            JsonElement root = document.RootElement;
            if (!ExactProperties(
                    root, "iss", "aud", "sub", "tenant", "permissions",
                    "correlation", "agentRun", "toolVersion", "server", "tool",
                    "catalogRevision", "catalogHash", "toolSchemaHash", "iat",
                    "nbf", "exp", "jti"))
            {
                return false;
            }

            string[] permissions = root.GetProperty("permissions")
                .EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray();
            payload = new TokenPayload(
                root.GetProperty("iss").GetString() ?? string.Empty,
                root.GetProperty("aud").GetString() ?? string.Empty,
                root.GetProperty("sub").GetString() ?? string.Empty,
                root.GetProperty("tenant").GetString() ?? string.Empty,
                permissions,
                root.GetProperty("correlation").GetString() ?? string.Empty,
                root.GetProperty("agentRun").GetGuid(),
                root.GetProperty("toolVersion").GetGuid(),
                root.GetProperty("server").GetString() ?? string.Empty,
                root.GetProperty("tool").GetString() ?? string.Empty,
                root.GetProperty("catalogRevision").GetInt64(),
                root.GetProperty("catalogHash").GetString() ?? string.Empty,
                root.GetProperty("toolSchemaHash").GetString() ?? string.Empty,
                root.GetProperty("iat").GetInt64(),
                root.GetProperty("nbf").GetInt64(),
                root.GetProperty("exp").GetInt64(),
                root.GetProperty("jti").GetString() ?? string.Empty);
            return JtiPattern().IsMatch(payload.Jti);
        }
        catch
        {
            return false;
        }
    }

    private static JsonDocument ParseStrictObject(byte[] json, int maximumProperties)
    {
        JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            MaxDepth = 4,
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new JsonException();
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (!names.Add(property.Name) || ++count > maximumProperties)
            {
                document.Dispose();
                throw new JsonException();
            }
        }

        return document;
    }

    private static bool ExactProperties(JsonElement root, params string[] expected) =>
        root.EnumerateObject().Select(value => value.Name)
            .ToHashSet(StringComparer.Ordinal)
            .SetEquals(expected);

    private static byte[] DecodeCanonical(string value)
    {
        string base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += new string('=', (4 - base64.Length % 4) % 4);
        byte[] decoded = Convert.FromBase64String(base64);
        string canonical = Convert.ToBase64String(decoded).TrimEnd('=')
            .Replace('+', '-').Replace('/', '_');
        return canonical == value ? decoded : throw new FormatException();
    }

    private static bool SafeIdentity(string value) =>
        value.Length is > 0 and <= 256 && !value.Any(char.IsControl);

    private static BusinessQueryExecutionContextValidation Failure() =>
        new(null, InvalidCode);

    private sealed record TokenPayload(
        string Issuer,
        string Audience,
        string UserId,
        string TenantId,
        string[] Permissions,
        string CorrelationId,
        Guid AgentRunId,
        Guid ToolVersionId,
        string ServerCode,
        string ToolName,
        long CatalogRevision,
        string CatalogHash,
        string ToolSchemaHash,
        long IssuedAt,
        long NotBefore,
        long ExpiresAt,
        string Jti);

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Base64UrlPattern();

    [GeneratedRegex("^[a-f0-9]{16}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyIdPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]{16,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex JtiPattern();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionPattern();
}

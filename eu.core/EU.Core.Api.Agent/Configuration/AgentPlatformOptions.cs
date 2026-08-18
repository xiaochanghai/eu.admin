using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentPlatformOptions
{
    public const string SectionName = "AgentPlatform";

    public string ServiceName { get; init; } = string.Empty;

    public string ModelEndpoint { get; init; } = string.Empty;

    public string ModelCredentialAlias { get; init; } = string.Empty;

    public bool ExposeOpenApi { get; init; }
}

public sealed partial class AgentPlatformOptionsValidator(IConfiguration configuration) : IValidateOptions<AgentPlatformOptions>
{
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex ServiceNamePattern();

    [GeneratedRegex("^alias:[a-z][a-z0-9-]{2,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialAliasPattern();

    [GeneratedRegex("^(?:sk-|bearer[._-]|eyJ)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SecretShapedAliasPattern();

    [GeneratedRegex("(?i)(api[_-]?key|authorization|password|pwd|token|secret|connection[_-]?string)", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveKeyPattern();

    [GeneratedRegex("(?i)(password|pwd|token|api[_-]?key|authorization|connection[_-]?string)\\s*=", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveValuePattern();

    [GeneratedRegex("(?i)(?:^|;)\\s*(?:server|data[ _-]?source|host|database|initial[ _-]?catalog|integrated[ _-]?security|trusted[ _-]?connection)\\s*=", RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringValuePattern();

    [GeneratedRegex("^\\s*(?:sk-|bearer\\s+|eyJ)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex CredentialValuePattern();

    public ValidateOptionsResult Validate(string? name, AgentPlatformOptions options)
    {
        List<string> failures = [];

        if (!ServiceNamePattern().IsMatch(options.ServiceName))
        {
            failures.Add("AgentPlatform:ServiceName is required and must be a lowercase service identifier.");
        }

        if (!Uri.TryCreate(options.ModelEndpoint, UriKind.Absolute, out Uri? endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(endpoint.UserInfo))
        {
            failures.Add("AgentPlatform:ModelEndpoint is required and must be an absolute HTTP or HTTPS URI.");
        }

        if (!CredentialAliasPattern().IsMatch(options.ModelCredentialAlias) || SecretShapedAliasPattern().IsMatch(options.ModelCredentialAlias))
        {
            failures.Add("AgentPlatform:ModelCredentialAlias is required and must be a credential alias, not a credential value.");
        }

        foreach ((string key, string? value) in configuration.AsEnumerable())
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string propertyName = key[(key.LastIndexOf(ConfigurationPath.KeyDelimiter, StringComparison.Ordinal) + 1)..];
            bool isModelCredentialAlias = string.Equals(key, $"{AgentPlatformOptions.SectionName}{ConfigurationPath.KeyDelimiter}{nameof(AgentPlatformOptions.ModelCredentialAlias)}", StringComparison.OrdinalIgnoreCase) &&
                CredentialAliasPattern().IsMatch(value) &&
                !SecretShapedAliasPattern().IsMatch(value);
            bool isCredentialAlias = isModelCredentialAlias;
            bool isRuntimeSecret =
                string.Equals(key, "AGENT_MODEL_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("AGENT_MODEL_CREDENTIAL_", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("AGENT_MCP_CREDENTIAL_", StringComparison.OrdinalIgnoreCase);
            bool isNonSecretLifetime = string.Equals(
                    key,
                    $"{BusinessQueryForwardingOptions.SectionName}{ConfigurationPath.KeyDelimiter}{nameof(BusinessQueryForwardingOptions.TokenLifetimeSeconds)}",
                    StringComparison.OrdinalIgnoreCase)
                && int.TryParse(
                    value,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int lifetime)
                && lifetime is >= 1 and <= 60;
            bool isSqlSugarConnection =
                key.StartsWith("DBS:", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(propertyName, "Connection", StringComparison.OrdinalIgnoreCase);
            bool isSharedAuthenticationCredential =
                string.Equals(key, "Audience:Secret", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Audience:SecretFile", StringComparison.OrdinalIgnoreCase);
            if (!isCredentialAlias && !isRuntimeSecret && !isNonSecretLifetime &&
                !isSqlSugarConnection && !isSharedAuthenticationCredential &&
                (SensitiveKeyPattern().IsMatch(propertyName) ||
                SensitiveValuePattern().IsMatch(value) ||
                ConnectionStringValuePattern().IsMatch(value) ||
                CredentialValuePattern().IsMatch(value)))
            {
                failures.Add($"Configuration entry '{key}' must not contain credentials or a connection string. Configure a credential alias instead.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

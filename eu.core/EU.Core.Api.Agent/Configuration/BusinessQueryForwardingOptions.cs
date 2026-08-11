using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed record BusinessQueryForwardingOptions
{
    public const string SectionName = "BusinessQueryForwarding";

    public bool Enabled { get; init; }
    public string ServerCode { get; init; } = string.Empty;
    public string ToolName { get; init; } = "query_business_data";
    public string Origin { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKeyAlias { get; init; } = string.Empty;
    public string DevelopmentSigningKey { get; init; } = string.Empty;
    public long CatalogRevision { get; init; }
    public string CatalogHash { get; init; } = string.Empty;
    public string ToolSchemaHash { get; init; } = string.Empty;
    public int TokenLifetimeSeconds { get; init; } = 45;
    public bool AllowDevelopmentHttp { get; init; }
}

public sealed partial class BusinessQueryForwardingOptionsValidator(
    IHostEnvironment environment) : IValidateOptions<BusinessQueryForwardingOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        BusinessQueryForwardingOptions options)
    {
        if (!string.IsNullOrEmpty(options.DevelopmentSigningKey)
            && (!environment.IsDevelopment()
                || !IsValidSigningKey(options.DevelopmentSigningKey)))
        {
            return ValidateOptionsResult.Fail(
                "BusinessQueryForwarding development signing key is invalid.");
        }

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        bool validOrigin = Uri.TryCreate(options.Origin, UriKind.Absolute, out Uri? origin)
            && string.IsNullOrEmpty(origin.UserInfo)
            && string.IsNullOrEmpty(origin.Query)
            && string.IsNullOrEmpty(origin.Fragment)
            && origin.AbsolutePath == "/"
            && (origin.Scheme == Uri.UriSchemeHttps
                || (environment.IsDevelopment()
                    && options.AllowDevelopmentHttp
                    && origin.Scheme == Uri.UriSchemeHttp));
        if (!CodePattern().IsMatch(options.ServerCode ?? string.Empty)
            || !string.Equals(options.ToolName, "query_business_data", StringComparison.Ordinal)
            || !validOrigin
            || !CodePattern().IsMatch(options.Issuer ?? string.Empty)
            || !CodePattern().IsMatch(options.Audience ?? string.Empty)
            || !AliasPattern().IsMatch(options.SigningKeyAlias ?? string.Empty)
            || options.CatalogRevision < 1
            || !HashPattern().IsMatch(options.CatalogHash ?? string.Empty)
            || !HashPattern().IsMatch(options.ToolSchemaHash ?? string.Empty)
            || options.TokenLifetimeSeconds is < 1 or > 60)
        {
            return ValidateOptionsResult.Fail(
                "BusinessQueryForwarding configuration is invalid.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidSigningKey(string encoded)
    {
        try
        {
            byte[] key = Convert.FromBase64String(encoded);
            bool valid = key.Length is >= 32 and <= 64;
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            return valid;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
}

public sealed class BusinessQueryMcpEgressOptionsValidator(
    IOptions<AgentMcpOptions> mcpOptions) :
    IValidateOptions<BusinessQueryForwardingOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        BusinessQueryForwardingOptions options)
    {
        if (!options.Enabled ||
            !Uri.TryCreate(options.Origin, UriKind.Absolute, out Uri? origin))
        {
            return ValidateOptionsResult.Success;
        }

        AgentMcpOptions mcp = mcpOptions.Value;
        string originHost = NormalizeHost(origin.IdnHost);
        bool hostAllowed = mcp.AllowedHosts.Any(host => string.Equals(
            NormalizeHost(host),
            originHost,
            StringComparison.OrdinalIgnoreCase));
        bool developmentHttpAllowed = origin.Scheme != Uri.UriSchemeHttp ||
            mcp.AllowDevelopmentHttp;
        if (!hostAllowed ||
            !mcp.AllowedPorts.Contains(origin.Port) ||
            !developmentHttpAllowed)
        {
            return ValidateOptionsResult.Fail(
                "BusinessQueryForwarding origin must be allowed by AgentMcp egress configuration.");
        }

        return ValidateOptionsResult.Success;
    }

    private static string NormalizeHost(string host)
    {
        string value = host.Trim().TrimEnd('.');
        if (value.Length >= 2 && value[0] == '[' && value[^1] == ']')
        {
            value = value[1..^1];
        }

        if (IPAddress.TryParse(value, out IPAddress? address))
        {
            return address.ToString();
        }

        try
        {
            return new IdnMapping().GetAscii(value).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return value;
        }
    }
}

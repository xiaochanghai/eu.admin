using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed record BusinessQueryForwardingOptions
{
    public const string SectionName = "BusinessQueryForwarding";

    public bool Enabled { get; init; }
    public string ServerCode { get; init; } = string.Empty;
    public string ToolName { get; init; } = "query_business_data";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKeyAlias { get; init; } = "alias:project-jwt";
    // Legacy field retained for configuration compatibility; ignored.
    public string DevelopmentSigningKey { get; init; } = string.Empty;
    public long CatalogRevision { get; init; }
    public string CatalogHash { get; init; } = string.Empty;
    public string ToolSchemaHash { get; init; } = string.Empty;
    public int TokenLifetimeSeconds { get; init; } = 45;
}

public sealed partial class BusinessQueryForwardingOptionsValidator
    : IValidateOptions<BusinessQueryForwardingOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        BusinessQueryForwardingOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (!CodePattern().IsMatch(options.ServerCode ?? string.Empty)
            || !string.Equals(options.ToolName, "query_business_data", StringComparison.Ordinal)
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

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
}

using System.Text.RegularExpressions;
using EU.Core.Agent.Application.Mcp;

namespace EU.Core.Agent.Application.UnifiedEntry;

public sealed partial class BusinessQueryToolPolicy
{
    public BusinessQueryToolPolicy(
        string serverCode,
        string toolName,
        Uri origin,
        string issuer,
        string audience,
        string signingKeyAlias,
        long catalogRevision,
        string catalogHash,
        string toolSchemaHash,
        TimeSpan tokenLifetime,
        bool allowDevelopmentHttp)
    {
        if (!CodePattern().IsMatch(serverCode ?? string.Empty)
            || !string.Equals(toolName, "query_business_data", StringComparison.Ordinal)
            || origin is null
            || !origin.IsAbsoluteUri
            || origin.UserInfo.Length > 0
            || origin.Query.Length > 0
            || origin.Fragment.Length > 0
            || origin.AbsolutePath != "/"
            || (origin.Scheme != Uri.UriSchemeHttps
                && !(allowDevelopmentHttp && origin.Scheme == Uri.UriSchemeHttp))
            || !CodePattern().IsMatch(issuer ?? string.Empty)
            || !CodePattern().IsMatch(audience ?? string.Empty)
            || !AliasPattern().IsMatch(signingKeyAlias ?? string.Empty)
            || catalogRevision < 1
            || !HashPattern().IsMatch(catalogHash ?? string.Empty)
            || !HashPattern().IsMatch(toolSchemaHash ?? string.Empty)
            || tokenLifetime <= TimeSpan.Zero
            || tokenLifetime > TimeSpan.FromSeconds(60))
        {
            throw new ArgumentException("The Business Query tool policy is invalid.");
        }

        ServerCode = serverCode!;
        ToolName = toolName!;
        Origin = origin!;
        Issuer = issuer!;
        Audience = audience!;
        SigningKeyAlias = signingKeyAlias!;
        CatalogRevision = catalogRevision;
        CatalogHash = catalogHash!;
        ToolSchemaHash = toolSchemaHash!;
        TokenLifetime = tokenLifetime;
        AllowDevelopmentHttp = allowDevelopmentHttp;
    }

    public string ServerCode { get; }
    public string ToolName { get; }
    public Uri Origin { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public string SigningKeyAlias { get; }
    public long CatalogRevision { get; }
    public string CatalogHash { get; }
    public string ToolSchemaHash { get; }
    public TimeSpan TokenLifetime { get; }
    public bool AllowDevelopmentHttp { get; }

    public bool Matches(string serverCode, string toolName, string endpoint) =>
        string.Equals(ServerCode, serverCode, StringComparison.Ordinal)
        && string.Equals(ToolName, toolName, StringComparison.Ordinal)
        && Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
        && string.Equals(Origin.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Origin.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
        && Origin.Port == uri.Port;

    public bool Matches(PublishedMcpToolReference tool) =>
        tool is not null
        && string.Equals(ServerCode, tool.ServerCode, StringComparison.Ordinal)
        && string.Equals(ToolName, tool.ToolName, StringComparison.Ordinal);

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
}

public sealed record BusinessQueryToolPolicyAccessor(
    BusinessQueryToolPolicy? Policy);

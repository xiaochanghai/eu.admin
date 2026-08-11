using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentMcpOptions
{
    public const string SectionName = "AgentMcp";

    public IReadOnlyList<string> AllowedHosts { get; init; } = Array.Empty<string>();

    public IReadOnlyList<int> AllowedPorts { get; init; } = [443];

    public IReadOnlyList<AgentMcpStdioProfileOptions> StdioProfiles { get; init; } =
        Array.Empty<AgentMcpStdioProfileOptions>();

    public bool EnableStdio { get; init; }

    public bool AllowDevelopmentHttp { get; init; }

    public int ConnectionTimeoutSeconds { get; init; } = 15;

    public int DiscoveryTimeoutSeconds { get; init; } = 15;

    public string DevelopmentCredentialAlias { get; init; } = string.Empty;

    public string DevelopmentCredential { get; init; } = string.Empty;
}

public sealed class AgentMcpOptionsValidator(IHostEnvironment? environment = null)
    : IValidateOptions<AgentMcpOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentMcpOptions options)
    {
        if (options.AllowDevelopmentHttp && environment?.IsDevelopment() != true)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:AllowDevelopmentHttp is allowed only in Development.");
        }

        if (options.ConnectionTimeoutSeconds is < 1 or > 120 ||
            options.DiscoveryTimeoutSeconds is < 1 or > 120)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp timeouts must be from 1 through 120 seconds.");
        }

        if (options.AllowedPorts.Count is 0 or > 32 ||
            options.AllowedPorts.Any(port => port is < 1 or > 65535) ||
            options.AllowedPorts.Distinct().Count() != options.AllowedPorts.Count)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:AllowedPorts must contain valid TCP ports.");
        }

        if (options.AllowedHosts.Count > 64 ||
            options.AllowedHosts.Any(host => !IsValidAllowedHost(host)) ||
            options.AllowedHosts
                .Select(NormalizeAllowedHost)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != options.AllowedHosts.Count)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:AllowedHosts contains an invalid host.");
        }

        if (options.EnableStdio && options.StdioProfiles.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:StdioProfiles is required when stdio is enabled.");
        }

        if (options.StdioProfiles.Count > 16 ||
            options.StdioProfiles.Any(profile => !IsValidStdioProfile(profile)) ||
            options.StdioProfiles
                .Select(StdioProfileKey)
                .Distinct(CommandComparer)
                .Count() != options.StdioProfiles.Count)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp:StdioProfiles contains an invalid or duplicate invocation.");
        }

        bool hasAlias = !string.IsNullOrEmpty(options.DevelopmentCredentialAlias);
        bool hasCredential = !string.IsNullOrEmpty(options.DevelopmentCredential);
        if (hasAlias != hasCredential
            || (hasAlias
                && (environment?.IsDevelopment() != true
                    || !options.DevelopmentCredentialAlias.StartsWith(
                        "alias:", StringComparison.Ordinal)
                    || options.DevelopmentCredentialAlias.Length > 200
                    || options.DevelopmentCredential.Length is < 32 or > 256
                    || options.DevelopmentCredential.Contains('\r')
                    || options.DevelopmentCredential.Contains('\n'))))
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp development credential configuration is invalid.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidAllowedHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) ||
            host.Length > 255 ||
            !string.Equals(host, host.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        string value = UnwrapIpLiteral(host.TrimEnd('.'));
        if (IPAddress.TryParse(value, out _)) return true;
        try
        {
            string ascii = new IdnMapping().GetAscii(value);
            return Uri.CheckHostName(ascii) == UriHostNameType.Dns;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string NormalizeAllowedHost(string host)
    {
        string value = UnwrapIpLiteral(host.TrimEnd('.'));
        return IPAddress.TryParse(value, out IPAddress? address)
            ? address.ToString()
            : new IdnMapping().GetAscii(value).ToLowerInvariant();
    }

    private static string UnwrapIpLiteral(string value) =>
        value.Length >= 2 && value[0] == '[' && value[^1] == ']'
            ? value[1..^1]
            : value;

    private bool IsValidStdioProfile(AgentMcpStdioProfileOptions? profile)
    {
        bool isDevelopment = environment?.IsDevelopment() == true;
        bool hasIntegrityPin = !string.IsNullOrEmpty(profile?.ExecutableSha256);
        if (profile is null ||
            string.IsNullOrWhiteSpace(profile.Command) ||
            profile.Command.Length > 512 ||
            !string.Equals(profile.Command, profile.Command.Trim(), StringComparison.Ordinal) ||
            profile.Command.Contains('\0') ||
            ((!isDevelopment || hasIntegrityPin) &&
             !Path.IsPathFullyQualified(profile.Command)) ||
            (!isDevelopment && !hasIntegrityPin) ||
            (hasIntegrityPin && !IsLowerHexSha256(profile.ExecutableSha256)) ||
            profile.Arguments is null ||
            profile.Arguments.Count > 32 ||
            profile.Arguments.Any(argument =>
                argument is null || argument.Length > 1024 || argument.Contains('\0')))
        {
            return false;
        }

        return true;
    }

    private static bool IsLowerHexSha256(string value) =>
        value.Length == 64 && value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string StdioProfileKey(AgentMcpStdioProfileOptions profile) =>
        string.Join("\0", new[] { profile.Command }.Concat(profile.Arguments));

    private static StringComparer CommandComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}

public sealed class AgentMcpStdioProfileOptions
{
    public string Command { get; init; } = string.Empty;

    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    public string ExecutableSha256 { get; init; } = string.Empty;
}

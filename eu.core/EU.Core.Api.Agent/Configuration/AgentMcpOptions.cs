using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentMcpOptions
{
    public const string SectionName = "AgentMcp";

    public IReadOnlyList<AgentMcpStdioProfileOptions> StdioProfiles { get; init; } =
        Array.Empty<AgentMcpStdioProfileOptions>();

    public bool EnableStdio { get; init; }

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
        if (options.ConnectionTimeoutSeconds is < 1 or > 120 ||
            options.DiscoveryTimeoutSeconds is < 1 or > 120)
        {
            return ValidateOptionsResult.Fail(
                "AgentMcp timeouts must be from 1 through 120 seconds.");
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

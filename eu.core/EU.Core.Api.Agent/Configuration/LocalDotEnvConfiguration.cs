using Microsoft.Extensions.Configuration;

namespace EU.Core.Api.Agent.Configuration;

public static class LocalDotEnvConfiguration
{
    private const string LoadDotEnvKey = "AgentPlatform:LoadDotEnv";
    private const string ServiceNameKey = "AgentPlatform:ServiceName";
    private const string ModelEndpointKey = "AgentPlatform:ModelEndpoint";
    private const string ModelCredentialAliasKey = "AgentPlatform:ModelCredentialAlias";
    private const string ModelProfileSection = "AgentControl:ModelProfileIds";

    public static void Apply(
        IConfigurationManager configuration,
        string contentRootPath,
        string baseDirectory)
    {
        if (!configuration.GetValue<bool>(LoadDotEnvKey))
        {
            return;
        }

        string? path = FindNearest(contentRootPath) ?? FindNearest(baseDirectory);
        if (path is null)
        {
            return;
        }

        IReadOnlyDictionary<string, string> entries = ReadAllowedEntries(path);
        Dictionary<string, string?> additions = new(StringComparer.OrdinalIgnoreCase);

        AddWhenMissing(
            configuration,
            additions,
            ServiceNameKey,
            GetAny(
                entries,
                "AgentPlatform__ServiceName",
                "AGENT_PLATFORM__SERVICE_NAME") ??
            "agent-api");
        AddWhenMissing(
            configuration,
            additions,
            ModelEndpointKey,
            GetAny(
                entries,
                "AgentPlatform__ModelEndpoint",
                "AGENT_PLATFORM__MODEL_ENDPOINT") ??
            Get(entries, "AGENT_MODEL_ENDPOINT"));
        AddWhenMissing(
            configuration,
            additions,
            ModelCredentialAliasKey,
            GetAny(
                entries,
                "AgentPlatform__ModelCredentialAlias",
                "AGENT_PLATFORM__MODEL_CREDENTIAL_ALIAS") ??
            "alias:local-agent-model");
        AddWhenMissing(
            configuration,
            additions,
            "AgentStorage:Provider",
            Get(entries, "AgentStorage__Provider"));
        AddWhenMissing(
            configuration,
            additions,
            "AgentStorage:DatabasePath",
            Get(entries, "AgentStorage__DatabasePath"));
        AddWhenMissing(
            configuration,
            additions,
            "AgentStorage:SkillRootPath",
            Get(entries, "AgentStorage__SkillRootPath"));
        AddWhenMissing(configuration, additions, "AgentMcp:EnableStdio", Get(entries, "AgentMcp__EnableStdio"));
        AddWhenMissing(configuration, additions, "AgentMcp:AllowDevelopmentHttp", Get(entries, "AgentMcp__AllowDevelopmentHttp"));
        AddWhenMissing(configuration, additions, "AgentMcp:ConnectionTimeoutSeconds", Get(entries, "AgentMcp__ConnectionTimeoutSeconds"));
        AddWhenMissing(configuration, additions, "AgentMcp:DiscoveryTimeoutSeconds", Get(entries, "AgentMcp__DiscoveryTimeoutSeconds"));
        AddWhenMissing(configuration, additions, "AgentExecution:ModelTimeoutSeconds", Get(entries, "AgentExecution__ModelTimeoutSeconds"));
        AddWhenMissing(configuration, additions, "AgentExecution:ToolCallTimeoutSeconds", Get(entries, "AgentExecution__ToolCallTimeoutSeconds"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumToolResultBytes", Get(entries, "AgentExecution__MaximumToolResultBytes"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumModelOutputBytes", Get(entries, "AgentExecution__MaximumModelOutputBytes"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumModelOutputEvents", Get(entries, "AgentExecution__MaximumModelOutputEvents"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumModelInputBytes", Get(entries, "AgentExecution__MaximumModelInputBytes"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumToolArgumentBytes", Get(entries, "AgentExecution__MaximumToolArgumentBytes"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumInternalToolResultBytes", Get(entries, "AgentExecution__MaximumInternalToolResultBytes"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumInternalToolCalls", Get(entries, "AgentExecution__MaximumInternalToolCalls"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumMcpToolCalls", Get(entries, "AgentExecution__MaximumMcpToolCalls"));
        AddWhenMissing(configuration, additions, "AgentExecution:MaximumApprovedToolResultBytes", Get(entries, "AgentExecution__MaximumApprovedToolResultBytes"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:MaximumDelegationDepth", Get(entries, "UnifiedEntry__MaximumDelegationDepth"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:MaximumChildCalls", Get(entries, "UnifiedEntry__MaximumChildCalls"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:MaximumOrchestrationCalls", Get(entries, "UnifiedEntry__MaximumOrchestrationCalls"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:MaximumMcpCalls", Get(entries, "UnifiedEntry__MaximumMcpCalls"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:EntryTimeoutSeconds", Get(entries, "UnifiedEntry__EntryTimeoutSeconds"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:ChildTimeoutSeconds", Get(entries, "UnifiedEntry__ChildTimeoutSeconds"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:MaximumInternalPayloadBytes", Get(entries, "UnifiedEntry__MaximumInternalPayloadBytes"));
        AddWhenMissing(configuration, additions, "UnifiedEntry:MaximumMcpResultBytes", Get(entries, "UnifiedEntry__MaximumMcpResultBytes"));
        AddWhenMissing(configuration, additions, "AgentRateLimit:Enabled", Get(entries, "AgentRateLimit__Enabled"));
        AddWhenMissing(configuration, additions, "AgentRateLimit:GeneralPermitLimit", Get(entries, "AgentRateLimit__GeneralPermitLimit"));
        AddWhenMissing(configuration, additions, "AgentRateLimit:ExpensivePermitLimit", Get(entries, "AgentRateLimit__ExpensivePermitLimit"));
        AddWhenMissing(configuration, additions, "AgentRateLimit:WindowSeconds", Get(entries, "AgentRateLimit__WindowSeconds"));
        AddWhenMissing(configuration, additions, "AgentCapacity:Enabled", Get(entries, "AgentCapacity__Enabled"));
        AddWhenMissing(configuration, additions, "AgentCapacity:MaximumConcurrentExpensiveRequests", Get(entries, "AgentCapacity__MaximumConcurrentExpensiveRequests"));
        AddWhenMissing(configuration, additions, "AgentCapacity:RetryAfterSeconds", Get(entries, "AgentCapacity__RetryAfterSeconds"));
        AddWhenMissing(configuration, additions, "AgentIdempotency:Enabled", Get(entries, "AgentIdempotency__Enabled"));
        AddWhenMissing(configuration, additions, "AgentIdempotency:RetentionHours", Get(entries, "AgentIdempotency__RetentionHours"));
        AddWhenMissing(configuration, additions, "AgentIdempotency:MaximumCachedResponseBytes", Get(entries, "AgentIdempotency__MaximumCachedResponseBytes"));
        AddWhenMissing(configuration, additions, "AgentHttpSecurity:AllowDevelopmentHttpOrigins", Get(entries, "AgentHttpSecurity__AllowDevelopmentHttpOrigins"));
        CopyIndexed(entries, additions, "AgentMcp__AllowedHosts__", "AgentMcp:AllowedHosts");
        CopyIndexed(entries, additions, "AgentMcp__AllowedPorts__", "AgentMcp:AllowedPorts");
        CopyIndexed(entries, additions, "AgentHttpSecurity__AllowedOrigins__", "AgentHttpSecurity:AllowedOrigins");

        if (!configuration.GetSection(ModelProfileSection).GetChildren().Any())
        {
            IEnumerable<KeyValuePair<string, string>> configuredProfiles = entries
                .Where(entry =>
                    entry.Key.StartsWith(
                        "AgentControl__ModelProfileIds__",
                        StringComparison.OrdinalIgnoreCase) ||
                    entry.Key.StartsWith(
                        "AGENT_CONTROL__MODEL_PROFILE_IDS__",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

            int index = 0;
            foreach ((_, string value) in configuredProfiles)
            {
                additions[$"{ModelProfileSection}:{index++}"] = value;
            }

            if (index == 0 && Get(entries, "AGENT_MODEL_DEFAULT_ID") is string modelId)
            {
                additions[$"{ModelProfileSection}:0"] = modelId;
            }
        }

        configuration.AddInMemoryCollection(additions);
    }

    public static string? FindNearest(string startDirectory)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> ReadAllowedEntries(string path)
    {
        Dictionary<string, string> entries = new(StringComparer.OrdinalIgnoreCase);
        foreach (string line in File.ReadLines(path))
        {
            if (!TryParse(line, out string name, out string value) ||
                !IsAllowed(name))
            {
                continue;
            }

            entries[name] = value;
        }

        return entries;
    }

    private static bool IsAllowed(string name) =>
        name.Equals("AgentPlatform__ServiceName", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentPlatform__ModelEndpoint", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentPlatform__ModelCredentialAlias", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AGENT_PLATFORM__SERVICE_NAME", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AGENT_PLATFORM__MODEL_ENDPOINT", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AGENT_PLATFORM__MODEL_CREDENTIAL_ALIAS", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AGENT_MODEL_ENDPOINT", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AGENT_MODEL_DEFAULT_ID", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentStorage__Provider", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentStorage__DatabasePath", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentStorage__SkillRootPath", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentMcp__EnableStdio", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentMcp__AllowDevelopmentHttp", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentMcp__ConnectionTimeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentMcp__DiscoveryTimeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__ModelTimeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__ToolCallTimeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumToolResultBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumModelOutputBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumModelOutputEvents", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumModelInputBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumToolArgumentBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumInternalToolResultBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumInternalToolCalls", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumMcpToolCalls", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentExecution__MaximumApprovedToolResultBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__MaximumDelegationDepth", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__MaximumChildCalls", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__MaximumOrchestrationCalls", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__MaximumMcpCalls", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__EntryTimeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__ChildTimeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__MaximumInternalPayloadBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnifiedEntry__MaximumMcpResultBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentRateLimit__Enabled", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentRateLimit__GeneralPermitLimit", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentRateLimit__ExpensivePermitLimit", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentRateLimit__WindowSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentCapacity__Enabled", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentCapacity__MaximumConcurrentExpensiveRequests", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentCapacity__RetryAfterSeconds", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentIdempotency__Enabled", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentIdempotency__RetentionHours", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentIdempotency__MaximumCachedResponseBytes", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AgentHttpSecurity__AllowDevelopmentHttpOrigins", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("AgentHttpSecurity__AllowedOrigins__", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("AgentMcp__AllowedHosts__", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("AgentMcp__AllowedPorts__", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("AgentControl__ModelProfileIds__", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("AGENT_CONTROL__MODEL_PROFILE_IDS__", StringComparison.OrdinalIgnoreCase);

    private static void CopyIndexed(
        IReadOnlyDictionary<string, string> entries,
        IDictionary<string, string?> additions,
        string sourcePrefix,
        string targetSection)
    {
        foreach ((string key, string value) in entries.Where(entry =>
                     entry.Key.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            string index = key[sourcePrefix.Length..];
            if (int.TryParse(index, out int parsed) && parsed >= 0)
            {
                additions[$"{targetSection}:{parsed}"] = value;
            }
        }
    }

    private static string? Get(IReadOnlyDictionary<string, string> entries, string key) =>
        entries.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static string? GetAny(
        IReadOnlyDictionary<string, string> entries,
        params string[] keys) =>
        keys.Select(key => Get(entries, key)).FirstOrDefault(value => value is not null);

    private static void AddWhenMissing(
        IConfiguration configuration,
        IDictionary<string, string?> additions,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]) &&
            !string.IsNullOrWhiteSpace(value))
        {
            additions[key] = value;
        }
    }

    private static bool TryParse(
        string line,
        out string name,
        out string value)
    {
        name = string.Empty;
        value = string.Empty;

        string entry = line.Trim();
        if (entry.Length == 0 || entry.StartsWith('#'))
        {
            return false;
        }

        if (entry.StartsWith("export ", StringComparison.Ordinal))
        {
            entry = entry["export ".Length..].TrimStart();
        }

        int separator = entry.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        name = entry[..separator].Trim();
        if (!IsValidName(name))
        {
            return false;
        }

        value = ParseValue(entry[(separator + 1)..].Trim());
        return true;
    }

    private static string ParseValue(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        int comment = value.IndexOf(" #", StringComparison.Ordinal);
        return (comment >= 0 ? value[..comment] : value).TrimEnd();
    }

    private static bool IsValidName(string name) =>
        name.Length > 0 &&
        (char.IsAsciiLetter(name[0]) || name[0] == '_') &&
        name.All(character =>
            char.IsAsciiLetterOrDigit(character) || character == '_');
}

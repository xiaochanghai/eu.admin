using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class AgentStorageOptions
{
    public const string SectionName = "AgentStorage";

    public string Provider { get; init; } = "Sqlite";

    public string DatabasePath { get; init; } = "data/eu-core-agent.db";

    public string SkillRootPath { get; init; } = "agent-data/skills";

    public string ResolveDatabasePath(string contentRootPath) =>
        ResolvePath(contentRootPath, DatabasePath);

    public string ResolveSkillRootPath(string contentRootPath) =>
        ResolvePath(contentRootPath, SkillRootPath);

    private static string ResolvePath(string contentRootPath, string configuredPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        return Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }
}

public sealed class AgentStorageOptionsValidator : IValidateOptions<AgentStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentStorageOptions options)
    {
        if (!string.Equals(options.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                "AgentStorage:Provider must be Sqlite or InMemory.");
        }

        if (string.Equals(options.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(options.DatabasePath) ||
             !string.Equals(Path.GetExtension(options.DatabasePath), ".db", StringComparison.OrdinalIgnoreCase)))
        {
            return ValidateOptionsResult.Fail(
                "AgentStorage:DatabasePath is required for SQLite and must end with .db.");
        }

        if (string.IsNullOrWhiteSpace(options.SkillRootPath))
        {
            return ValidateOptionsResult.Fail(
                "AgentStorage:SkillRootPath is required.");
        }

        return ValidateOptionsResult.Success;
    }
}

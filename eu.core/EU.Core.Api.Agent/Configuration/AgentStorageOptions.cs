using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentStorageOptions
{
    public const string SectionName = "AgentStorage";

    public string SkillRootPath { get; init; } = "wwwroot/skills";

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
        if (string.IsNullOrWhiteSpace(options.SkillRootPath))
        {
            return ValidateOptionsResult.Fail(
                "AgentStorage:SkillRootPath is required.");
        }

        return ValidateOptionsResult.Success;
    }
}

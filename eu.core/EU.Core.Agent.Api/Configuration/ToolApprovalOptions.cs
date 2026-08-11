using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class ToolApprovalOptions
{
    public const string SectionName = "ToolApproval";

    public bool Enabled { get; init; }

    public int LifetimeMinutes { get; init; } = 15;

    public string DevelopmentPayloadKey { get; init; } = string.Empty;

}

public sealed class ToolApprovalOptionsValidator(
    IHostEnvironment environment,
    IConfiguration configuration) :
    IValidateOptions<ToolApprovalOptions>
{
    public ValidateOptionsResult Validate(string? name, ToolApprovalOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (options.LifetimeMinutes is < 1 or > 60)
        {
            failures.Add("ToolApproval:LifetimeMinutes must be between 1 and 60.");
        }

        string configured = environment.IsDevelopment()
            ? options.DevelopmentPayloadKey
            : ToolApprovalPayloadKeyResolver.ResolveEncoded(
                environment.ContentRootPath,
                configuration.GetValue<bool>("AgentPlatform:LoadDotEnv"));
        byte[]? key = null;
        try
        {
            key = Convert.FromBase64String(configured);
            if (key.Length != 32)
            {
                failures.Add(
                    "The tool approval payload key must decode to exactly 32 bytes.");
            }
        }
        catch (FormatException)
        {
            failures.Add("The tool approval payload key is unavailable.");
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

internal static class ToolApprovalPayloadKeyResolver
{
    private const string VariableName = "AGENT_TOOL_APPROVAL_PAYLOAD_KEY";

    public static string ResolveEncoded(string contentRoot, bool allowDotEnv)
    {
        string? process = Environment.GetEnvironmentVariable(VariableName);
        if (!string.IsNullOrWhiteSpace(process))
        {
            return process.Trim();
        }

        if (!allowDotEnv)
        {
            return string.Empty;
        }

        for (DirectoryInfo? directory = new(Path.GetFullPath(contentRoot));
             directory is not null;
             directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, ".env");
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (string line in File.ReadLines(path))
            {
                int equals = line.IndexOf('=');
                if (equals <= 0
                    || !string.Equals(
                        line[..equals].Trim(),
                        VariableName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string value = line[(equals + 1)..].Trim();
                if (value.Length >= 2
                    && ((value[0] == '"' && value[^1] == '"')
                        || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value[1..^1];
                }

                return value;
            }

            return string.Empty;
        }

        return string.Empty;
    }
}

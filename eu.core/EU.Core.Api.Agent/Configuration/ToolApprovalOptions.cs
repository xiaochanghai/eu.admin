using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class ToolApprovalOptions
{
    public const string SectionName = "ToolApproval";

    public bool Enabled { get; init; }

    public int LifetimeMinutes { get; init; } = 15;

    public string DevelopmentPayloadKey { get; init; } = string.Empty;

}

public sealed class ToolApprovalOptionsValidator(
    IHostEnvironment environment) :
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
            : ToolApprovalPayloadKeyResolver.ResolveEncoded();
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

    public static string ResolveEncoded() =>
        Environment.GetEnvironmentVariable(VariableName)?.Trim() ?? string.Empty;
}

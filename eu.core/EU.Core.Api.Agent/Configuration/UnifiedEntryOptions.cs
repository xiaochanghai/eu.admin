using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class UnifiedEntryOptions
{
    public const string SectionName = "UnifiedEntry";

    public int MaximumDelegationDepth { get; init; } = 4;

    public int MaximumChildCalls { get; init; } = 8;

    public int MaximumOrchestrationCalls { get; init; } = 4;

    public int MaximumMcpCalls { get; init; } = 20;

    public int EntryTimeoutSeconds { get; init; } = 300;

    public int ChildTimeoutSeconds { get; init; } = 120;

    public int MaximumInternalPayloadBytes { get; init; } = 32_768;

    public int MaximumMcpResultBytes { get; init; } = 4_194_304;

    public UnifiedEntryLimits ToLimits() =>
        new(
            MaximumDelegationDepth,
            MaximumChildCalls,
            MaximumOrchestrationCalls,
            MaximumMcpCalls,
            TimeSpan.FromSeconds(EntryTimeoutSeconds),
            TimeSpan.FromSeconds(ChildTimeoutSeconds),
            MaximumInternalPayloadBytes,
            MaximumMcpResultBytes);
}

public sealed class UnifiedEntryOptionsValidator
    : IValidateOptions<UnifiedEntryOptions>
{
    private const int MaximumSupportedTimeoutSeconds = 4_294_967;

    public ValidateOptionsResult Validate(
        string? name,
        UnifiedEntryOptions options)
    {
        if (options.MaximumDelegationDepth < 0
            || options.MaximumChildCalls < 0
            || options.MaximumOrchestrationCalls < 0
            || options.MaximumMcpCalls < 0
            || options.MaximumInternalPayloadBytes < 0)
        {
            return ValidateOptionsResult.Fail(
                "UnifiedEntry limits must be non-negative.");
        }

        if (options.MaximumMcpResultBytes is < 4_096 or > 16_777_216)
        {
            return ValidateOptionsResult.Fail(
                "UnifiedEntry:MaximumMcpResultBytes must be from 4096 through 16777216.");
        }

        if (!IsSupportedTimeout(options.EntryTimeoutSeconds)
            || !IsSupportedTimeout(options.ChildTimeoutSeconds))
        {
            return ValidateOptionsResult.Fail(
                $"UnifiedEntry timeouts must be from 1 through {MaximumSupportedTimeoutSeconds} seconds.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsSupportedTimeout(int seconds) =>
        seconds is > 0 and <= MaximumSupportedTimeoutSeconds;
}

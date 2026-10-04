using EU.Core.IServices.UnifiedEntry;
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

    /// <summary>一次统一入口执行树（含主、子 Agent 和编排节点）的模型总 Token 阈值；null 不启用。</summary>
    public long? MaximumModelTotalTokens { get; init; }

    /// <summary>执行树预算的已知累计用量预警百分比，默认 80；null 只关闭接近上限预警。</summary>
    public int? TokenBudgetWarningPercent { get; init; } = 80;

    public UnifiedEntryLimits ToLimits() =>
        new(
            MaximumDelegationDepth,
            MaximumChildCalls,
            MaximumOrchestrationCalls,
            MaximumMcpCalls,
            TimeSpan.FromSeconds(EntryTimeoutSeconds),
            TimeSpan.FromSeconds(ChildTimeoutSeconds),
            MaximumInternalPayloadBytes,
            MaximumMcpResultBytes,
            MaximumModelTotalTokens,
            TokenBudgetWarningPercent);
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

        if (options.MaximumModelTotalTokens is < 1)
        {
            return ValidateOptionsResult.Fail("UnifiedEntry:MaximumModelTotalTokens must be null or positive.");
        }

        if (options.TokenBudgetWarningPercent is < 1 or > 99)
        {
            return ValidateOptionsResult.Fail("UnifiedEntry:TokenBudgetWarningPercent must be null or from 1 through 99.");
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

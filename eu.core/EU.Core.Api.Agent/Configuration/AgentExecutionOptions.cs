using EU.Core.IServices.Approvals;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentExecutionOptions
{
    public const string SectionName = "AgentExecution";

    public int ToolCallTimeoutSeconds { get; init; } = 60;

    public int MaximumToolResultBytes { get; init; } = 1_048_576;

    public int MaximumModelOutputBytes { get; init; } = 32_768;

    public int MaximumModelOutputEvents { get; init; } = 4_096;

    public int MaximumModelInputBytes { get; init; } = 262_144;

    /// <summary>每次模型响应的输出 Token 上限；null 保持供应商默认，不按字符数估算。</summary>
    public int? MaximumModelOutputTokens { get; init; }

    /// <summary>单个 Agent 运行内模型上报的累计总 Token 阈值；null 不启用，不包含委派子运行。</summary>
    public long? MaximumRunTotalTokens { get; init; }

    /// <summary>输出及单运行预算的已知用量预警百分比，默认 80；null 关闭预警，不关闭预算拦截。</summary>
    public int? TokenBudgetWarningPercent { get; init; } = 80;

    public int MaximumToolArgumentBytes { get; init; } = 32_768;

    public int MaximumInternalToolResultBytes { get; init; } = 32_768;

    public int MaximumInternalToolCalls { get; init; } = 32;

    public int MaximumMcpToolCalls { get; init; } = 32;

    public int MaximumApprovedToolResultBytes { get; init; } = 30_000;
}

public sealed class AgentExecutionOptionsValidator :
    IValidateOptions<AgentExecutionOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        AgentExecutionOptions options) =>
        options.ToolCallTimeoutSeconds is < 1 or > 300 ||
        options.MaximumToolResultBytes is < 4_096 or > 4_194_304 ||
        options.MaximumModelOutputBytes is < 4_096 or > 1_048_576 ||
        options.MaximumModelOutputEvents is < 32 or > 16_384 ||
        options.MaximumModelInputBytes is < 65_536 or > 4_194_304 ||
        options.MaximumModelOutputTokens is < 1 ||
        options.MaximumRunTotalTokens is < 1 ||
        options.TokenBudgetWarningPercent is < 1 or > 99 ||
        options.MaximumToolArgumentBytes is < 4_096 or > 262_144 ||
        options.MaximumInternalToolResultBytes is < 4_096 or > 1_048_576 ||
        options.MaximumInternalToolCalls is < 1 or > 256 ||
        options.MaximumMcpToolCalls is < 1 or > 256 ||
        options.MaximumApprovedToolResultBytes is < 4_096
            or > ToolApprovalStateMachine.MaximumResultPlaintextUtf8Bytes
            ? ValidateOptionsResult.Fail(
                "AgentExecution limits are outside the supported range.")
            : ValidateOptionsResult.Success;
}

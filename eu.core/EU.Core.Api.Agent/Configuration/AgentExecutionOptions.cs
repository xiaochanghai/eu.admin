using EU.Core.IServices.Approvals;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentExecutionOptions
{
    public const string SectionName = "AgentExecution";

    public int ModelTimeoutSeconds { get; init; } = 120;

    public int ToolCallTimeoutSeconds { get; init; } = 60;

    public int MaximumToolResultBytes { get; init; } = 1_048_576;

    public int MaximumModelOutputBytes { get; init; } = 32_768;

    public int MaximumModelOutputEvents { get; init; } = 4_096;

    public int MaximumModelInputBytes { get; init; } = 262_144;

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
        options.ModelTimeoutSeconds is < 1 or > 600 ||
        options.ToolCallTimeoutSeconds is < 1 or > 300 ||
        options.MaximumToolResultBytes is < 4_096 or > 4_194_304 ||
        options.MaximumModelOutputBytes is < 4_096 or > 1_048_576 ||
        options.MaximumModelOutputEvents is < 32 or > 16_384 ||
        options.MaximumModelInputBytes is < 65_536 or > 4_194_304 ||
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

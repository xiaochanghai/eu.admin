using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class AgentExecutionOptions
{
    public const string SectionName = "AgentExecution";

    public int ModelTimeoutSeconds { get; init; } = 120;

    public int ToolCallTimeoutSeconds { get; init; } = 60;
}

public sealed class AgentExecutionOptionsValidator :
    IValidateOptions<AgentExecutionOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        AgentExecutionOptions options) =>
        options.ModelTimeoutSeconds is < 1 or > 600 ||
        options.ToolCallTimeoutSeconds is < 1 or > 300
            ? ValidateOptionsResult.Fail(
                "AgentExecution timeouts are outside the supported range.")
            : ValidateOptionsResult.Success;
}

using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class AgentEvaluationOptions
{
    public const string SectionName = "AgentEvaluation";

    public bool EnableModelJudge { get; init; }

    public int ModelJudgeMaximumCases { get; init; } = 20;

    public int ModelJudgeTimeoutSeconds { get; init; } = 300;
}

public sealed class AgentEvaluationOptionsValidator
    : IValidateOptions<AgentEvaluationOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentEvaluationOptions options)
    {
        if (options.ModelJudgeMaximumCases is < 1 or > 20)
        {
            return ValidateOptionsResult.Fail(
                "AgentEvaluation:ModelJudgeMaximumCases must be from 1 through 20.");
        }

        if (options.ModelJudgeTimeoutSeconds is < 10 or > 1800)
        {
            return ValidateOptionsResult.Fail(
                "AgentEvaluation:ModelJudgeTimeoutSeconds must be from 10 through 1800.");
        }

        return ValidateOptionsResult.Success;
    }
}

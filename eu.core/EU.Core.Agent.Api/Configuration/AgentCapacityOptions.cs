using Microsoft.Extensions.Options;

namespace EU.Core.Agent.Api.Configuration;

public sealed class AgentCapacityOptions
{
    public const string SectionName = "AgentCapacity";

    public bool Enabled { get; init; } = true;

    public int MaximumConcurrentExpensiveRequests { get; init; } = 8;

    public int RetryAfterSeconds { get; init; } = 1;
}

public sealed class AgentCapacityOptionsValidator
    : IValidateOptions<AgentCapacityOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentCapacityOptions options)
    {
        var failures = new List<string>();
        if (options.MaximumConcurrentExpensiveRequests is < 1 or > 256)
            failures.Add(
                "AgentCapacity:MaximumConcurrentExpensiveRequests must be from 1 through 256.");
        if (options.RetryAfterSeconds is < 1 or > 60)
            failures.Add("AgentCapacity:RetryAfterSeconds must be from 1 through 60.");
        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

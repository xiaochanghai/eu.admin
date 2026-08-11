using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentRateLimitOptions
{
    public const string SectionName = "AgentRateLimit";

    public bool Enabled { get; init; } = true;

    public int GeneralPermitLimit { get; init; } = 600;

    public int ExpensivePermitLimit { get; init; } = 30;

    public int WindowSeconds { get; init; } = 60;
}

public sealed class AgentRateLimitOptionsValidator
    : IValidateOptions<AgentRateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentRateLimitOptions options)
    {
        var failures = new List<string>();
        if (options.GeneralPermitLimit is < 1 or > 10_000)
            failures.Add("AgentRateLimit:GeneralPermitLimit must be from 1 through 10000.");
        if (options.ExpensivePermitLimit is < 1 or > 1_000)
            failures.Add("AgentRateLimit:ExpensivePermitLimit must be from 1 through 1000.");
        if (options.ExpensivePermitLimit > options.GeneralPermitLimit)
            failures.Add("AgentRateLimit:ExpensivePermitLimit cannot exceed GeneralPermitLimit.");
        if (options.WindowSeconds is < 1 or > 3_600)
            failures.Add("AgentRateLimit:WindowSeconds must be from 1 through 3600.");
        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

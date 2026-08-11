using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentIdempotencyOptions
{
    public const string SectionName = "AgentIdempotency";

    public bool Enabled { get; init; } = true;

    public int RetentionHours { get; init; } = 24;

    public int MaximumCachedResponseBytes { get; init; } = 4_194_304;
}

public sealed class AgentIdempotencyOptionsValidator
    : IValidateOptions<AgentIdempotencyOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentIdempotencyOptions options)
    {
        var failures = new List<string>();
        if (options.RetentionHours is < 1 or > 168)
            failures.Add("AgentIdempotency:RetentionHours must be from 1 through 168.");
        if (options.MaximumCachedResponseBytes is < 16_384 or > 4_194_304)
            failures.Add(
                "AgentIdempotency:MaximumCachedResponseBytes must be from 16384 through 4194304.");
        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

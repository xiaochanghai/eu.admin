namespace EU.Core.Agent.Api.Configuration;

public sealed class AgentControlOptions
{
    public const string SectionName = "AgentControl";

    public string[] ModelProfileIds { get; init; } = [];
}

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentControlOptions
{
    public const string SectionName = "AgentControl";

    public string[] ModelProfileIds { get; init; } = [];
}

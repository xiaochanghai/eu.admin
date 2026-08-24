namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentTaskWorkerOptions
{
    public const string SectionName = "AgentTaskWorker";

    public bool Enabled { get; set; }

    public string WorkerId { get; set; } = string.Empty;

    public int PollIntervalSeconds { get; set; } = 2;

    public int LeaseSeconds { get; set; } = 300;

    public int RetryDelaySeconds { get; set; } = 30;

    public string[] ExecutionPermissions { get; set; } = ["agent.chat"];
}

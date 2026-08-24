using EU.Core.IServices.Tasks;

#nullable enable

namespace EU.Core.Api.Agent.Background;

public sealed record AgentTaskExecutionContext(
    AgentTaskRecord Task,
    string WorkerId,
    TimeSpan LeaseDuration);

public interface IAgentTaskExecutor
{
    string SourceType { get; }
    Task ExecuteAsync(AgentTaskExecutionContext context, CancellationToken cancellationToken);
}

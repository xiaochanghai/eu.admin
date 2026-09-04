using EU.Core.IServices.Tasks;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgAgentTaskServices 服务契约

public interface IAgAgentTaskServices : IBaseServices<AgAgentTask>
{
    Task<AgentTaskRecord> CreateAsync(CreateAgentTaskCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentTaskRecord>> ListAsync(AgentTaskQuery query, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord?> GetAsync(Guid id, string tenantId, string? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentTaskAttemptRecord>> ListAttemptsAsync(Guid taskId, string tenantId, string? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentTaskEventRecord>> ListEventsAsync(Guid taskId, string tenantId, string? userId, int take = 200, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord?> TryClaimNextAsync(ClaimAgentTaskCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> RenewLeaseAsync(RenewAgentTaskLeaseCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> SaveCheckpointAsync(SaveAgentTaskCheckpointCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> WaitAsync(WaitAgentTaskCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> CompleteAsync(CompleteAgentTaskCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> FailAsync(FailAgentTaskCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> ResumeWithUserInputAsync(ResumeAgentTaskWithUserInputCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord?> SynchronizeRunAsync(SynchronizeAgentTaskRunCommand command, CancellationToken cancellationToken = default);
    Task<AgentTaskRecord> CancelAsync(Guid id, string tenantId, string userId, DateTimeOffset cancelledAtUtc, CancellationToken cancellationToken = default);
}

#endregion

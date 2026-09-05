using EU.Core.IServices.Tasks;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgAgentTaskServices 服务契约

/// <summary>
/// 定义可恢复 Agent 任务的持久化与状态转换服务。
/// </summary>
public interface IAgAgentTaskServices : IBaseServices<AgAgentTask>
{
    /// <summary>创建Agent 任务。</summary>
    Task<AgentTaskRecord> CreateAsync(CreateAgentTaskCommand command, CancellationToken cancellationToken = default);
    /// <summary>查询Agent 任务列表。</summary>
    Task<IReadOnlyList<AgentTaskRecord>> ListAsync(AgentTaskQuery query, CancellationToken cancellationToken = default);
    /// <summary>获取Agent 任务。</summary>
    Task<AgentTaskRecord?> GetAsync(Guid id, string tenantId, string? userId, CancellationToken cancellationToken = default);
    /// <summary>查询 Agent 任务的执行尝试记录。</summary>
    Task<IReadOnlyList<AgentTaskAttemptRecord>> ListAttemptsAsync(Guid taskId, string tenantId, string? userId, CancellationToken cancellationToken = default);
    /// <summary>查询Agent 任务事件列表。</summary>
    Task<IReadOnlyList<AgentTaskEventRecord>> ListEventsAsync(Guid taskId, string tenantId, string? userId, int take = 200, CancellationToken cancellationToken = default);
    /// <summary>尝试认领下一个可执行的 Agent 任务。</summary>
    Task<AgentTaskRecord?> TryClaimNextAsync(ClaimAgentTaskCommand command, CancellationToken cancellationToken = default);
    /// <summary>续订 Agent 任务租约。</summary>
    Task<AgentTaskRecord> RenewLeaseAsync(RenewAgentTaskLeaseCommand command, CancellationToken cancellationToken = default);
    /// <summary>保存 Agent 任务检查点。</summary>
    Task<AgentTaskRecord> SaveCheckpointAsync(SaveAgentTaskCheckpointCommand command, CancellationToken cancellationToken = default);
    /// <summary>将 Agent 任务转换为等待状态并保存检查点。</summary>
    Task<AgentTaskRecord> WaitAsync(WaitAgentTaskCommand command, CancellationToken cancellationToken = default);
    /// <summary>完成Agent 任务。</summary>
    Task<AgentTaskRecord> CompleteAsync(CompleteAgentTaskCommand command, CancellationToken cancellationToken = default);
    /// <summary>记录Agent 任务失败并按规则安排重试。</summary>
    Task<AgentTaskRecord> FailAsync(FailAgentTaskCommand command, CancellationToken cancellationToken = default);
    /// <summary>使用新的用户输入恢复 Agent 任务。</summary>
    Task<AgentTaskRecord> ResumeWithUserInputAsync(ResumeAgentTaskWithUserInputCommand command, CancellationToken cancellationToken = default);
    /// <summary>根据关联运行结果同步 Agent 任务状态。</summary>
    Task<AgentTaskRecord?> SynchronizeRunAsync(SynchronizeAgentTaskRunCommand command, CancellationToken cancellationToken = default);
    /// <summary>取消Agent 任务。</summary>
    Task<AgentTaskRecord> CancelAsync(Guid id, string tenantId, string userId, DateTimeOffset cancelledAtUtc, CancellationToken cancellationToken = default);
}

#endregion

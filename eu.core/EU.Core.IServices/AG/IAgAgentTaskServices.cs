using EU.Core.IServices.Tasks;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IAgAgentTaskServices 服务契约

/// <summary>
/// 定义可恢复 Agent 任务的持久化与状态转换服务。
/// </summary>
public interface IAgAgentTaskServices : IBaseServices<AgAgentTask>
{
    #region 创建Agent 任务。
    /// <summary>创建Agent 任务。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>新建的待执行任务；幂等键已存在且请求内容一致时返回原任务。</returns>
    Task<AgentTaskRecord> CreateAsync(CreateAgentTaskCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 查询Agent 任务列表。
    /// <summary>查询Agent 任务列表。</summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和用户下匹配状态的任务列表，按创建时间倒序、标识升序排列。</returns>
    Task<IReadOnlyList<AgentTaskRecord>> ListAsync(AgentTaskQuery query, CancellationToken cancellationToken = default);
    #endregion
    #region 获取Agent 任务。
    /// <summary>获取Agent 任务。</summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配租户及可选用户条件的未删除任务；不存在时为 null。</returns>
    Task<AgentTaskRecord?> GetAsync(Guid id, string tenantId, string? userId, CancellationToken cancellationToken = default);
    #endregion
    #region 查询 Agent 任务的执行尝试记录。
    /// <summary>查询 Agent 任务的执行尝试记录。</summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按尝试次数升序排列的任务执行记录；任务不可访问时抛出 NotFound 异常。</returns>
    Task<IReadOnlyList<AgentTaskAttemptRecord>> ListAttemptsAsync(Guid taskId, string tenantId, string? userId, CancellationToken cancellationToken = default);
    #endregion
    #region 查询Agent 任务事件列表。
    /// <summary>查询Agent 任务事件列表。</summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>最近指定条数的任务事件，按发生时间及创建时间升序返回；任务不可访问时抛出 NotFound 异常。</returns>
    Task<IReadOnlyList<AgentTaskEventRecord>> ListEventsAsync(Guid taskId, string tenantId, string? userId, int take = 200, CancellationToken cancellationToken = default);
    #endregion
    #region 尝试认领下一个可执行的 Agent 任务。
    /// <summary>尝试认领下一个可执行的 Agent 任务。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功认领并更新租约的任务；本轮候选中没有可认领任务时为 null。</returns>
    Task<AgentTaskRecord?> TryClaimNextAsync(ClaimAgentTaskCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 续订 Agent 任务租约。
    /// <summary>续订 Agent 任务租约。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>续租并递增逻辑版本后的任务；租约、状态或版本不匹配时抛出冲突异常。</returns>
    Task<AgentTaskRecord> RenewLeaseAsync(RenewAgentTaskLeaseCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 保存 Agent 任务检查点。
    /// <summary>保存 Agent 任务检查点。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保存检查点后的任务；租约、状态或版本不匹配时抛出冲突异常。</returns>
    Task<AgentTaskRecord> SaveCheckpointAsync(SaveAgentTaskCheckpointCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 将 Agent 任务转换为等待状态并保存检查点。
    /// <summary>将 Agent 任务转换为等待状态并保存检查点。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>已释放租约并进入等待审批或等待用户状态的任务。</returns>
    Task<AgentTaskRecord> WaitAsync(WaitAgentTaskCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 完成Agent 任务。
    /// <summary>完成Agent 任务。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>已记录完成时间并释放租约的已完成任务。</returns>
    Task<AgentTaskRecord> CompleteAsync(CompleteAgentTaskCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 记录Agent 任务失败并按规则安排重试。
    /// <summary>记录Agent 任务失败并按规则安排重试。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>尝试次数耗尽时返回失败任务，否则返回已安排延迟重试的待执行任务。</returns>
    Task<AgentTaskRecord> FailAsync(FailAgentTaskCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 使用新的用户输入恢复 Agent 任务。
    /// <summary>使用新的用户输入恢复 Agent 任务。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保存用户输入并重新置为待执行的任务；仅允许恢复指定版本的等待用户任务。</returns>
    Task<AgentTaskRecord> ResumeWithUserInputAsync(ResumeAgentTaskWithUserInputCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 根据关联运行结果同步 Agent 任务状态。
    /// <summary>根据关联运行结果同步 Agent 任务状态。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>同步终态后的任务，或原有终态任务；没有匹配运行和所属用户的任务时为 null。</returns>
    Task<AgentTaskRecord?> SynchronizeRunAsync(SynchronizeAgentTaskRunCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 取消Agent 任务。
    /// <summary>取消Agent 任务。</summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancelledAtUtc">取消时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>取消后的任务；原任务已取消时原样返回，其他终态或并发状态变化会导致冲突异常。</returns>
    Task<AgentTaskRecord> CancelAsync(Guid id, string tenantId, string userId, DateTimeOffset cancelledAtUtc, CancellationToken cancellationToken = default);
    #endregion
}

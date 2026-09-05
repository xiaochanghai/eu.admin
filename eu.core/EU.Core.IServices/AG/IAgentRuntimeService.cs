using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IAgentRuntimeService 服务契约

/// <summary>
/// 定义 Agent 运行的准备与启动服务。
/// </summary>
public interface IAgentRuntimeService
{
    #region 根据 Agent 标识准备一次运行。
    /// <summary>根据 Agent 标识准备一次运行。</summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>最新发布快照的运行准备结果，成功时包含运行上下文，校验失败时包含错误信息。</returns>
    Task<AgentRunPreparationResult> PrepareAsync(Guid agentId, string? input, CancellationToken cancellationToken = default);
    #endregion

    #region 根据指定 Agent 版本准备一次运行。
    /// <summary>根据指定 Agent 版本准备一次运行。</summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定已发布版本的运行准备结果，成功时包含运行上下文，校验失败时包含错误信息。</returns>
    Task<AgentRunPreparationResult> PrepareVersionAsync(Guid agentId, Guid agentVersionId, string? input, CancellationToken cancellationToken = default);
    #endregion

    #region 启动Agent 运行并流式返回事件。
    /// <summary>启动Agent 运行并流式返回事件。</summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按执行顺序产生的异步事件流。</returns>
    IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, CancellationToken cancellationToken = default);
    #endregion

    #region 查询 Agent 运行审计记录。
    /// <summary>查询 Agent 运行审计记录。</summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定 Agent 最近的运行审计记录，最多 100 条。</returns>
    Task<IReadOnlyList<AgentRunAuditRecord>> ListAuditAsync(Guid agentId, int take, CancellationToken cancellationToken = default);
    #endregion

    #region 终止已准备但不再执行的 Agent 运行。
    /// <summary>终止已准备但不再执行的 Agent 运行。</summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task TerminatePreparedRunAsync(AgentRunContext context, AgentRunStatus status, string errorCode, CancellationToken cancellationToken = default);
    #endregion
}

using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgentRuntimeService 服务契约

/// <summary>
/// 定义 Agent 运行的准备与启动服务。
/// </summary>
public interface IAgentRuntimeService
{
    /// <summary>根据 Agent 标识准备一次运行。</summary>
    Task<AgentRunPreparationResult> PrepareAsync(Guid agentId, string? input, CancellationToken cancellationToken = default);

    /// <summary>根据指定 Agent 版本准备一次运行。</summary>
    Task<AgentRunPreparationResult> PrepareVersionAsync(Guid agentId, Guid agentVersionId, string? input, CancellationToken cancellationToken = default);

    /// <summary>启动Agent 运行并流式返回事件。</summary>
    IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, CancellationToken cancellationToken = default);

    /// <summary>查询 Agent 运行审计记录。</summary>
    Task<IReadOnlyList<AgentRunAuditRecord>> ListAuditAsync(Guid agentId, int take, CancellationToken cancellationToken = default);

    /// <summary>终止已准备但不再执行的 Agent 运行。</summary>
    Task TerminatePreparedRunAsync(AgentRunContext context, AgentRunStatus status, string errorCode, CancellationToken cancellationToken = default);
}

#endregion

using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

// 文件职责：MainAgentAssignmentService 职责实现

/// <summary>
/// 管理租户范围内的主 Agent 分配。
/// </summary>
/// <param name="agents">用于查询 Agent 定义及已发布版本的目录。</param>
/// <param name="assignments">用于读取和持久化主 Agent 分配记录的仓储。</param>
public sealed class MainAgentAssignmentService(
    IAgentDefinitionCatalog agents,
    IMainAgentAssignmentRepository assignments) : BaseServices, IMainAgentAssignmentService
{
    private readonly IAgentDefinitionCatalog _agents = agents ?? throw new ArgumentNullException(nameof(agents));
    private readonly IMainAgentAssignmentRepository _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含主 Agent 固定版本分配，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<MainAgentAssignment>> GetAsync(CancellationToken cancellationToken = default)
    {
        MainAgentAssignment? assignment = await _assignments.GetAsync(cancellationToken);
        if (assignment is null)
        {
            return Failure(
                MainAgentErrorCodes.NotConfigured,
                "No Main Agent is configured.");
        }

        AgentDefinition? agent = await _agents.GetDefinitionAsync(assignment.AgentId, cancellationToken);
        if (agent is null)
        {
            return Failure(
                MainAgentErrorCodes.AgentNotFound,
                "The configured Main Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return Failure(
                MainAgentErrorCodes.AgentDisabled,
                "The configured Main Agent is disabled.");
        }

        AgentVersion? latestPublished = LatestPublishedVersion(agent);
        if (latestPublished?.Snapshot is null)
        {
            return Failure(
                MainAgentErrorCodes.VersionMissing,
                "The configured Main Agent has no published snapshot.");
        }

        // The assignment selects an Agent, while every new Unified Chat run uses
        // that Agent's latest published snapshot. Existing runs keep their own
        // version ID in the persisted run record for historical replay.
        return Success(assignment with { AgentVersionId = latestPublished.Id });
    }
    #endregion

    #region 设置（SetAsync）
    /// <summary>
    /// 设置（SetAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含主 Agent 固定版本分配，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<MainAgentAssignment>> SetAsync(SetMainAgentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        AgentDefinition? agent = await _agents.GetDefinitionAsync(command.AgentId, cancellationToken);
        if (agent is null)
        {
            return Failure(
                MainAgentErrorCodes.AgentNotFound,
                "The selected Main Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return Failure(
                MainAgentErrorCodes.AgentDisabled,
                "The selected Main Agent is disabled.");
        }

        AgentVersion? currentPublished = LatestPublishedVersion(agent);
        if (currentPublished?.Snapshot is null)
        {
            return Failure(
                MainAgentErrorCodes.VersionMissing,
                "The selected Main Agent has no published snapshot.");
        }

        if (command.ExpectedLogicalRevision == long.MaxValue)
        {
            return Failure(
                MainAgentErrorCodes.RowVersionConflict,
                "The Main Agent assignment was changed by another request.");
        }

        var assignment = new MainAgentAssignment(
            agent.Id,
            currentPublished.Id,
            command.ExpectedLogicalRevision is null ? 0 : command.ExpectedLogicalRevision.Value + 1,
            DateTimeOffset.UtcNow);
        bool replaced = await _assignments.TryReplaceAsync(
            assignment,
            command.ExpectedLogicalRevision,
            cancellationToken);
        return replaced
            ? Success(assignment)
            : Failure(
                MainAgentErrorCodes.RowVersionConflict,
                "The Main Agent assignment was changed by another request.");
    }
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<MainAgentAssignment> Failure(string code, string message) =>
        ServiceResult<MainAgentAssignment>.Failure(
            MainAgentServiceStatusCodes.FromErrorCode(code),
            message);
    #endregion

    #region 处理（LatestPublishedVersion）
    /// <summary>
    /// 处理（LatestPublishedVersion）
    /// </summary>
    /// <param name="agent">Agent 定义。</param>
    /// <returns>发布版本集合的最后一个版本；尚未发布时为 null。</returns>
    private static AgentVersion? LatestPublishedVersion(AgentDefinition agent) =>
        agent.PublishedVersions.Count == 0
            ? null
            : agent.PublishedVersions[^1];
    #endregion
}

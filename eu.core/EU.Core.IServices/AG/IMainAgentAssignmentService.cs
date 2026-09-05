using EU.Core.IServices.MainAgent;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IMainAgentAssignmentService 服务契约

/// <summary>
/// 定义主 Agent 分配的应用服务。
/// </summary>
public interface IMainAgentAssignmentService
{
    /// <summary>获取主 Agent 分配。</summary>
    Task<ServiceResult<MainAgentAssignment>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>设置主 Agent 分配。</summary>
    Task<ServiceResult<MainAgentAssignment>> SetAsync(SetMainAgentCommand command, CancellationToken cancellationToken = default);
}

#endregion

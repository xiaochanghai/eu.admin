using EU.Core.IServices.MainAgent;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IMainAgentAssignmentService 服务契约

/// <summary>
/// 定义主 Agent 分配的应用服务。
/// </summary>
public interface IMainAgentAssignmentService
{
    #region 获取主 Agent 分配。
    /// <summary>获取主 Agent 分配。</summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含主 Agent 固定版本分配，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<MainAgentAssignment>> GetAsync(CancellationToken cancellationToken = default);
    #endregion

    #region 设置主 Agent 分配。
    /// <summary>设置主 Agent 分配。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含主 Agent 固定版本分配，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<MainAgentAssignment>> SetAsync(SetMainAgentCommand command, CancellationToken cancellationToken = default);
    #endregion
}

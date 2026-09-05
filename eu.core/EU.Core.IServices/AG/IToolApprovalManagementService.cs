using EU.Core.IServices.Approvals;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IToolApprovalManagementService 服务契约

/// <summary>
/// 定义工具调用审批的查询与决策服务。
/// </summary>
public interface IToolApprovalManagementService
{
    #region 查询工具调用审批列表。
    /// <summary>查询工具调用审批列表。</summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>刷新过期状态后仍匹配筛选条件的审批请求集合；读取过程中可能持久化过期状态。</returns>
    Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        string tenantId,
        ToolApprovalStatus? status,
        int take,
        CancellationToken cancellationToken = default);
    #endregion

    #region 获取工具调用审批。
    /// <summary>获取工具调用审批。</summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下刷新过期状态后的审批请求；不存在时为 null，读取过程中可能持久化过期状态。</returns>
    Task<ToolApprovalRequestRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询工具审批的决策历史。
    /// <summary>查询工具审批的决策历史。</summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下该审批请求的决策历史。</returns>
    Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 提交工具调用审批决策。
    /// <summary>提交工具调用审批决策。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功持久化批准、拒绝或取消决策后的审批请求；无效状态、过期或版本冲突会抛出审批异常。</returns>
    Task<ToolApprovalRequestRecord> DecideAsync(ToolApprovalDecisionCommand command, CancellationToken cancellationToken = default);
    #endregion
}

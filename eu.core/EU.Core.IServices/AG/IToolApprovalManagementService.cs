using EU.Core.IServices.Approvals;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IToolApprovalManagementService 服务契约

/// <summary>
/// 定义工具调用审批的查询与决策服务。
/// </summary>
public interface IToolApprovalManagementService
{
    /// <summary>查询工具调用审批列表。</summary>
    Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        string tenantId,
        ToolApprovalStatus? status,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>获取工具调用审批。</summary>
    Task<ToolApprovalRequestRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>查询工具审批的决策历史。</summary>
    Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>提交工具调用审批决策。</summary>
    Task<ToolApprovalRequestRecord> DecideAsync(ToolApprovalDecisionCommand command, CancellationToken cancellationToken = default);
}

#endregion

using EU.Core.IServices.Approvals;

#nullable enable

namespace EU.Core.IServices;

public interface IToolApprovalManagementService
{
    Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        string tenantId,
        ToolApprovalStatus? status,
        int take,
        CancellationToken cancellationToken = default);

    Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<ToolApprovalRequestRecord> DecideAsync(
        ToolApprovalDecisionCommand command,
        CancellationToken cancellationToken = default);
}

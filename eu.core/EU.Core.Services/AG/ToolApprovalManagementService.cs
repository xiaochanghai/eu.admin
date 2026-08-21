using EU.Core.IServices.Approvals;
#nullable enable

namespace EU.Core.Services;

public sealed class ToolApprovalManagementService(
    IToolApprovalRepository approvals,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        string tenantId,
        ToolApprovalStatus? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ToolApprovalRequestRecord> values = await approvals.ListAsync(
            new ToolApprovalQuery(tenantId, status, take),
            cancellationToken);
        DateTimeOffset now = _timeProvider.GetUtcNow();
        var refreshed = new List<ToolApprovalRequestRecord>(values.Count);
        foreach (ToolApprovalRequestRecord value in values)
        {
            ToolApprovalRequestRecord current = await ExpireIfNeededAsync(
                value,
                now,
                cancellationToken);
            if (status is null || current.Status == status.Value)
            {
                refreshed.Add(current);
            }
        }

        return refreshed;
    }

    public async Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalRequestRecord? value = await approvals.GetAsync(
            id,
            tenantId,
            cancellationToken);
        return value is null
            ? null
            : await ExpireIfNeededAsync(
                value,
                _timeProvider.GetUtcNow(),
                cancellationToken);
    }

    public Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default) =>
        approvals.ListDecisionsAsync(id, tenantId, cancellationToken);

    public async Task<ToolApprovalRequestRecord> DecideAsync(
        ToolApprovalDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ApprovalId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.TenantId)
            || string.IsNullOrWhiteSpace(command.ActorUserId)
            || !Enum.IsDefined(command.Action))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.Invalid,
                "The tool approval decision is invalid.");
        }

        ToolApprovalRequestRecord current = await approvals.GetAsync(
            command.ApprovalId,
            command.TenantId,
            cancellationToken) ?? throw new ToolApprovalException(
                ToolApprovalErrorCodes.InvalidState,
                "The tool approval is not available.");

        ToolApprovalRequestRecord replacement;
        try
        {
            replacement = command.Action switch
            {
                ToolApprovalDecisionAction.Approve =>
                    ToolApprovalStateMachine.Approve(
                        current,
                        command.ActorUserId,
                        command.Reason,
                        command.DecidedAtUtc),
                ToolApprovalDecisionAction.Reject =>
                    ToolApprovalStateMachine.Reject(
                        current,
                        command.ActorUserId,
                        command.Reason,
                        command.DecidedAtUtc),
                ToolApprovalDecisionAction.Cancel =>
                    ToolApprovalStateMachine.Cancel(
                        current,
                        command.ActorUserId,
                        command.Reason,
                        command.DecidedAtUtc),
                _ => throw new ToolApprovalException(
                    ToolApprovalErrorCodes.Invalid,
                    "The tool approval decision is invalid.")
            };
        }
        catch (ToolApprovalException exception) when (
            exception.ErrorCode == ToolApprovalErrorCodes.Expired)
        {
            ToolApprovalRequestRecord expired = ToolApprovalStateMachine.Expire(
                current,
                command.DecidedAtUtc);
            await approvals.TryReplaceAsync(
                expired,
                current.LogicalRevision,
                cancellationToken);
            throw;
        }

        if (!await approvals.TryReplaceAsync(
                replacement,
                current.LogicalRevision,
                cancellationToken))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.InvalidState,
                "The tool approval was already decided or changed.");
        }

        return replacement;
    }

    private async Task<ToolApprovalRequestRecord> ExpireIfNeededAsync(
        ToolApprovalRequestRecord value,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ToolApprovalRequestRecord current = value;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (current.Status is not (ToolApprovalStatus.Pending or ToolApprovalStatus.Approved)
                || now < current.ExpiresAtUtc)
            {
                return current;
            }

            ToolApprovalRequestRecord expired = ToolApprovalStateMachine.Expire(current, now);
            if (await approvals.TryReplaceAsync(
                    expired,
                    current.LogicalRevision,
                    cancellationToken))
            {
                return expired;
            }

            current = await approvals.GetAsync(
                current.Id,
                current.TenantId,
                cancellationToken) ?? current;
        }

        return current;
    }
}

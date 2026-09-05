using EU.Core.IServices.Approvals;
#nullable enable

namespace EU.Core.Services;

// 文件职责：ToolApprovalManagementService 职责实现

/// <summary>
/// 管理工具调用审批的查询与决策。
/// </summary>
/// <param name="approvals">用于读取和持久化工具审批请求的仓储。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器；为 null 时使用系统时间提供器。</param>
public sealed class ToolApprovalManagementService(
    IToolApprovalRepository approvals,
    TimeProvider? timeProvider = null) : IToolApprovalManagementService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>刷新过期状态后仍匹配筛选条件的审批请求集合；读取过程中可能持久化过期状态。</returns>
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
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下刷新过期状态后的审批请求；不存在时为 null，读取过程中可能持久化过期状态。</returns>
    public async Task<ToolApprovalRequestRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
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
    #endregion

    #region 查询列表（ListDecisionsAsync）
    /// <summary>
    /// 查询列表（ListDecisionsAsync）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下该审批请求的决策历史。</returns>
    public Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
        approvals.ListDecisionsAsync(id, tenantId, cancellationToken);
    #endregion

    #region 处理（DecideAsync）
    /// <summary>
    /// 处理（DecideAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功持久化批准、拒绝或取消决策后的审批请求；无效状态、过期或版本冲突会抛出审批异常。</returns>
    public async Task<ToolApprovalRequestRecord> DecideAsync(ToolApprovalDecisionCommand command, CancellationToken cancellationToken = default)
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
    #endregion

    #region 处理（ExpireIfNeededAsync）
    /// <summary>
    /// 处理（ExpireIfNeededAsync）
    /// </summary>
    /// <param name="value">本次操作使用的工具审批请求记录。</param>
    /// <param name="now">当前时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>过期转换成功后的审批请求，或无需转换、有限重试后重新读取的当前记录；不保证并发冲突后已过期。</returns>
    private async Task<ToolApprovalRequestRecord> ExpireIfNeededAsync(ToolApprovalRequestRecord value, DateTimeOffset now, CancellationToken cancellationToken)
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
    #endregion
}

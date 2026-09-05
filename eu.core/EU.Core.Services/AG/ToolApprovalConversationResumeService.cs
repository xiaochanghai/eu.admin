using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

// 文件职责：ToolApprovalConversationResumeService 职责实现

/// <summary>
/// 在工具审批完成后恢复对应会话运行。
/// </summary>
/// <param name="approvals">用于读取和持久化工具审批请求的仓储。</param>
/// <param name="runtime">用于恢复并执行获批工具调用的服务。</param>
/// <param name="unifiedEntries">用于读取和持久化统一入口会话、运行及事件的仓储。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器。</param>
public sealed class ToolApprovalConversationResumeService(
    IToolApprovalRepository approvals,
    ToolApprovalRuntimeService runtime,
    IUnifiedEntryRepository unifiedEntries,
    TimeProvider timeProvider)
{
    #region 处理（ResumeAsync）
    /// <summary>
    /// 处理（ResumeAsync）
    /// </summary>
    /// <param name="approvalId">工具审批请求标识。</param>
    /// <param name="requester">请求发起方。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批结果投影后的会话运行状态和输出，或已有终态结果；不可恢复状态及投影失败抛出审批异常。</returns>
    public async Task<ToolApprovalConversationResumeResult> ResumeAsync(
        Guid approvalId,
        AgentExecutionIdentity requester,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requester);
        ToolApprovalRequestRecord approval = await approvals.GetAsync(
            approvalId,
            requester.TenantId,
            cancellationToken) ?? throw InvalidState();
        if (!string.Equals(
                approval.RequesterUserId,
                requester.UserId,
                StringComparison.Ordinal))
        {
            throw InvalidState();
        }

        UnifiedEntryAggregate aggregate = await unifiedEntries.GetAggregateForOwnerAsync(
            approval.EntryRunId,
            requester.TenantId,
            requester.UserId,
            cancellationToken) ?? throw InvalidState();
        bool terminalWithoutExecution = approval.Status is ToolApprovalStatus.Rejected
            or ToolApprovalStatus.Cancelled
            or ToolApprovalStatus.Expired
            or ToolApprovalStatus.Invalidated;
        if (!terminalWithoutExecution && approval.Status == ToolApprovalStatus.Failed)
        {
            terminalWithoutExecution = await approvals.GetExecutionResultAsync(
                approval.Id,
                approval.TenantId,
                cancellationToken) is null;
        }

        if (terminalWithoutExecution)
        {
            return await ProjectTerminalDecisionAsync(
                approval,
                requester,
                aggregate,
                cancellationToken);
        }

        if (aggregate.Details.EntryRun.Status != UnifiedRunStatus.WaitingForApproval)
        {
            if (aggregate.Details.EntryRun.Status is UnifiedRunStatus.Completed
                or UnifiedRunStatus.Failed)
            {
                return ResultFromAggregate(approval, aggregate);
            }

            throw InvalidState();
        }

        McpRuntimeToolResult toolResult = await runtime.ResumeApprovedAsync(
            new ToolApprovalResumeRequest(
                approval.Id,
                approval.LogicalRevision,
                requester,
                timeProvider.GetUtcNow()),
            cancellationToken);
        ToolApprovalRequestRecord completedApproval = await approvals.GetAsync(
            approval.Id,
            approval.TenantId,
            cancellationToken) ?? throw InvalidState();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            aggregate = await unifiedEntries.GetAggregateForOwnerAsync(
                approval.EntryRunId,
                requester.TenantId,
                requester.UserId,
                cancellationToken) ?? throw InvalidState();
            if (aggregate.Details.EntryRun.Status is UnifiedRunStatus.Completed
                or UnifiedRunStatus.Failed)
            {
                return ResultFromAggregate(completedApproval, aggregate);
            }

            if (aggregate.Details.EntryRun.Status != UnifiedRunStatus.WaitingForApproval)
            {
                throw InvalidState();
            }

            UnifiedEntryAggregate projected = Project(
                aggregate,
                completedApproval,
                toolResult);
            try
            {
                UnifiedEntryAggregate saved = await unifiedEntries.SaveAsync(
                    projected,
                    cancellationToken);
                return ResultFromAggregate(completedApproval, saved);
            }
            catch (InvalidOperationException) when (attempt < 2)
            {
                // Reload the optimistic aggregate. The external call result is
                // already durably protected by the approval repository.
            }
        }

        throw new ToolApprovalException(
            ToolApprovalErrorCodes.ExecutionOutcomeUnknown,
            "The approved tool result could not be projected into its conversation.");
    }
    #endregion

    #region 处理（ProjectTerminalDecisionAsync）
    /// <summary>
    /// 处理（ProjectTerminalDecisionAsync）
    /// </summary>
    /// <param name="approval">审批记录。</param>
    /// <param name="requester">请求发起方。</param>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>拒绝、取消、过期等非执行决策投影后的会话结果，或已有终态结果；冲突时重新加载并有限重试。</returns>
    private async Task<ToolApprovalConversationResumeResult>
        ProjectTerminalDecisionAsync(
            ToolApprovalRequestRecord approval,
            AgentExecutionIdentity requester,
            UnifiedEntryAggregate aggregate,
            CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (aggregate.Details.EntryRun.Status is UnifiedRunStatus.Completed
                or UnifiedRunStatus.Failed
                or UnifiedRunStatus.Cancelled)
            {
                return ResultFromAggregate(approval, aggregate);
            }

            if (aggregate.Details.EntryRun.Status != UnifiedRunStatus.WaitingForApproval)
            {
                throw InvalidState();
            }

            try
            {
                UnifiedEntryAggregate saved = await unifiedEntries.SaveAsync(
                    Project(aggregate, approval, toolResult: null),
                    cancellationToken);
                return ResultFromAggregate(approval, saved);
            }
            catch (InvalidOperationException) when (attempt < 2)
            {
                aggregate = await unifiedEntries.GetAggregateForOwnerAsync(
                    approval.EntryRunId,
                    requester.TenantId,
                    requester.UserId,
                    cancellationToken) ?? throw InvalidState();
            }
        }

        throw new ToolApprovalException(
            ToolApprovalErrorCodes.ExecutionOutcomeUnknown,
            "The approval decision could not be projected into its conversation.");
    }
    #endregion

    #region 处理（Project）
    /// <summary>
    /// 处理（Project）
    /// </summary>
    /// <param name="aggregate">聚合状态。</param>
    /// <param name="approval">审批记录。</param>
    /// <param name="toolResult">工具调用结果。</param>
    /// <returns>根据审批终态更新入口、子运行、工具、消息和事件后的聚合副本；不执行持久化。</returns>
    private UnifiedEntryAggregate Project(UnifiedEntryAggregate aggregate, ToolApprovalRequestRecord approval, McpRuntimeToolResult? toolResult)
    {
        DateTimeOffset finishedAt = approval.FinishedAtUtc
            ?? timeProvider.GetUtcNow();
        bool succeeded = approval.Status == ToolApprovalStatus.Consumed
            && toolResult?.Succeeded == true;
        (UnifiedRunStatus status, string errorCode, string narrative) =
            approval.Status switch
            {
                ToolApprovalStatus.Consumed => (
                    UnifiedRunStatus.Completed,
                    string.Empty,
                    string.IsNullOrWhiteSpace(toolResult?.Content)
                        ? "操作已获批准并执行完成。"
                        : toolResult.Content),
                ToolApprovalStatus.Rejected => (
                    UnifiedRunStatus.Failed,
                    ToolApprovalErrorCodes.Rejected,
                    "该工具调用已被拒绝，未执行任何外部操作。"),
                ToolApprovalStatus.Cancelled => (
                    UnifiedRunStatus.Cancelled,
                    ToolApprovalErrorCodes.Cancelled,
                    "该工具调用已取消，未执行任何外部操作。"),
                ToolApprovalStatus.Expired => (
                    UnifiedRunStatus.Failed,
                    ToolApprovalErrorCodes.Expired,
                    "该工具调用审批已过期，未执行任何外部操作。"),
                ToolApprovalStatus.Invalidated => (
                    UnifiedRunStatus.Failed,
                    string.IsNullOrWhiteSpace(approval.ErrorCode)
                        ? ToolApprovalErrorCodes.RevalidationFailed
                        : approval.ErrorCode,
                    "批准已因工具版本、Schema 或权限变化而失效，未执行外部操作。"),
                _ => (
                    UnifiedRunStatus.Failed,
                    string.IsNullOrWhiteSpace(toolResult?.ErrorCode)
                        ? string.IsNullOrWhiteSpace(approval.ErrorCode)
                            ? AgentRunErrorCodes.ToolFailed
                            : approval.ErrorCode
                        : toolResult.ErrorCode,
                    "操作已获批准，但工具执行失败。请查看执行追踪或联系管理员。")
            };
        ProtectedUnifiedPayload protectedNarrative =
            UnifiedEntryPayloadProtector.ProtectInternal(narrative);

        UnifiedEntryRunRecord entry = aggregate.Details.EntryRun with
        {
            Status = status,
            FinishedAtUtc = finishedAt,
            Duration = NonNegative(finishedAt - aggregate.Details.EntryRun.StartedAtUtc),
            Output = protectedNarrative.Content,
            OutputSha256 = protectedNarrative.OriginalSha256,
            ErrorCode = errorCode
        };
        UnifiedAgentRunRecord[] agents = aggregate.Details.AgentRuns
            .Select(value => value.Status is UnifiedRunStatus.Pending
                or UnifiedRunStatus.Running
                or UnifiedRunStatus.WaitingForApproval
                    ? value with
                    {
                        Status = status,
                        FinishedAtUtc = finishedAt,
                        Duration = NonNegative(finishedAt - value.StartedAtUtc),
                        Output = protectedNarrative.Content,
                        OutputSha256 = protectedNarrative.OriginalSha256,
                        ErrorCode = errorCode
                    }
                    : value)
            .ToArray();
        UnifiedOrchestrationRunLink[] orchestrations = aggregate.Details.Orchestrations
            .Select(value => value.Status is UnifiedRunStatus.Pending
                or UnifiedRunStatus.Running
                or UnifiedRunStatus.WaitingForApproval
                    ? value with
                    {
                        Status = status,
                        FinishedAtUtc = finishedAt,
                        Duration = NonNegative(finishedAt - value.StartedAtUtc),
                        Output = protectedNarrative.Content,
                        OutputSha256 = protectedNarrative.OriginalSha256,
                        ErrorCode = errorCode
                    }
                    : value)
            .ToArray();
        UnifiedToolCallRecord[] tools = aggregate.Details.ToolCalls
            .Select(value => value.ToolVersionId == approval.ToolVersionId
                && value.Status == UnifiedRunStatus.WaitingForApproval
                    ? value with
                    {
                        Status = status,
                        FinishedAtUtc = finishedAt,
                        Duration = NonNegative(finishedAt - value.StartedAtUtc),
                        ResultContent = protectedNarrative.Content,
                        ResultSha256 = protectedNarrative.OriginalSha256,
                        ErrorCode = errorCode
                    }
                    : value)
            .ToArray();
        if (!tools.Any(value => value.ToolVersionId == approval.ToolVersionId
            && value.Status == status))
        {
            throw InvalidState();
        }

        var message = new ConversationMessageRecord(
            Guid.NewGuid(),
            aggregate.Conversation.Id,
            ConversationMessageRole.Assistant,
            protectedNarrative.Content,
            protectedNarrative.OriginalSha256,
            protectedNarrative.OriginalUtf8Bytes,
            finishedAt)
        {
            Kind = ConversationMessageKind.AssistantNarrative
        };
        List<UnifiedRunEventRecord> events = aggregate.Events.ToList();
        long sequence = events.Count == 0 ? 0 : events[^1].Sequence;
        AppendEvent(events, entry, approval, "approval-decided", finishedAt, ref sequence,
            new { approvalId = approval.Id, status = approval.Status.ToString() });
        if (toolResult is not null)
        {
            AppendEvent(events, entry, approval, "approval-resumed", finishedAt, ref sequence,
                new { approvalId = approval.Id, toolVersionId = approval.ToolVersionId });
        }
        AppendEvent(events, entry, approval,
            succeeded ? "tool-succeeded" : toolResult is null ? "tool-blocked" : "tool-failed",
            finishedAt,
            ref sequence,
            new
            {
                approvalId = approval.Id,
                toolVersionId = approval.ToolVersionId,
                toolName = approval.ToolName,
                text = protectedNarrative.Content,
                errorCode
            });
        AppendEvent(events, entry, approval,
            status == UnifiedRunStatus.Completed
                ? "completed"
                : status == UnifiedRunStatus.Cancelled ? "cancelled" : "failed",
            finishedAt,
            ref sequence,
            new { approvalId = approval.Id, text = protectedNarrative.Content, errorCode });

        return new UnifiedEntryAggregate(
            aggregate.Conversation with { UpdatedAtUtc = finishedAt },
            aggregate.Messages.Append(message).ToArray(),
            new UnifiedRunDetails(entry, agents, orchestrations, tools),
            events,
            aggregate.PersistenceRevision);
    }
    #endregion

    #region 处理（AppendEvent）
    /// <summary>
    /// 处理（AppendEvent）
    /// </summary>
    /// <param name="events">事件集合。</param>
    /// <param name="entry">当前处理的入口记录或条目。</param>
    /// <param name="approval">审批记录。</param>
    /// <param name="kind">记录或事件类型。</param>
    /// <param name="occurredAt">事件发生时间（UTC）。</param>
    /// <param name="sequence">事件或记录序号。</param>
    /// <param name="payload">待按展示权限检查和脱敏的事件或工具结果载荷。</param>
    private static void AppendEvent(
        ICollection<UnifiedRunEventRecord> events,
        UnifiedEntryRunRecord entry,
        ToolApprovalRequestRecord approval,
        string kind,
        DateTimeOffset occurredAt,
        ref long sequence,
        object payload)
    {
        string raw = JsonSerializer.Serialize(payload);
        ProtectedUnifiedPayload protectedPayload =
            UnifiedEntryPayloadProtector.ProtectInternal(raw);
        events.Add(new UnifiedRunEventRecord(
            Guid.NewGuid(),
            entry.Id,
            checked(++sequence),
            entry.CorrelationId,
            kind,
            occurredAt,
            approval.AgentRunId,
            0,
            protectedPayload.Content,
            protectedPayload.OriginalSha256));
    }
    #endregion

    #region 处理（ResultFromAggregate）
    /// <summary>
    /// 处理（ResultFromAggregate）
    /// </summary>
    /// <param name="approval">审批记录。</param>
    /// <param name="aggregate">聚合状态。</param>
    /// <returns>从审批标识和聚合当前运行终态、输出及错误码构造的会话恢复结果。</returns>
    private static ToolApprovalConversationResumeResult ResultFromAggregate(ToolApprovalRequestRecord approval, UnifiedEntryAggregate aggregate) =>
        new(
            approval.Id,
            aggregate.Details.EntryRun.Id,
            aggregate.Conversation.Id,
            aggregate.Details.EntryRun.Status,
            aggregate.Details.EntryRun.Output,
            aggregate.Details.EntryRun.ErrorCode);
    #endregion

    #region 处理（NonNegative）
    /// <summary>
    /// 将负时长归零（NonNegative）。
    /// </summary>
    /// <param name="value">待检查的持续时间。</param>
    /// <returns>输入为负数时返回零时长，否则返回原时长。</returns>
    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;
    #endregion

    #region 处理（InvalidState）
    /// <summary>
    /// 处理（InvalidState）
    /// </summary>
    /// <returns>表示审批会话当前状态不允许恢复的 InvalidState 异常。</returns>
    private static ToolApprovalException InvalidState() =>
        new(
            ToolApprovalErrorCodes.InvalidState,
            "The tool approval conversation cannot be resumed from its current state.");
    #endregion
}

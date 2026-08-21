using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

public sealed class ToolApprovalConversationResumeService(
    IToolApprovalRepository approvals,
    ToolApprovalRuntimeService runtime,
    IUnifiedEntryRepository unifiedEntries,
    TimeProvider timeProvider)
{
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

    private UnifiedEntryAggregate Project(
        UnifiedEntryAggregate aggregate,
        ToolApprovalRequestRecord approval,
        McpRuntimeToolResult? toolResult)
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

    private static ToolApprovalConversationResumeResult ResultFromAggregate(
        ToolApprovalRequestRecord approval,
        UnifiedEntryAggregate aggregate) =>
        new(
            approval.Id,
            aggregate.Details.EntryRun.Id,
            aggregate.Conversation.Id,
            aggregate.Details.EntryRun.Status,
            aggregate.Details.EntryRun.Output,
            aggregate.Details.EntryRun.ErrorCode);

    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;

    private static ToolApprovalException InvalidState() =>
        new(
            ToolApprovalErrorCodes.InvalidState,
            "The tool approval conversation cannot be resumed from its current state.");
}

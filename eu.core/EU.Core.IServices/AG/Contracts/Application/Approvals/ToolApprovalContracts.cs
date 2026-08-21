#nullable enable

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Approvals;

public enum ToolApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Expired,
    Consuming,
    Consumed,
    Failed,
    Invalidated
}

public static class ToolApprovalErrorCodes
{
    public const string Invalid = "TOOL_APPROVAL_INVALID";
    public const string InvalidState = "TOOL_APPROVAL_INVALID_STATE";
    public const string Expired = "TOOL_APPROVAL_EXPIRED";
    public const string SelfApprovalForbidden =
        "TOOL_APPROVAL_SELF_APPROVAL_FORBIDDEN";
    public const string CancellationForbidden =
        "TOOL_APPROVAL_CANCELLATION_FORBIDDEN";
    public const string ExecutionFailed = "TOOL_APPROVAL_EXECUTION_FAILED";
    public const string ExecutionOutcomeUnknown =
        "TOOL_APPROVAL_EXECUTION_OUTCOME_UNKNOWN";
    public const string Rejected = "TOOL_APPROVAL_REJECTED";
    public const string Cancelled = "TOOL_APPROVAL_CANCELLED";
    public const string PayloadInvalid = "TOOL_APPROVAL_PAYLOAD_INVALID";
    public const string RevalidationFailed =
        "TOOL_APPROVAL_REVALIDATION_FAILED";
}

public sealed class ToolApprovalException(string errorCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed record ToolApprovalRequestRecord(
    Guid Id,
    string TenantId,
    string RequesterUserId,
    Guid ConversationId,
    Guid EntryRunId,
    Guid AgentRunId,
    Guid AgentVersionId,
    Guid McpServerId,
    Guid ToolVersionId,
    string ToolName,
    McpToolRisk Risk,
    string ToolSchemaSha256,
    string ArgumentsSha256,
    string SafeArgumentsSummaryJson,
    ToolApprovalStatus Status,
    long LogicalRevision,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string DecisionUserId,
    string DecisionReason,
    DateTimeOffset? DecidedAtUtc,
    DateTimeOffset? ClaimedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string ErrorCode);

public sealed record ToolApprovalExecutionClaim(
    ToolApprovalRequestRecord Request,
    string ProtectedResumePayload,
    string ProtectedResumePayloadSha256);

public sealed record ToolApprovalExecutionResultRecord(
    Guid ApprovalId,
    string TenantId,
    bool Succeeded,
    bool Blocked,
    string ProtectedContent,
    string ProtectedContentSha256,
    string ContentSha256,
    string ErrorCode,
    DateTimeOffset FinishedAtUtc);

public sealed record ToolApprovalDecisionRecord(
    Guid Id,
    Guid ApprovalId,
    string TenantId,
    ToolApprovalStatus FromStatus,
    ToolApprovalStatus ToStatus,
    string DecisionUserId,
    string DecisionReason,
    DateTimeOffset DecidedAtUtc,
    long ResultingLogicalRevision);

public sealed record ToolApprovalPayloadContext(
    Guid ApprovalId,
    string TenantId,
    string ArgumentsSha256);

public interface IToolApprovalPayloadProtector
{
    string Protect(ToolApprovalPayloadContext context, string plaintext);

    string Unprotect(ToolApprovalPayloadContext context, string protectedPayload);
}

public sealed record ToolApprovalQuery(
    string TenantId,
    ToolApprovalStatus? Status = null,
    int Take = 100);

public enum ToolApprovalDecisionAction
{
    Approve,
    Reject,
    Cancel
}

public sealed record ToolApprovalDecisionCommand(
    Guid ApprovalId,
    string TenantId,
    string ActorUserId,
    ToolApprovalDecisionAction Action,
    string Reason,
    DateTimeOffset DecidedAtUtc);

public interface IToolApprovalRepository
{
    Task<bool> TryCreateAsync(
        ToolApprovalRequestRecord request,
        string protectedResumePayload,
        CancellationToken cancellationToken = default);

    Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        ToolApprovalQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid approvalId,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);

    Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default);

    Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<int> RecoverInterruptedExecutionsAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

public static partial class ToolApprovalStateMachine
{
    public const int MaximumSafeSummaryUtf8Bytes = 8_192;
    public const int MaximumProtectedPayloadUtf8Bytes = 65_536;
    public const int MaximumResultPlaintextUtf8Bytes = 1_048_576;
    public const int MaximumProtectedResultUtf8Bytes = 1_500_000;
    public const int MaximumDecisionReasonCharacters = 512;
    public const int MaximumTake = 200;

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();

    public static void ValidateNew(
        ToolApprovalRequestRecord value,
        string protectedResumePayload)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Id == Guid.Empty
            || value.ConversationId == Guid.Empty
            || value.EntryRunId == Guid.Empty
            || value.AgentRunId == Guid.Empty
            || value.AgentVersionId == Guid.Empty
            || value.McpServerId == Guid.Empty
            || value.ToolVersionId == Guid.Empty
            || string.IsNullOrWhiteSpace(value.TenantId)
            || value.TenantId.Length > 256
            || string.IsNullOrWhiteSpace(value.RequesterUserId)
            || value.RequesterUserId.Length > 256
            || string.IsNullOrWhiteSpace(value.ToolName)
            || value.ToolName.Length > 256
            || value.Risk is not (McpToolRisk.Mutating or McpToolRisk.HighRisk)
            || !Sha256Pattern().IsMatch(value.ToolSchemaSha256)
            || !Sha256Pattern().IsMatch(value.ArgumentsSha256)
            || value.Status != ToolApprovalStatus.Pending
            || value.LogicalRevision != 0
            || value.RequestedAtUtc >= value.ExpiresAtUtc
            || !string.IsNullOrEmpty(value.DecisionUserId)
            || !string.IsNullOrEmpty(value.DecisionReason)
            || value.DecidedAtUtc is not null
            || value.ClaimedAtUtc is not null
            || value.FinishedAtUtc is not null
            || !string.IsNullOrEmpty(value.ErrorCode))
        {
            throw Invalid();
        }

        ValidateSafeSummary(value.SafeArgumentsSummaryJson);
        ValidateProtectedPayload(protectedResumePayload);
        ValidateStateShape(value);
    }

    public static ToolApprovalRequestRecord Approve(
        ToolApprovalRequestRecord value,
        string decisionUserId,
        string reason,
        DateTimeOffset decidedAtUtc)
    {
        EnsurePendingAndLive(value, decidedAtUtc);
        string actor = RequiredIdentity(decisionUserId);
        if (value.Risk == McpToolRisk.HighRisk
            && string.Equals(
                actor,
                value.RequesterUserId,
                StringComparison.Ordinal))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.SelfApprovalForbidden,
                "High-risk tool requests cannot be self-approved.");
        }

        return value with
        {
            Status = ToolApprovalStatus.Approved,
            LogicalRevision = NextRevision(value.LogicalRevision),
            DecisionUserId = actor,
            DecisionReason = NormalizeReason(reason),
            DecidedAtUtc = decidedAtUtc
        };
    }

    public static ToolApprovalRequestRecord Reject(
        ToolApprovalRequestRecord value,
        string decisionUserId,
        string reason,
        DateTimeOffset decidedAtUtc)
    {
        EnsurePendingAndLive(value, decidedAtUtc);
        return value with
        {
            Status = ToolApprovalStatus.Rejected,
            LogicalRevision = NextRevision(value.LogicalRevision),
            DecisionUserId = RequiredIdentity(decisionUserId),
            DecisionReason = NormalizeReason(reason),
            DecidedAtUtc = decidedAtUtc,
            FinishedAtUtc = decidedAtUtc
        };
    }

    public static ToolApprovalRequestRecord Cancel(
        ToolApprovalRequestRecord value,
        string requesterUserId,
        string reason,
        DateTimeOffset cancelledAtUtc)
    {
        EnsurePendingAndLive(value, cancelledAtUtc);
        string actor = RequiredIdentity(requesterUserId);
        if (!string.Equals(actor, value.RequesterUserId, StringComparison.Ordinal))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.CancellationForbidden,
                "Only the requester can cancel a pending tool approval.");
        }

        return value with
        {
            Status = ToolApprovalStatus.Cancelled,
            LogicalRevision = NextRevision(value.LogicalRevision),
            DecisionUserId = actor,
            DecisionReason = NormalizeReason(reason),
            DecidedAtUtc = cancelledAtUtc,
            FinishedAtUtc = cancelledAtUtc
        };
    }

    public static ToolApprovalRequestRecord Expire(
        ToolApprovalRequestRecord value,
        DateTimeOffset expiredAtUtc)
    {
        if (value.Status is not (ToolApprovalStatus.Pending
            or ToolApprovalStatus.Approved)
            || expiredAtUtc < value.ExpiresAtUtc)
        {
            throw InvalidState();
        }

        return value with
        {
            Status = ToolApprovalStatus.Expired,
            LogicalRevision = NextRevision(value.LogicalRevision),
            FinishedAtUtc = expiredAtUtc,
            ErrorCode = ToolApprovalErrorCodes.Expired
        };
    }

    public static ToolApprovalRequestRecord Claim(
        ToolApprovalRequestRecord value,
        DateTimeOffset claimedAtUtc)
    {
        if (value.Status != ToolApprovalStatus.Approved)
        {
            throw InvalidState();
        }

        if (claimedAtUtc >= value.ExpiresAtUtc)
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.Expired,
                "The tool approval has expired.");
        }

        return value with
        {
            Status = ToolApprovalStatus.Consuming,
            LogicalRevision = NextRevision(value.LogicalRevision),
            ClaimedAtUtc = claimedAtUtc
        };
    }

    public static ToolApprovalRequestRecord Invalidate(
        ToolApprovalRequestRecord value,
        string errorCode,
        DateTimeOffset invalidatedAtUtc)
    {
        if (value.Status != ToolApprovalStatus.Approved
            || value.DecidedAtUtc is null
            || invalidatedAtUtc < value.DecidedAtUtc)
        {
            throw InvalidState();
        }

        return value with
        {
            Status = ToolApprovalStatus.Invalidated,
            LogicalRevision = NextRevision(value.LogicalRevision),
            FinishedAtUtc = invalidatedAtUtc,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                ? ToolApprovalErrorCodes.RevalidationFailed
                : NormalizeErrorCode(errorCode)
        };
    }

    public static ToolApprovalRequestRecord Complete(
        ToolApprovalRequestRecord value,
        bool succeeded,
        string errorCode,
        DateTimeOffset finishedAtUtc)
    {
        if (value.Status != ToolApprovalStatus.Consuming
            || value.ClaimedAtUtc is null
            || finishedAtUtc < value.ClaimedAtUtc)
        {
            throw InvalidState();
        }

        return value with
        {
            Status = succeeded
                ? ToolApprovalStatus.Consumed
                : ToolApprovalStatus.Failed,
            LogicalRevision = NextRevision(value.LogicalRevision),
            FinishedAtUtc = finishedAtUtc,
            ErrorCode = succeeded
                ? string.Empty
                : NormalizeErrorCode(errorCode)
        };
    }

    public static ToolApprovalRequestRecord RecoverUnknownOutcome(
        ToolApprovalRequestRecord value,
        DateTimeOffset recoveredAtUtc) =>
        Complete(
            value,
            succeeded: false,
            ToolApprovalErrorCodes.ExecutionOutcomeUnknown,
            recoveredAtUtc);

    public static void ValidateReplacement(
        ToolApprovalRequestRecord existing,
        ToolApprovalRequestRecord replacement)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(replacement);
        if (!PreservesBinding(existing, replacement)
            || existing.LogicalRevision == long.MaxValue
            || replacement.LogicalRevision != existing.LogicalRevision + 1
            || !AllowedTransition(existing.Status, replacement.Status))
        {
            throw InvalidState();
        }

        ValidateSafeSummary(replacement.SafeArgumentsSummaryJson);
        ValidateStateShape(replacement);
    }

    public static void ValidateQuery(ToolApprovalQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.TenantId)
            || query.Take < 1
            || query.Take > MaximumTake
            || query.Status is not null && !Enum.IsDefined(query.Status.Value))
        {
            throw Invalid();
        }
    }

    public static void ValidateProtectedPayload(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith("enc:v1:", StringComparison.Ordinal)
            || Encoding.UTF8.GetByteCount(value) > MaximumProtectedPayloadUtf8Bytes)
        {
            throw Invalid();
        }
    }

    public static void ValidateExecutionResultEnvelope(
        ToolApprovalExecutionResultRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ApprovalId == Guid.Empty
            || string.IsNullOrWhiteSpace(value.TenantId)
            || value.TenantId.Length > 256
            || string.IsNullOrWhiteSpace(value.ProtectedContent)
            || !value.ProtectedContent.StartsWith("enc:v1:", StringComparison.Ordinal)
            || Encoding.UTF8.GetByteCount(value.ProtectedContent)
                > MaximumProtectedResultUtf8Bytes
            || !Sha256Pattern().IsMatch(value.ProtectedContentSha256)
            || !Sha256Pattern().IsMatch(value.ContentSha256)
            || !string.Equals(
                value.ProtectedContentSha256,
                Sha256(value.ProtectedContent),
                StringComparison.Ordinal)
            || value.Succeeded && value.Blocked
            || value.ErrorCode.Length > 128
            || value.FinishedAtUtc == default)
        {
            throw Invalid();
        }
    }

    private static void EnsurePendingAndLive(
        ToolApprovalRequestRecord value,
        DateTimeOffset occurredAtUtc)
    {
        if (value.Status != ToolApprovalStatus.Pending)
        {
            throw InvalidState();
        }

        if (occurredAtUtc >= value.ExpiresAtUtc)
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.Expired,
                "The tool approval has expired.");
        }
    }

    private static bool PreservesBinding(
        ToolApprovalRequestRecord existing,
        ToolApprovalRequestRecord replacement) =>
        existing.Id == replacement.Id
        && string.Equals(existing.TenantId, replacement.TenantId, StringComparison.Ordinal)
        && string.Equals(existing.RequesterUserId, replacement.RequesterUserId, StringComparison.Ordinal)
        && existing.ConversationId == replacement.ConversationId
        && existing.EntryRunId == replacement.EntryRunId
        && existing.AgentRunId == replacement.AgentRunId
        && existing.AgentVersionId == replacement.AgentVersionId
        && existing.McpServerId == replacement.McpServerId
        && existing.ToolVersionId == replacement.ToolVersionId
        && string.Equals(existing.ToolName, replacement.ToolName, StringComparison.Ordinal)
        && existing.Risk == replacement.Risk
        && string.Equals(existing.ToolSchemaSha256, replacement.ToolSchemaSha256, StringComparison.Ordinal)
        && string.Equals(existing.ArgumentsSha256, replacement.ArgumentsSha256, StringComparison.Ordinal)
        && string.Equals(existing.SafeArgumentsSummaryJson, replacement.SafeArgumentsSummaryJson, StringComparison.Ordinal)
        && existing.RequestedAtUtc == replacement.RequestedAtUtc
        && existing.ExpiresAtUtc == replacement.ExpiresAtUtc;

    private static bool AllowedTransition(
        ToolApprovalStatus from,
        ToolApprovalStatus to) =>
        (from, to) switch
        {
            (ToolApprovalStatus.Pending, ToolApprovalStatus.Approved
                or ToolApprovalStatus.Rejected
                or ToolApprovalStatus.Cancelled
                or ToolApprovalStatus.Expired) => true,
            (ToolApprovalStatus.Approved, ToolApprovalStatus.Consuming
                or ToolApprovalStatus.Expired
                or ToolApprovalStatus.Invalidated) => true,
            (ToolApprovalStatus.Consuming, ToolApprovalStatus.Consumed
                or ToolApprovalStatus.Failed) => true,
            _ => false
        };

    private static void ValidateSafeSummary(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Encoding.UTF8.GetByteCount(value) > MaximumSafeSummaryUtf8Bytes)
        {
            throw Invalid();
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }

            if (!string.Equals(
                UnifiedEntryPayloadProtector.ProtectInternal(value).Content,
                value,
                StringComparison.Ordinal))
            {
                throw Invalid();
            }
        }
        catch (JsonException)
        {
            throw Invalid();
        }
    }

    private static void ValidateStateShape(ToolApprovalRequestRecord value)
    {
        bool valid = value.Status switch
        {
            ToolApprovalStatus.Pending =>
                value.LogicalRevision == 0
                && string.IsNullOrEmpty(value.DecisionUserId)
                && string.IsNullOrEmpty(value.DecisionReason)
                && value.DecidedAtUtc is null
                && value.ClaimedAtUtc is null
                && value.FinishedAtUtc is null
                && string.IsNullOrEmpty(value.ErrorCode),
            ToolApprovalStatus.Approved =>
                !string.IsNullOrWhiteSpace(value.DecisionUserId)
                && value.DecidedAtUtc is not null
                && value.ClaimedAtUtc is null
                && value.FinishedAtUtc is null
                && string.IsNullOrEmpty(value.ErrorCode),
            ToolApprovalStatus.Rejected or ToolApprovalStatus.Cancelled =>
                !string.IsNullOrWhiteSpace(value.DecisionUserId)
                && value.DecidedAtUtc is not null
                && value.FinishedAtUtc is not null
                && value.ClaimedAtUtc is null
                && string.IsNullOrEmpty(value.ErrorCode),
            ToolApprovalStatus.Expired =>
                value.FinishedAtUtc is not null
                && value.ClaimedAtUtc is null
                && string.Equals(
                    value.ErrorCode,
                    ToolApprovalErrorCodes.Expired,
                    StringComparison.Ordinal),
            ToolApprovalStatus.Consuming =>
                !string.IsNullOrWhiteSpace(value.DecisionUserId)
                && value.DecidedAtUtc is not null
                && value.ClaimedAtUtc is not null
                && value.FinishedAtUtc is null
                && string.IsNullOrEmpty(value.ErrorCode),
            ToolApprovalStatus.Consumed =>
                value.DecidedAtUtc is not null
                && value.ClaimedAtUtc is not null
                && value.FinishedAtUtc is not null
                && string.IsNullOrEmpty(value.ErrorCode),
            ToolApprovalStatus.Failed =>
                value.DecidedAtUtc is not null
                && value.ClaimedAtUtc is not null
                && value.FinishedAtUtc is not null
                && !string.IsNullOrWhiteSpace(value.ErrorCode),
            ToolApprovalStatus.Invalidated =>
                value.DecidedAtUtc is not null
                && value.ClaimedAtUtc is null
                && value.FinishedAtUtc is not null
                && !string.IsNullOrWhiteSpace(value.ErrorCode),
            _ => false
        };

        if (!valid
            || value.DecidedAtUtc < value.RequestedAtUtc
            || value.ClaimedAtUtc < value.DecidedAtUtc
            || value.FinishedAtUtc < (value.ClaimedAtUtc ?? value.DecidedAtUtc)
            || value.DecisionReason.Length > MaximumDecisionReasonCharacters
            || value.DecisionUserId.Length > 256
            || value.ErrorCode.Length > 128)
        {
            throw InvalidState();
        }
    }

    private static string RequiredIdentity(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 256
            ? throw Invalid()
            : value.Trim();

    private static string NormalizeReason(string value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > MaximumDecisionReasonCharacters)
        {
            throw Invalid();
        }

        string protectedReason =
            UnifiedEntryPayloadProtector.ProtectInternal(normalized).Content;
        return protectedReason.Length <= MaximumDecisionReasonCharacters
            ? protectedReason
            : throw Invalid();
    }

    private static string NormalizeErrorCode(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 128
            ? ToolApprovalErrorCodes.ExecutionFailed
            : value.Trim();

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static long NextRevision(long value) =>
        value == long.MaxValue
            ? throw InvalidState()
            : value + 1;

    private static ToolApprovalException Invalid() =>
        new(ToolApprovalErrorCodes.Invalid, "The tool approval is invalid.");

    private static ToolApprovalException InvalidState() =>
        new(
            ToolApprovalErrorCodes.InvalidState,
            "The tool approval state transition is invalid.");
}

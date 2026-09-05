#nullable enable

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Approvals;

/// <summary>
/// 工具调用审批请求的生命周期状态。
/// </summary>
public enum ToolApprovalStatus
{
    /// <summary>等待审批。</summary>
    Pending,
    /// <summary>审批已通过。</summary>
    Approved,
    /// <summary>审批已拒绝。</summary>
    Rejected,
    /// <summary>审批请求已取消。</summary>
    Cancelled,
    /// <summary>审批请求已过期。</summary>
    Expired,
    /// <summary>审批结果正在被执行流程占用。</summary>
    Consuming,
    /// <summary>审批结果已被执行流程消费。</summary>
    Consumed,
    /// <summary>获批后的工具执行失败。</summary>
    Failed,
    /// <summary>审批结果因上下文变化而失效。</summary>
    Invalidated
}

/// <summary>
/// 定义工具调用审批领域错误码。
/// </summary>
public static class ToolApprovalErrorCodes
{
    /// <summary>表示 <c>Invalid</c> 场景的错误码。</summary>
    public const string Invalid = "TOOL_APPROVAL_INVALID";
    /// <summary>表示 <c>InvalidState</c> 场景的错误码。</summary>
    public const string InvalidState = "TOOL_APPROVAL_INVALID_STATE";
    /// <summary>表示 <c>Expired</c> 场景的错误码。</summary>
    public const string Expired = "TOOL_APPROVAL_EXPIRED";
    /// <summary>表示 <c>SelfApprovalForbidden</c> 场景的错误码。</summary>
    public const string SelfApprovalForbidden =
        "TOOL_APPROVAL_SELF_APPROVAL_FORBIDDEN";
    /// <summary>表示 <c>CancellationForbidden</c> 场景的错误码。</summary>
    public const string CancellationForbidden =
        "TOOL_APPROVAL_CANCELLATION_FORBIDDEN";
    /// <summary>表示 <c>ExecutionFailed</c> 场景的错误码。</summary>
    public const string ExecutionFailed = "TOOL_APPROVAL_EXECUTION_FAILED";
    /// <summary>表示 <c>ExecutionOutcomeUnknown</c> 场景的错误码。</summary>
    public const string ExecutionOutcomeUnknown =
        "TOOL_APPROVAL_EXECUTION_OUTCOME_UNKNOWN";
    /// <summary>表示 <c>Rejected</c> 场景的错误码。</summary>
    public const string Rejected = "TOOL_APPROVAL_REJECTED";
    /// <summary>表示 <c>Cancelled</c> 场景的错误码。</summary>
    public const string Cancelled = "TOOL_APPROVAL_CANCELLED";
    /// <summary>表示 <c>PayloadInvalid</c> 场景的错误码。</summary>
    public const string PayloadInvalid = "TOOL_APPROVAL_PAYLOAD_INVALID";
    /// <summary>表示 <c>RevalidationFailed</c> 场景的错误码。</summary>
    public const string RevalidationFailed =
        "TOOL_APPROVAL_REVALIDATION_FAILED";
}

/// <summary>
/// 表示工具调用审批流程中的领域异常。
/// </summary>
public sealed class ToolApprovalException(string errorCode, string message)
    : Exception(message)
{
    /// <summary>
    /// 获取领域异常对应的错误码。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// 工具调用审批请求记录。
/// </summary>
/// <param name="Id">记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="RequesterUserId">发起审批的用户标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="AgentRunId">Agent 运行标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="McpServerId">MCP 服务标识。</param>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="ToolName">工具名称。</param>
/// <param name="Risk">工具风险等级。</param>
/// <param name="ToolSchemaSha256">工具输入架构的 SHA-256 摘要。</param>
/// <param name="ArgumentsSha256">工具参数的 SHA-256 摘要。</param>
/// <param name="SafeArgumentsSummaryJson">已脱敏的工具参数摘要 JSON。</param>
/// <param name="Status">当前状态。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="RequestedAtUtc">审批发起的 UTC 时间。</param>
/// <param name="ExpiresAtUtc">记录或审批过期的 UTC 时间。</param>
/// <param name="DecisionUserId">作出审批决策的用户标识。</param>
/// <param name="DecisionReason">审批决策原因。</param>
/// <param name="DecidedAtUtc">作出审批决策的 UTC 时间。</param>
/// <param name="ClaimedAtUtc">执行流程取得审批请求的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
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

/// <summary>
/// 审批执行流程取得的执行声明。
/// </summary>
/// <param name="Request">审批请求记录。</param>
/// <param name="ProtectedResumePayload">受保护的恢复执行载荷。</param>
/// <param name="ProtectedResumePayloadSha256">受保护恢复载荷的 SHA-256 摘要。</param>
public sealed record ToolApprovalExecutionClaim(
    ToolApprovalRequestRecord Request,
    string ProtectedResumePayload,
    string ProtectedResumePayloadSha256);

/// <summary>
/// 获批工具调用的执行结果记录。
/// </summary>
/// <param name="ApprovalId">工具调用审批标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="Succeeded">执行是否成功。</param>
/// <param name="Blocked">执行是否被策略阻止。</param>
/// <param name="ProtectedContent">受保护的工具执行结果。</param>
/// <param name="ProtectedContentSha256">受保护结果的 SHA-256 摘要。</param>
/// <param name="ContentSha256">结果明文的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
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

/// <summary>
/// 工具调用审批的决策记录。
/// </summary>
/// <param name="Id">记录标识。</param>
/// <param name="ApprovalId">工具调用审批标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="FromStatus">决策前的审批状态。</param>
/// <param name="ToStatus">决策后的审批状态。</param>
/// <param name="DecisionUserId">作出审批决策的用户标识。</param>
/// <param name="DecisionReason">审批决策原因。</param>
/// <param name="DecidedAtUtc">作出审批决策的 UTC 时间。</param>
/// <param name="ResultingLogicalRevision">决策完成后的逻辑版本。</param>
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

/// <summary>
/// 审批载荷加解密使用的上下文。
/// </summary>
/// <param name="ApprovalId">工具调用审批标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="ArgumentsSha256">工具参数的 SHA-256 摘要。</param>
public sealed record ToolApprovalPayloadContext(
    Guid ApprovalId,
    string TenantId,
    string ArgumentsSha256);

/// <summary>
/// 定义工具审批恢复载荷的保护与解保护能力。
/// </summary>
public interface IToolApprovalPayloadProtector
{
    /// <summary>保护工具审批恢复载荷。</summary>
    string Protect(ToolApprovalPayloadContext context, string plaintext);

    /// <summary>解保护并校验工具审批恢复载荷。</summary>
    string Unprotect(ToolApprovalPayloadContext context, string protectedPayload);
}

/// <summary>
/// 工具调用审批的查询条件。
/// </summary>
/// <param name="TenantId">租户标识。</param>
/// <param name="Status">当前状态。</param>
/// <param name="Take">最多返回的记录数量。</param>
public sealed record ToolApprovalQuery(
    string TenantId,
    ToolApprovalStatus? Status = null,
    int Take = 100);

/// <summary>
/// 对工具调用审批请求执行的决策动作。
/// </summary>
public enum ToolApprovalDecisionAction
{
    /// <summary>批准工具调用。</summary>
    Approve,
    /// <summary>拒绝工具调用。</summary>
    Reject,
    /// <summary>取消审批请求。</summary>
    Cancel
}

/// <summary>
/// 提交工具调用审批决策的命令。
/// </summary>
/// <param name="ApprovalId">工具调用审批标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="ActorUserId">执行审批动作的用户标识。</param>
/// <param name="Action">审批动作。</param>
/// <param name="Reason">执行该动作的原因。</param>
/// <param name="DecidedAtUtc">作出审批决策的 UTC 时间。</param>
public sealed record ToolApprovalDecisionCommand(
    Guid ApprovalId,
    string TenantId,
    string ActorUserId,
    ToolApprovalDecisionAction Action,
    string Reason,
    DateTimeOffset DecidedAtUtc);

/// <summary>
/// 定义工具调用审批记录的存储和原子状态转换边界。
/// </summary>
public interface IToolApprovalRepository
{
    /// <summary>尝试创建工具调用审批记录。</summary>
    Task<bool> TryCreateAsync(
        ToolApprovalRequestRecord request,
        string protectedResumePayload,
        CancellationToken cancellationToken = default);

    /// <summary>获取工具调用审批记录。</summary>
    Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>查询工具调用审批记录列表。</summary>
    Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        ToolApprovalQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>查询工具审批的决策历史。</summary>
    Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid approvalId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>按并发条件尝试替换工具调用审批记录。</summary>
    Task<bool> TryReplaceAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);

    /// <summary>尝试取得工具审批的执行权。</summary>
    Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>尝试提交获批工具调用的执行结果。</summary>
    Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default);

    /// <summary>获取获批工具调用的执行结果。</summary>
    Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>恢复或终结中断的审批执行。</summary>
    Task<int> RecoverInterruptedExecutionsAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

/// <summary>
/// 集中实现工具调用审批的状态转换规则。
/// </summary>
public static partial class ToolApprovalStateMachine
{
    /// <summary>审批参数安全摘要允许的最大 UTF-8 字节数。</summary>
    public const int MaximumSafeSummaryUtf8Bytes = 8_192;
    /// <summary>受保护审批载荷允许的最大 UTF-8 字节数。</summary>
    public const int MaximumProtectedPayloadUtf8Bytes = 65_536;
    /// <summary>工具结果明文允许的最大 UTF-8 字节数。</summary>
    public const int MaximumResultPlaintextUtf8Bytes = 1_048_576;
    /// <summary>受保护工具结果允许的最大 UTF-8 字节数。</summary>
    public const int MaximumProtectedResultUtf8Bytes = 1_500_000;
    /// <summary>审批决策原因允许的最大字符数。</summary>
    public const int MaximumDecisionReasonCharacters = 512;
    /// <summary>单次查询允许返回的最大记录数量。</summary>
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

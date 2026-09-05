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
/// <param name="errorCode">用于标识失败原因的领域错误码。</param>
/// <param name="message">描述异常原因的错误消息。</param>
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
    #region 加密并绑定工具审批恢复载荷（Protect）
    /// <summary>
    /// 加密并绑定工具审批恢复载荷（Protect）。
    /// </summary>
    /// <param name="context">用于绑定密文的审批身份和执行上下文。</param>
    /// <param name="plaintext">需要加密保护的明文。</param>
    /// <returns>与给定审批上下文绑定的加密载荷字符串。</returns>
    string Protect(ToolApprovalPayloadContext context, string plaintext);
    #endregion

    #region 解密并验证工具审批恢复载荷（Unprotect）
    /// <summary>
    /// 解密并验证工具审批恢复载荷（Unprotect）。
    /// </summary>
    /// <param name="context">必须与加密时一致的审批身份和执行上下文。</param>
    /// <param name="protectedPayload">已加密保护的载荷。</param>
    /// <returns>通过上下文绑定和完整性校验后恢复的明文。</returns>
    string Unprotect(ToolApprovalPayloadContext context, string protectedPayload);
    #endregion
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
    #region 校验并创建工具审批请求及恢复载荷（TryCreateAsync）
    /// <summary>
    /// 校验并创建工具审批请求及恢复载荷（TryCreateAsync）。
    /// </summary>
    /// <param name="request">待创建的审批请求，初始状态必须为 Pending，逻辑修订号必须为零。</param>
    /// <param name="protectedResumePayload">与审批请求一起保存的非空加密恢复载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批请求及恢复载荷保存成功时返回 true；审批标识已存在时返回 false。</returns>
    /// <exception cref="ToolApprovalException">审批请求字段、初始状态或恢复载荷不符合状态机约束。</exception>
    Task<bool> TryCreateAsync(ToolApprovalRequestRecord request, string protectedResumePayload, CancellationToken cancellationToken = default);
    #endregion

    #region 按租户和审批标识查询请求（GetAsync）
    /// <summary>
    /// 按租户和审批标识查询请求（GetAsync）。
    /// </summary>
    /// <param name="id">工具审批请求标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回指定租户下未删除的审批请求；不存在时返回 null。</returns>
    Task<ToolApprovalRequestRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 按租户和可选状态查询审批请求（ListAsync）
    /// <summary>
    /// 按租户和可选状态查询审批请求（ListAsync）。
    /// </summary>
    /// <param name="query">租户、可选审批状态和返回数量限制，Take 须为 1 至 MaximumTake。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回符合条件、按请求时间倒序排列且不超过 Take 条的审批记录；没有匹配项时返回空集合。</returns>
    Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(ToolApprovalQuery query, CancellationToken cancellationToken = default);
    #endregion

    #region 查询指定审批的人工决策历史（ListDecisionsAsync）
    /// <summary>
    /// 查询指定审批的人工决策历史（ListDecisionsAsync）。
    /// </summary>
    /// <param name="approvalId">工具审批请求标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回指定租户和审批下的未删除决策记录，按决策产生的逻辑修订号升序排列；无记录时返回空集合。</returns>
    Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(Guid approvalId, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 按修订号更新审批状态并记录人工决策（TryReplaceAsync）
    /// <summary>
    /// 按修订号更新审批状态并记录人工决策（TryReplaceAsync）。
    /// </summary>
    /// <param name="replacement">保留原审批绑定信息且符合状态机规则的新记录，逻辑修订号须递增一。</param>
    /// <param name="expectedLogicalRevision">现有审批记录应具有的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批状态及必要的人工决策记录提交成功时返回 true；记录不存在、预期修订号不匹配或并发条件更新未生效时返回 false。</returns>
    /// <exception cref="ToolApprovalException">新记录破坏审批绑定信息，或修订号、状态转换及状态字段不符合状态机规则。</exception>
    Task<bool> TryReplaceAsync(ToolApprovalRequestRecord replacement, long expectedLogicalRevision, CancellationToken cancellationToken = default);
    #endregion

    #region 按修订号认领尚未过期的已批准请求（TryClaimExecutionAsync）
    /// <summary>
    /// 按修订号认领尚未过期的已批准请求（TryClaimExecutionAsync）。
    /// </summary>
    /// <param name="id">工具审批请求标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="expectedLogicalRevision">认领前审批记录应具有的逻辑修订号。</param>
    /// <param name="claimedAtUtc">执行认领时间，须不早于请求时间且早于过期时间。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功转为 Consuming 时返回更新后的审批记录及加密恢复载荷；条件不匹配、记录或载荷不存在时返回 null。</returns>
    Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default);
    #endregion

    #region 原子提交审批终态及工具执行结果（TryCompleteExecutionAsync）
    /// <summary>
    /// 原子提交审批终态及工具执行结果（TryCompleteExecutionAsync）。
    /// </summary>
    /// <param name="replacement">符合状态机规则的审批完成记录，绑定字段须保持不变且修订号递增一。</param>
    /// <param name="expectedLogicalRevision">完成执行前审批记录应具有的逻辑修订号。</param>
    /// <param name="result">与审批标识、租户、完成时间、成功状态及错误码一致的受保护执行结果。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批状态与执行结果提交成功时返回 true；结果封装无效、记录不存在、修订号不匹配、结果与审批终态不一致或并发更新未生效时返回 false。</returns>
    /// <exception cref="ToolApprovalException">审批替换记录的绑定信息、修订号或状态转换不符合状态机规则。</exception>
    Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default);
    #endregion

    #region 按租户和审批标识读取执行结果（GetExecutionResultAsync）
    /// <summary>
    /// 按租户和审批标识读取执行结果（GetExecutionResultAsync）。
    /// </summary>
    /// <param name="id">工具审批请求标识，而非执行结果记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回通过封装校验的受保护执行结果；不存在时返回 null；已保存结果损坏时通过异常报告。</returns>
    Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 终结结果未知的中断审批执行（RecoverInterruptedExecutionsAsync）
    /// <summary>
    /// 终结结果未知的中断审批执行（RecoverInterruptedExecutionsAsync）。
    /// </summary>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回已处理的中断执行数量；接口默认实现不执行恢复并返回零。</returns>
    Task<int> RecoverInterruptedExecutionsAsync(DateTimeOffset recoveredAtUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    #endregion
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

    #region 获取 SHA-256 摘要格式匹配器（Sha256Pattern）
    /// <summary>
    /// 获取 SHA-256 摘要格式匹配器（Sha256Pattern）。
    /// </summary>
    /// <returns>匹配 64 位小写十六进制摘要的正则表达式。</returns>
    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
    #endregion

    #region 校验新审批请求及加密恢复载荷（ValidateNew）
    /// <summary>
    /// 校验新审批请求及加密恢复载荷（ValidateNew）。
    /// </summary>
    /// <param name="value">待创建的审批请求，须为 Pending 状态、零修订号且无决策或执行信息。</param>
    /// <param name="protectedResumePayload">待校验前缀及大小的加密恢复载荷。</param>
    /// <exception cref="ToolApprovalException">审批初始字段、载荷、安全摘要或状态字段不合法。</exception>
    public static void ValidateNew(ToolApprovalRequestRecord value, string protectedResumePayload)
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
    #endregion

    #region 生成批准后的审批记录（Approve）
    /// <summary>
    /// 生成批准后的审批记录（Approve）。
    /// </summary>
    /// <param name="value">尚未过期且处于 Pending 状态的审批请求。</param>
    /// <param name="decisionUserId">作出审批决策的用户标识。</param>
    /// <param name="reason">将进行首尾空白清理和敏感内容保护的审批原因。</param>
    /// <param name="decidedAtUtc">作出审批决策的 UTC 时间。</param>
    /// <returns>返回状态为 Approved、修订号递增并记录决策人、原因及时间的新记录；不持久化。</returns>
    /// <exception cref="ToolApprovalException">请求不是待审批状态、已过期、高风险请求由申请人自行批准，或操作者、原因及修订号不合法。</exception>
    public static ToolApprovalRequestRecord Approve(ToolApprovalRequestRecord value, string decisionUserId, string reason, DateTimeOffset decidedAtUtc)
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
    #endregion

    #region 生成拒绝后的审批记录（Reject）
    /// <summary>
    /// 生成拒绝后的审批记录（Reject）。
    /// </summary>
    /// <param name="value">尚未过期且处于 Pending 状态的审批请求。</param>
    /// <param name="decisionUserId">作出审批决策的用户标识。</param>
    /// <param name="reason">将进行首尾空白清理和敏感内容保护的拒绝原因。</param>
    /// <param name="decidedAtUtc">作出审批决策的 UTC 时间。</param>
    /// <returns>返回状态为 Rejected、修订号递增且决策和完成时间均已设置的新记录；不持久化。</returns>
    /// <exception cref="ToolApprovalException">请求不是待审批状态、已过期，或操作者、原因及修订号不合法。</exception>
    public static ToolApprovalRequestRecord Reject(ToolApprovalRequestRecord value, string decisionUserId, string reason, DateTimeOffset decidedAtUtc)
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
    #endregion

    #region 由申请人生成撤销后的审批记录（Cancel）
    /// <summary>
    /// 由申请人生成撤销后的审批记录（Cancel）。
    /// </summary>
    /// <param name="value">尚未过期且处于 Pending 状态的审批请求。</param>
    /// <param name="requesterUserId">必须与原申请人一致的撤销操作者标识。</param>
    /// <param name="reason">将进行首尾空白清理和敏感内容保护的撤销原因。</param>
    /// <param name="cancelledAtUtc">取消时间（UTC）。</param>
    /// <returns>返回状态为 Cancelled、修订号递增且决策和完成时间均已设置的新记录；不持久化。</returns>
    /// <exception cref="ToolApprovalException">请求不是待审批状态、已过期、操作者不是申请人，或原因及修订号不合法。</exception>
    public static ToolApprovalRequestRecord Cancel(ToolApprovalRequestRecord value, string requesterUserId, string reason, DateTimeOffset cancelledAtUtc)
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
    #endregion

    #region 生成已过期的审批记录（Expire）
    /// <summary>
    /// 生成已过期的审批记录（Expire）。
    /// </summary>
    /// <param name="value">处于 Pending 或 Approved 状态的审批请求。</param>
    /// <param name="expiredAtUtc">本次确认过期的 UTC 时间，不得早于请求的 ExpiresAtUtc。</param>
    /// <returns>返回状态为 Expired、修订号递增并设置过期错误码及完成时间的新记录；不持久化。</returns>
    /// <exception cref="ToolApprovalException">请求既非 Pending 也非 Approved、尚未过期，或修订号无法递增。</exception>
    public static ToolApprovalRequestRecord Expire(ToolApprovalRequestRecord value, DateTimeOffset expiredAtUtc)
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
    #endregion

    #region 生成已认领执行权的审批记录（Claim）
    /// <summary>
    /// 生成已认领执行权的审批记录（Claim）。
    /// </summary>
    /// <param name="value">处于 Approved 状态且认领时尚未过期的审批请求。</param>
    /// <param name="claimedAtUtc">认领时间（UTC）。</param>
    /// <returns>返回状态为 Consuming、修订号递增且已设置认领时间的新记录；不执行工具调用或持久化。</returns>
    /// <exception cref="ToolApprovalException">请求未批准、已过期，或修订号无法递增。</exception>
    public static ToolApprovalRequestRecord Claim(ToolApprovalRequestRecord value, DateTimeOffset claimedAtUtc)
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
    #endregion

    #region 生成重新校验失败后的审批失效记录（Invalidate）
    /// <summary>
    /// 生成重新校验失败后的审批失效记录（Invalidate）。
    /// </summary>
    /// <param name="value">处于 Approved 状态且具有决策时间的审批请求。</param>
    /// <param name="errorCode">失效原因错误码；空值使用 RevalidationFailed，非空值按错误码规则规范化。</param>
    /// <param name="invalidatedAtUtc">确认失效的 UTC 时间，不得早于审批决策时间。</param>
    /// <returns>返回状态为 Invalidated、修订号递增并设置完成时间及错误码的新记录；不持久化。</returns>
    /// <exception cref="ToolApprovalException">请求未批准、缺少决策时间、失效时间早于决策时间，或修订号无法递增。</exception>
    public static ToolApprovalRequestRecord Invalidate(ToolApprovalRequestRecord value, string errorCode, DateTimeOffset invalidatedAtUtc)
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
    #endregion

    #region 生成工具执行结束后的审批记录（Complete）
    /// <summary>
    /// 生成工具执行结束后的审批记录（Complete）。
    /// </summary>
    /// <param name="value">处于 Consuming 状态且具有认领时间的审批请求。</param>
    /// <param name="succeeded">工具执行是否成功，决定使用 Consumed 还是 Failed 状态。</param>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="finishedAtUtc">工具执行完成的 UTC 时间，不得早于认领时间。</param>
    /// <returns>返回修订号递增并设置完成时间的新记录；成功状态为 Consumed 且错误码为空，失败状态为 Failed 并保存规范化错误码；不持久化。</returns>
    /// <exception cref="ToolApprovalException">请求不处于 Consuming 状态、缺少认领时间、完成时间早于认领时间，或修订号无法递增。</exception>
    public static ToolApprovalRequestRecord Complete(ToolApprovalRequestRecord value, bool succeeded, string errorCode, DateTimeOffset finishedAtUtc)
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
    #endregion

    #region 将结果未知的审批执行生成为失败记录（RecoverUnknownOutcome）
    /// <summary>
    /// 将结果未知的审批执行生成为失败记录（RecoverUnknownOutcome）。
    /// </summary>
    /// <param name="value">处于 Consuming 状态且具有认领时间的中断执行记录。</param>
    /// <param name="recoveredAtUtc">恢复处理的 UTC 时间，不得早于原认领时间。</param>
    /// <returns>返回状态为 Failed、错误码为 ExecutionOutcomeUnknown 的新记录；修订号递增，不重新执行工具或持久化。</returns>
    /// <exception cref="ToolApprovalException">记录状态或恢复时间不满足执行完成条件，或修订号无法递增。</exception>
    public static ToolApprovalRequestRecord RecoverUnknownOutcome(ToolApprovalRequestRecord value, DateTimeOffset recoveredAtUtc) =>
        Complete(
            value,
            succeeded: false,
            ToolApprovalErrorCodes.ExecutionOutcomeUnknown,
            recoveredAtUtc);
    #endregion

    #region 校验审批替换记录的绑定和状态转换（ValidateReplacement）
    /// <summary>
    /// 校验审批替换记录的绑定和状态转换（ValidateReplacement）。
    /// </summary>
    /// <param name="existing">更新前的审批请求。</param>
    /// <param name="replacement">待提交的新记录，绑定字段必须不变、修订号递增一且状态转换合法。</param>
    /// <exception cref="ToolApprovalException">绑定字段改变、修订号不合法、状态转换不允许，或新记录字段不符合状态约束。</exception>
    public static void ValidateReplacement(ToolApprovalRequestRecord existing, ToolApprovalRequestRecord replacement)
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
    #endregion

    #region 校验审批查询的租户和分页限制（ValidateQuery）
    /// <summary>
    /// 校验审批查询的租户和分页限制（ValidateQuery）。
    /// </summary>
    /// <param name="query">必须提供租户、合法可选状态及 1 至 MaximumTake 条返回数量的查询条件。</param>
    /// <exception cref="ToolApprovalException">租户为空、返回数量超出限制或可选状态无效。</exception>
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
    #endregion

    #region 校验加密审批载荷的格式及大小（ValidateProtectedPayload）
    /// <summary>
    /// 校验加密审批载荷的格式及大小（ValidateProtectedPayload）。
    /// </summary>
    /// <param name="value">以 enc:v1: 开头且 UTF-8 大小不超过 MaximumProtectedPayloadUtf8Bytes 的非空载荷；本方法不解密。</param>
    /// <exception cref="ToolApprovalException">载荷为空、缺少 enc:v1: 前缀或超出大小限制。</exception>
    public static void ValidateProtectedPayload(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith("enc:v1:", StringComparison.Ordinal)
            || Encoding.UTF8.GetByteCount(value) > MaximumProtectedPayloadUtf8Bytes)
        {
            throw Invalid();
        }
    }
    #endregion

    #region 校验工具执行结果的封装及密文摘要（ValidateExecutionResultEnvelope）
    /// <summary>
    /// 校验工具执行结果的封装及密文摘要（ValidateExecutionResultEnvelope）。
    /// </summary>
    /// <param name="value">待校验身份、密文前缀与大小、摘要、结果标记及完成时间的执行结果；不解密校验明文摘要。</param>
    /// <exception cref="ToolApprovalException">结果身份、密文、摘要或状态字段不符合封装约束。</exception>
    public static void ValidateExecutionResultEnvelope(ToolApprovalExecutionResultRecord value)
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
    #endregion

    #region 检查前置条件（EnsurePendingAndLive）
    /// <summary>
    /// 检查前置条件（EnsurePendingAndLive）
    /// </summary>
    /// <param name="value">本次操作使用的工具审批请求记录。</param>
    /// <param name="occurredAtUtc">事件发生时间（UTC）。</param>
    private static void EnsurePendingAndLive(ToolApprovalRequestRecord value, DateTimeOffset occurredAtUtc)
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
    #endregion

    #region 核对审批绑定信息是否保持不变（PreservesBinding）
    /// <summary>
    /// 核对审批绑定信息是否保持不变（PreservesBinding）。
    /// </summary>
    /// <param name="existing">原有审批记录。</param>
    /// <param name="replacement">待验证的替换审批记录。</param>
    /// <returns>审批标识、身份、运行及工具绑定、参数摘要、安全摘要和请求有效期均未改变时返回 true，否则返回 false；不比较可变状态字段。</returns>
    private static bool PreservesBinding(ToolApprovalRequestRecord existing, ToolApprovalRequestRecord replacement) =>
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
    #endregion

    #region 判断是否允许审批状态转换（AllowedTransition）
    /// <summary>
    /// 判断是否允许审批状态转换（AllowedTransition）。
    /// </summary>
    /// <param name="from">转换前的审批状态。</param>
    /// <param name="to">拟转换到的审批状态。</param>
    /// <returns>状态转换命中允许规则时返回 true；相同状态或其他未列出的转换返回 false。</returns>
    /// <remarks>Pending 可转为 Approved、Rejected、Cancelled 或 Expired；Approved 可转为 Consuming、Expired 或 Invalidated；Consuming 可转为 Consumed 或 Failed。</remarks>
    private static bool AllowedTransition(ToolApprovalStatus from, ToolApprovalStatus to) =>
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
    #endregion

    #region 校验审批参数安全摘要（ValidateSafeSummary）
    /// <summary>
    /// 校验审批参数安全摘要（ValidateSafeSummary）。
    /// </summary>
    /// <param name="value">非空 JSON 对象文本，UTF-8 大小不超过 MaximumSafeSummaryUtf8Bytes，且不能再被内部载荷保护规则改写。</param>
    /// <exception cref="ToolApprovalException">摘要不是受支持大小的 JSON 对象，或包含需要进一步保护的内容。</exception>
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
    #endregion

    #region 校验审批状态字段及时间顺序（ValidateStateShape）
    /// <summary>
    /// 校验审批状态字段及时间顺序（ValidateStateShape）。
    /// </summary>
    /// <param name="value">待检查各状态必填或禁填字段、时间先后关系及文本长度的审批记录。</param>
    /// <exception cref="ToolApprovalException">状态字段、时间顺序或文本长度不符合约束。</exception>
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
    #endregion

    #region 校验并规范化审批操作者标识（RequiredIdentity）
    /// <summary>
    /// 校验并规范化审批操作者标识（RequiredIdentity）。
    /// </summary>
    /// <param name="value">待校验的原始操作者标识。</param>
    /// <returns>返回移除首尾空白的操作者标识；原始值为空或超过 256 字符时抛出审批参数异常。</returns>
    private static string RequiredIdentity(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 256
            ? throw Invalid()
            : value.Trim();
    #endregion

    #region 规范化并保护审批原因（NormalizeReason）
    /// <summary>
    /// 规范化并保护审批原因（NormalizeReason）。
    /// </summary>
    /// <param name="value">待规范化的审批原因。</param>
    /// <returns>返回去除首尾空白并经内部载荷保护处理的原因；null 按空字符串处理，处理前后任一长度超过限制时抛异常。</returns>
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
    #endregion

    #region 规范化工具审批错误码（NormalizeErrorCode）
    /// <summary>
    /// 规范化工具审批错误码（NormalizeErrorCode）。
    /// </summary>
    /// <param name="value">待规范化的错误码。</param>
    /// <returns>原始值为空白或超过 128 字符时返回 ExecutionFailed，否则返回去除首尾空白的错误码。</returns>
    private static string NormalizeErrorCode(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 128
            ? ToolApprovalErrorCodes.ExecutionFailed
            : value.Trim();
    #endregion

    #region 计算文本的 SHA-256 摘要（Sha256）
    /// <summary>
    /// 计算文本的 SHA-256 摘要（Sha256）。
    /// </summary>
    /// <param name="value">待计算摘要的文本。</param>
    /// <returns>返回输入文本按 UTF-8 编码计算得到的 64 位小写十六进制 SHA-256 摘要。</returns>
    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    #endregion

    #region 计算下一审批逻辑修订号（NextRevision）
    /// <summary>
    /// 计算下一审批逻辑修订号（NextRevision）。
    /// </summary>
    /// <param name="value">当前审批逻辑修订号。</param>
    /// <returns>返回当前修订号加一；当前值为 long.MaxValue 时抛出状态异常。</returns>
    private static long NextRevision(long value) =>
        value == long.MaxValue
            ? throw InvalidState()
            : value + 1;
    #endregion

    #region 创建审批参数无效异常（Invalid）
    /// <summary>
    /// 创建审批参数无效异常（Invalid）。
    /// </summary>
    /// <returns>返回错误码为 Invalid 的审批异常对象；本方法仅创建异常，不抛出。</returns>
    private static ToolApprovalException Invalid() =>
        new(ToolApprovalErrorCodes.Invalid, "The tool approval is invalid.");
    #endregion

    #region 创建审批状态转换无效异常（InvalidState）
    /// <summary>
    /// 创建审批状态转换无效异常（InvalidState）。
    /// </summary>
    /// <returns>返回错误码为 InvalidState 的审批异常对象；本方法仅创建异常，不抛出。</returns>
    private static ToolApprovalException InvalidState() =>
        new(
            ToolApprovalErrorCodes.InvalidState,
            "The tool approval state transition is invalid.");
    #endregion
}

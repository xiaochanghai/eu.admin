#nullable enable

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Approvals;

/// <summary>
/// 运行时发起工具调用审批的请求。
/// </summary>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="AgentRunId">Agent 运行标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="Tool">申请调用的已发布 MCP 工具。</param>
/// <param name="ArgumentsJson">工具调用参数 JSON。</param>
/// <param name="Requester">发起调用的执行身份。</param>
/// <param name="RequestedAtUtc">审批发起的 UTC 时间。</param>
/// <param name="ExpiresAtUtc">记录或审批过期的 UTC 时间。</param>
public sealed record ToolApprovalRuntimeRequest(
    Guid ConversationId,
    Guid EntryRunId,
    Guid AgentRunId,
    Guid AgentVersionId,
    PublishedMcpToolReference Tool,
    string ArgumentsJson,
    AgentExecutionIdentity Requester,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExpiresAtUtc);

/// <summary>
/// 恢复获批工具调用的请求。
/// </summary>
/// <param name="ApprovalId">工具调用审批标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Requester">发起调用的执行身份。</param>
/// <param name="ResumedAtUtc">恢复执行的 UTC 时间。</param>
public sealed record ToolApprovalResumeRequest(
    Guid ApprovalId,
    long ExpectedLogicalRevision,
    AgentExecutionIdentity Requester,
    DateTimeOffset ResumedAtUtc);

/// <summary>
/// 工具调用审批策略的判定结果。
/// </summary>
/// <param name="Allowed">策略是否允许继续执行。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record ToolApprovalPolicyResult(bool Allowed, string ErrorCode)
{
    #region 处理（Allow）
    /// <summary>
    /// 处理（Allow）
    /// </summary>
    /// <returns>允许继续执行且无错误码的审批策略结果。</returns>
    public static ToolApprovalPolicyResult Allow() => new(true, string.Empty);
    #endregion

    #region 处理（Deny）
    /// <summary>
    /// 处理（Deny）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <returns>拒绝执行的策略结果；未提供错误码时使用 ToolBlocked。</returns>
    public static ToolApprovalPolicyResult Deny(string errorCode) =>
        new(false, string.IsNullOrWhiteSpace(errorCode)
            ? AgentRunErrorCodes.ToolBlocked
            : errorCode);
    #endregion
}

/// <summary>
/// 定义获批工具恢复执行前的安全策略。
/// </summary>
public interface IToolApprovalExecutionPolicy
{
    #region 重新校验获批工具是否仍允许执行。
    /// <summary>重新校验获批工具是否仍允许执行。</summary>
    /// <param name="approval">审批记录。</param>
    /// <param name="currentTool">当前工具版本。</param>
    /// <param name="requester">请求发起方。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批在当前工具配置及请求身份下是否仍允许执行，以及拒绝时的错误码。</returns>
    Task<ToolApprovalPolicyResult> RevalidateAsync(
        ToolApprovalRequestRecord approval,
        PublishedMcpToolReference currentTool,
        AgentExecutionIdentity requester,
        CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义已获批 MCP 工具的运行时调用能力。
/// </summary>
public interface IApprovedMcpRuntimeToolInvoker
{
    #region 调用已经审批通过的 MCP 工具。
    /// <summary>调用已经审批通过的 MCP 工具。</summary>
    /// <param name="claim">已取得执行权的工具审批声明。</param>
    /// <param name="tool">工具定义。</param>
    /// <param name="arguments">调用参数。</param>
    /// <param name="invocationContext">MCP 调用所用的执行身份和运行上下文。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>使用已认领审批授权调用工具后的成功、阻止状态、内容及错误码。</returns>
    Task<McpRuntimeToolResult> InvokeApprovedAsync(
        ToolApprovalExecutionClaim claim,
        PublishedMcpToolReference tool,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext invocationContext,
        CancellationToken cancellationToken = default);
    #endregion
}

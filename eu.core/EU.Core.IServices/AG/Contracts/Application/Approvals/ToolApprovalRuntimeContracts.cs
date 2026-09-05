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
    public static ToolApprovalPolicyResult Allow() => new(true, string.Empty);

    public static ToolApprovalPolicyResult Deny(string errorCode) =>
        new(false, string.IsNullOrWhiteSpace(errorCode)
            ? AgentRunErrorCodes.ToolBlocked
            : errorCode);
}

/// <summary>
/// 定义获批工具恢复执行前的安全策略。
/// </summary>
public interface IToolApprovalExecutionPolicy
{
    /// <summary>重新校验获批工具是否仍允许执行。</summary>
    Task<ToolApprovalPolicyResult> RevalidateAsync(
        ToolApprovalRequestRecord approval,
        PublishedMcpToolReference currentTool,
        AgentExecutionIdentity requester,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义已获批 MCP 工具的运行时调用能力。
/// </summary>
public interface IApprovedMcpRuntimeToolInvoker
{
    /// <summary>调用已经审批通过的 MCP 工具。</summary>
    Task<McpRuntimeToolResult> InvokeApprovedAsync(
        ToolApprovalExecutionClaim claim,
        PublishedMcpToolReference tool,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext invocationContext,
        CancellationToken cancellationToken = default);
}

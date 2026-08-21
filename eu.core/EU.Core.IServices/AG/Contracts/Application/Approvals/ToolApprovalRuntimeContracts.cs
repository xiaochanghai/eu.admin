#nullable enable

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Approvals;

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

public sealed record ToolApprovalResumeRequest(
    Guid ApprovalId,
    long ExpectedLogicalRevision,
    AgentExecutionIdentity Requester,
    DateTimeOffset ResumedAtUtc);

public sealed record ToolApprovalPolicyResult(bool Allowed, string ErrorCode)
{
    public static ToolApprovalPolicyResult Allow() => new(true, string.Empty);

    public static ToolApprovalPolicyResult Deny(string errorCode) =>
        new(false, string.IsNullOrWhiteSpace(errorCode)
            ? AgentRunErrorCodes.ToolBlocked
            : errorCode);
}

public interface IToolApprovalExecutionPolicy
{
    Task<ToolApprovalPolicyResult> RevalidateAsync(
        ToolApprovalRequestRecord approval,
        PublishedMcpToolReference currentTool,
        AgentExecutionIdentity requester,
        CancellationToken cancellationToken = default);
}

public interface IApprovedMcpRuntimeToolInvoker
{
    Task<McpRuntimeToolResult> InvokeApprovedAsync(
        ToolApprovalExecutionClaim claim,
        PublishedMcpToolReference tool,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext invocationContext,
        CancellationToken cancellationToken = default);
}

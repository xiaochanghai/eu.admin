using System.Collections.ObjectModel;
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Application.Approvals;

namespace EU.Core.Agent.Application.Runtime;

public enum AgentRunStatus
{
    Running,
    WaitingForApproval,
    Completed,
    Failed,
    Cancelled
}

public enum AgentRunEventKind
{
    Started,
    SkillStarted,
    Delta,
    Citation,
    ToolStarted,
    ToolSucceeded,
    ToolBlocked,
    ToolFailed,
    ApprovalRequired,
    Completed,
    Failed,
    Cancelled
}

public enum AgentConversationRole
{
    User,
    Assistant
}

public sealed record AgentConversationMessage(
    AgentConversationRole Role,
    string Content);

public sealed record AgentRunEvent(
    Guid RunId,
    long Sequence,
    AgentRunEventKind Kind,
    DateTimeOffset OccurredAtUtc,
    string Text = "",
    Guid? ToolVersionId = null,
    string ToolName = "",
    string ErrorCode = "",
    Guid? ToolCallId = null,
    Guid? SkillVersionId = null,
    string SkillName = "")
{
    public string ArgumentsJson { get; init; } = "";

    public Guid? ApprovalId { get; init; }
}

public sealed class AgentRuntimeException(string errorCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed record AgentRunAuditRecord(
    Guid RunId,
    Guid AgentId,
    Guid AgentVersionId,
    string AgentCode,
    AgentRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string InputSha256,
    int OutputCharacters,
    int ToolCallCount,
    string ErrorCode,
    IReadOnlyList<AgentToolCallAuditRecord> ToolCalls);

public sealed record AgentToolCallAuditRecord(
    Guid ToolVersionId,
    string ToolName,
    McpToolRisk Risk,
    AgentRunEventKind Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    string ErrorCode);

public sealed record AgentRunContext(
    Guid RunId,
    Guid AgentId,
    AgentVersionSnapshot Snapshot,
    string Input,
    string InputSha256,
    DateTimeOffset StartedAtUtc,
    IReadOnlyList<PublishedMcpToolReference> Tools)
{
    public IReadOnlyList<PublishedSkillContent> Skills { get; init; } =
        SkillContractCloner.ReadOnly(Array.Empty<PublishedSkillContent>());

    public IReadOnlyList<KnowledgeSearchResult> Knowledge { get; init; } =
        KnowledgeContractCloner.ReadOnly(Array.Empty<KnowledgeSearchResult>());

    public IReadOnlyList<AgentConversationMessage> ConversationHistory { get; init; } =
        new ReadOnlyCollection<AgentConversationMessage>(
            Array.Empty<AgentConversationMessage>());

    public IReadOnlyList<IAgentInternalTool> InternalTools { get; init; } =
        Array.Empty<IAgentInternalTool>();

    public IAgentMcpCallGuard? McpCallGuard { get; init; }

    public IAgentMcpResultGuard? McpResultGuard { get; init; }

    public IReadOnlyList<AgentMcpToolCallLimit> McpToolCallLimits { get; init; } =
        Array.Empty<AgentMcpToolCallLimit>();

    public AgentExecutionIdentity? ExecutionIdentity { get; init; }

    public AgentToolApprovalBinding? ToolApprovalBinding { get; init; }

    public IAgentToolApprovalHandler? ToolApprovalHandler { get; init; }
}

public sealed record AgentMcpToolCallLimit(
    Guid ToolVersionId,
    int MaximumCalls,
    string ErrorCode,
    string Message);

public sealed record AgentToolApprovalBinding(
    Guid ConversationId,
    Guid EntryRunId);

public sealed record AgentToolApprovalRequest(
    AgentToolApprovalBinding Binding,
    Guid AgentRunId,
    Guid AgentVersionId,
    PublishedMcpToolReference Tool,
    string ArgumentsJson,
    AgentExecutionIdentity Requester);

public interface IAgentToolApprovalHandler
{
    Task<ToolApprovalRequestRecord> RequestAsync(
        AgentToolApprovalRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AgentRunError(string Code, string Message);

public sealed record AgentRunPreparationResult(
    AgentRunContext? Context,
    AgentRunError? Error)
{
    public bool Succeeded => Error is null;

    public static AgentRunPreparationResult Success(AgentRunContext context) =>
        new(context, null);

    public static AgentRunPreparationResult Failure(string code, string message) =>
        new(null, new AgentRunError(code, message));
}

public sealed record AgentRunExecutionOptions(
    IReadOnlyList<IAgentInternalTool>? InternalTools = null,
    IAgentMcpCallGuard? McpCallGuard = null)
{
    public IAgentMcpResultGuard? McpResultGuard { get; init; }

    public AgentExecutionIdentity? ExecutionIdentity { get; init; }

    public AgentToolApprovalBinding? ToolApprovalBinding { get; init; }

    public IAgentToolApprovalHandler? ToolApprovalHandler { get; init; }
}

public static class AgentRunErrorCodes
{
    public const string AgentNotFound = "AGENT_NOT_FOUND";
    public const string AgentDisabled = "AGENT_RUNTIME_DISABLED";
    public const string VersionMissing = "AGENT_PUBLISHED_VERSION_MISSING";
    public const string InputInvalid = "AGENT_RUN_INPUT_INVALID";
    public const string SkillUnavailable = "SKILL_VERSION_UNAVAILABLE";
    public const string ToolUnavailable = "MCP_TOOL_VERSION_UNAVAILABLE";
    public const string KnowledgeUnavailable = "KNOWLEDGE_BASE_UNAVAILABLE";
    public const string KnowledgeServiceUnavailable = "KNOWLEDGE_SERVICE_UNAVAILABLE";
    public const string KnowledgeRevisionStale = "KNOWLEDGE_REVISION_STALE";
    public const string KnowledgeBindingUnavailable = "KNOWLEDGE_BINDING_UNAVAILABLE";
    public const string ToolBlocked = "MCP_TOOL_CALL_BLOCKED";
    public const string ToolFailed = "MCP_TOOL_CALL_FAILED";
    public const string ToolTimedOut = "MCP_TOOL_CALL_TIMEOUT";
    public const string ToolResultTooLarge = "MCP_TOOL_RESULT_TOO_LARGE";
    public const string ToolArgumentLimitExceeded = "TOOL_ARGUMENT_LIMIT_EXCEEDED";
    public const string InternalToolResultTooLarge =
        "INTERNAL_TOOL_RESULT_TOO_LARGE";
    public const string InternalToolCallLimitExceeded =
        "INTERNAL_TOOL_CALL_LIMIT_EXCEEDED";
    public const string McpToolCallLimitExceeded =
        "MCP_TOOL_CALL_LIMIT_EXCEEDED";
    public const string ToolApprovalRequired = "MCP_TOOL_APPROVAL_REQUIRED";
    public const string ToolConfigurationInvalid = "AGENT_TOOL_CONFIGURATION_INVALID";
    public const string ModelCredentialMissing = "MODEL_CREDENTIAL_MISSING";
    public const string ModelFailed = "MODEL_INVOCATION_FAILED";
    public const string ModelOutputLimitExceeded = "MODEL_OUTPUT_LIMIT_EXCEEDED";
    public const string ModelOutputEventLimitExceeded =
        "MODEL_OUTPUT_EVENT_LIMIT_EXCEEDED";
    public const string ModelInputLimitExceeded = "MODEL_INPUT_LIMIT_EXCEEDED";
    public const string OutputInvalid = "AGENT_OUTPUT_INVALID";
}

public interface IAgentRuntimeEngine
{
    IAsyncEnumerable<AgentRunEvent> StreamAsync(
        AgentRunContext context,
        CancellationToken cancellationToken = default);
}

public interface IAgentRunAuditRepository
{
    Task SaveAsync(
        AgentRunAuditRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record AgentInternalToolResult(
    bool Succeeded,
    string Content,
    string ErrorCode);

public interface IAgentInternalTool
{
    string Name { get; }

    string Description { get; }

    string InputSchemaJson { get; }

    Task<AgentInternalToolResult> InvokeAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default);
}

public sealed record AgentMcpCallDenial(
    string ErrorCode,
    string Message);

public sealed record AgentMcpCallGuardResult(
    AgentMcpCallDenial? Denial)
{
    public bool Allowed => Denial is null;

    public static AgentMcpCallGuardResult Allow() =>
        new((AgentMcpCallDenial?)null);

    public static AgentMcpCallGuardResult Deny(
        string errorCode,
        string message) =>
        new(new AgentMcpCallDenial(errorCode, message));
}

public interface IAgentMcpCallGuard
{
    ValueTask<AgentMcpCallGuardResult> ReserveAsync(
        CancellationToken cancellationToken = default);
}

public sealed record McpRuntimeToolResult(
    bool Succeeded,
    bool Blocked,
    string Content,
    string ErrorCode);

public interface IMcpRuntimeToolInvoker
{
    Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);

    Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext? invocationContext,
        CancellationToken cancellationToken = default) =>
        InvokeAsync(toolVersionId, expectedRisk, arguments, cancellationToken);
}

public sealed record AgentMcpResultDenial(
    string ErrorCode,
    string Message);

public sealed record AgentMcpResultGuardResult(
    AgentMcpResultDenial? Denial)
{
    public bool Allowed => Denial is null;

    public static AgentMcpResultGuardResult Allow() =>
        new((AgentMcpResultDenial?)null);

    public static AgentMcpResultGuardResult Deny(
        string errorCode,
        string message) =>
        new(new AgentMcpResultDenial(errorCode, message));
}

public interface IAgentMcpResultGuard
{
    ValueTask<AgentMcpResultGuardResult> ReserveAsync(
        int resultUtf8Bytes,
        CancellationToken cancellationToken = default);
}

public static class AgentRunContractCloner
{
    public static AgentRunAuditRecord Clone(AgentRunAuditRecord record) =>
        record with
        {
            ToolCalls = new ReadOnlyCollection<AgentToolCallAuditRecord>(
                record.ToolCalls.Select(call => call with { }).ToArray())
        };
}

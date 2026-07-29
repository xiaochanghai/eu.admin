using System.Collections.ObjectModel;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Knowledge;

namespace EU.Core.Agent.Application.Runtime;

public enum AgentRunStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum AgentRunEventKind
{
    Started,
    Delta,
    Citation,
    ToolStarted,
    ToolSucceeded,
    ToolBlocked,
    ToolFailed,
    Completed,
    Failed,
    Cancelled
}

public sealed record AgentRunEvent(
    Guid RunId,
    long Sequence,
    AgentRunEventKind Kind,
    DateTimeOffset OccurredAtUtc,
    string Text = "",
    Guid? ToolVersionId = null,
    string ToolName = "",
    string ErrorCode = "",
    Guid? ToolCallId = null)
{
    public string ArgumentsJson { get; init; } = "";
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
    public IReadOnlyList<KnowledgeSearchResult> Knowledge { get; init; } =
        KnowledgeContractCloner.ReadOnly(Array.Empty<KnowledgeSearchResult>());
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

public static class AgentRunErrorCodes
{
    public const string AgentNotFound = "AGENT_NOT_FOUND";
    public const string AgentDisabled = "AGENT_RUNTIME_DISABLED";
    public const string VersionMissing = "AGENT_PUBLISHED_VERSION_MISSING";
    public const string InputInvalid = "AGENT_RUN_INPUT_INVALID";
    public const string ToolUnavailable = "MCP_TOOL_VERSION_UNAVAILABLE";
    public const string KnowledgeUnavailable = "KNOWLEDGE_BASE_UNAVAILABLE";
    public const string ToolBlocked = "MCP_TOOL_CALL_BLOCKED";
    public const string ToolFailed = "MCP_TOOL_CALL_FAILED";
    public const string ToolTimedOut = "MCP_TOOL_CALL_TIMEOUT";
    public const string ModelCredentialMissing = "MODEL_CREDENTIAL_MISSING";
    public const string ModelFailed = "MODEL_INVOCATION_FAILED";
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

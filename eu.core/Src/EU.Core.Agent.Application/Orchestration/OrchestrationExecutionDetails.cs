using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Application.Orchestration;

public sealed record ExecutionPayloadLimits(
    int NodeInputCharacters = 131_072,
    int NodeOutputCharacters = 262_144,
    int ToolArgumentsCharacters = 262_144,
    int ToolResultCharacters = 1_048_576,
    int FinalOutputCharacters = 262_144);

public sealed record OrchestrationToolCallRecord(
    Guid ToolCallId,
    Guid AgentRunId,
    Guid ToolVersionId,
    string ToolName,
    AgentRunEventKind Status,
    string ArgumentsJson,
    string ResultContent,
    string ResultSha256,
    int ResultCharacters,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string ErrorCode);

public sealed record OrchestrationNodeAttemptRecord(
    string NodeId,
    int Attempt,
    Guid AgentRunId,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    OrchestrationNodeRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string ErrorCode,
    IReadOnlyList<OrchestrationToolCallRecord> ToolCalls);

public sealed record OrchestrationRunDetails(
    Guid RunId,
    Guid OrchestrationId,
    string Input,
    string Output,
    IReadOnlyList<OrchestrationNodeAttemptRecord> Attempts);

#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.Orchestration;

public enum OrchestrationStatus { Enabled, Disabled, Archived }
public enum OrchestrationNodeInputMode { InitialInput, PreviousOutput, Template }
public enum OrchestrationEdgeCondition { Always, Succeeded, Failed, OutputContains }
public enum OrchestrationRunStatus { Running, Completed, Failed, Cancelled }
public enum OrchestrationNodeRunStatus { Pending, Running, Completed, Failed, Cancelled }
public enum OrchestrationTerminalTransitionPolicy { PreservePending, TerminalizePending }

public sealed record OrchestrationNode(
    string Id,
    string Name,
    Guid AgentId,
    OrchestrationNodeInputMode InputMode,
    string InputTemplate,
    int MaximumRetries,
    int TimeoutSeconds);

public sealed record OrchestrationEdge(
    string FromNodeId,
    string ToNodeId,
    OrchestrationEdgeCondition Condition,
    string ConditionValue,
    int Order);

public sealed record OrchestrationAgentBinding(Guid AgentId, Guid AgentVersionId);

public sealed record PublishedOrchestrationReference(
    Guid OrchestrationId, Guid OrchestrationVersionId, bool Enabled);

public interface IPublishedOrchestrationCatalog
{
    Task<IReadOnlyList<PublishedOrchestrationReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default);
}

public sealed record OrchestrationVersionSnapshot(
    Guid VersionId,
    string OrchestrationCode,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges,
    IReadOnlyList<OrchestrationAgentBinding> Agents);

public sealed record OrchestrationVersion(
    Guid Id,
    string Label,
    bool IsDraft,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges,
    OrchestrationVersionSnapshot? Snapshot);

public sealed record OrchestrationDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    OrchestrationStatus Status,
    long LogicalRevision,
    OrchestrationVersion Draft,
    IReadOnlyList<OrchestrationVersion> PublishedVersions);

public sealed record OrchestrationListItem(
    Guid Id, string Code, string Name, string Description, OrchestrationStatus Status,
    long LogicalRevision, int DraftNodeCount, string? CurrentPublishedLabel);

public sealed record CreateOrchestrationCommand(string Code, string Name, string Description);
public sealed record SaveOrchestrationDraftCommand(
    Guid Id,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    OrchestrationStatus Status,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges);
public sealed record PublishOrchestrationCommand(Guid Id, long ExpectedLogicalRevision);
public sealed record SetOrchestrationArchiveCommand(
    Guid Id,
    long ExpectedLogicalRevision,
    bool Archived);

public sealed record OrchestrationNodeRunRecord(
    string NodeId,
    string NodeName,
    Guid AgentId,
    Guid AgentVersionId,
    OrchestrationNodeRunStatus Status,
    int Attempts,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int OutputCharacters,
    string InputSha256,
    string ErrorCode);

public sealed record OrchestrationRunRecord(
    Guid Id,
    Guid OrchestrationId,
    Guid OrchestrationVersionId,
    string OrchestrationCode,
    OrchestrationRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string InputSha256,
    string ErrorCode,
    IReadOnlyList<OrchestrationNodeRunRecord> Nodes);

public sealed record OrchestrationError(string Code, string Message);
public sealed record OrchestrationOperationResult<T>(T? Value, OrchestrationError? Error)
{
    public bool Succeeded => Error is null;
    public static OrchestrationOperationResult<T> Success(T value) => new(value, null);
    public static OrchestrationOperationResult<T> Failure(string code, string message) =>
        new(default, new OrchestrationError(code, message));
}

public static class OrchestrationErrorCodes
{
    public const string NotFound = "ORCHESTRATION_NOT_FOUND";
    public const string CodeInvalid = "ORCHESTRATION_CODE_INVALID";
    public const string CodeConflict = "ORCHESTRATION_CODE_CONFLICT";
    public const string RowVersionConflict = "ORCHESTRATION_ROW_VERSION_CONFLICT";
    public const string DefinitionInvalid = "ORCHESTRATION_DEFINITION_INVALID";
    public const string VersionMissing = "ORCHESTRATION_PUBLISHED_VERSION_MISSING";
    public const string Disabled = "ORCHESTRATION_DISABLED";
    public const string AgentUnavailable = "ORCHESTRATION_AGENT_VERSION_UNAVAILABLE";
    public const string RunNotFound = "ORCHESTRATION_RUN_NOT_FOUND";
    public const string RunInputInvalid = "ORCHESTRATION_RUN_INPUT_INVALID";
    public const string PayloadLimitExceeded = "ORCHESTRATION_PAYLOAD_LIMIT_EXCEEDED";
    public const string LifecycleTransitionInvalid = "ORCHESTRATION_LIFECYCLE_TRANSITION_INVALID";
    public const string ArchiveBlocked = "ORCHESTRATION_ARCHIVE_BLOCKED";
}

public interface IOrchestrationRepository
{
    Task<OrchestrationDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrchestrationDefinition>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> TryCreateAsync(OrchestrationDefinition value, CancellationToken cancellationToken = default);
    Task<bool> TryReplaceAsync(OrchestrationDefinition value, long expectedRevision, CancellationToken cancellationToken = default);
}

public interface IOrchestrationRunRepository
{
    Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default);
    Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
        Guid orchestrationId, int take, CancellationToken cancellationToken = default);
    Task SaveDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default);
    Task<OrchestrationRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
    Task<bool> TrySaveRunningDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default);
    Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken = default);
    Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        string errorCode,
        CancellationToken cancellationToken = default);
}

public sealed record OrchestrationRunTransitionResult(
    OrchestrationRunRecord? Run,
    bool Transitioned);

public static class OrchestrationContractCloner
{
    public static OrchestrationDefinition Clone(OrchestrationDefinition value) =>
        value with
        {
            Draft = Clone(value.Draft),
            PublishedVersions = ReadOnly(value.PublishedVersions.Select(Clone))
        };

    public static OrchestrationVersion Clone(OrchestrationVersion value) =>
        value with
        {
            Nodes = ReadOnly(value.Nodes.Select(node => node with { })),
            Edges = ReadOnly(value.Edges.Select(edge => edge with { })),
            Snapshot = value.Snapshot is null ? null : Clone(value.Snapshot)
        };

    public static OrchestrationVersionSnapshot Clone(OrchestrationVersionSnapshot value) =>
        value with
        {
            Nodes = ReadOnly(value.Nodes.Select(node => node with { })),
            Edges = ReadOnly(value.Edges.Select(edge => edge with { })),
            Agents = ReadOnly(value.Agents.Select(agent => agent with { }))
        };

    public static OrchestrationRunRecord Clone(OrchestrationRunRecord value) =>
        value with { Nodes = ReadOnly(value.Nodes.Select(node => node with { })) };

    public static OrchestrationRunDetails Clone(OrchestrationRunDetails value) =>
        value with
        {
            Attempts = ReadOnly(value.Attempts.Select(attempt => attempt with
            {
                ToolCalls = ReadOnly(attempt.ToolCalls.Select(tool => tool with { }))
            }))
        };

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

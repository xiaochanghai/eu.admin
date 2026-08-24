#nullable enable

namespace EU.Core.IServices.Tasks;

public enum AgentTaskStatus
{
    Pending = 0,
    Running = 1,
    WaitingForApproval = 2,
    WaitingForUser = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

public enum AgentTaskAttemptStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3,
    Paused = 4
}

public static class AgentTaskEventKinds
{
    public const string Created = "created";
    public const string CheckpointSaved = "checkpoint-saved";
    public const string WaitingForApproval = "waiting-for-approval";
    public const string WaitingForUser = "waiting-for-user";
    public const string ResumedByUser = "resumed-by-user";
    public const string Completed = "completed";
    public const string RetryScheduled = "retry-scheduled";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string RunSynchronized = "run-synchronized";
}

public sealed record AgentTaskRecord(
    Guid Id,
    string TenantId,
    string UserId,
    string Title,
    string Description,
    string Input,
    string InputSha256,
    string SourceType,
    string SourceId,
    string IdempotencyKey,
    Guid? ConversationId,
    Guid? CurrentRunId,
    AgentTaskStatus Status,
    int Priority,
    int AttemptCount,
    int MaximumAttempts,
    long LogicalRevision,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string LeaseOwner,
    DateTimeOffset? LeaseExpiresAtUtc,
    string CheckpointKind,
    string CheckpointJson,
    string LastErrorCode,
    string LastErrorMessage);

public sealed record AgentTaskAttemptRecord(
    Guid Id,
    Guid TaskId,
    int AttemptNumber,
    Guid? RunId,
    AgentTaskAttemptStatus Status,
    string WorkerId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string ErrorCode,
    string ErrorMessage);

public sealed record AgentTaskEventRecord(
    Guid Id,
    Guid TaskId,
    int? AttemptNumber,
    Guid? RunId,
    string Kind,
    AgentTaskStatus Status,
    string WorkerId,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);

public sealed record CreateAgentTaskCommand(
    string TenantId,
    string UserId,
    string Title,
    string Description,
    string Input,
    string SourceType,
    string SourceId,
    string IdempotencyKey,
    Guid? ConversationId,
    int Priority,
    int MaximumAttempts,
    DateTimeOffset AvailableAtUtc);

public sealed record AgentTaskQuery(
    string TenantId,
    string UserId,
    AgentTaskStatus? Status,
    int Take);

public sealed record ClaimAgentTaskCommand(
    string TenantId,
    string WorkerId,
    TimeSpan LeaseDuration,
    DateTimeOffset ClaimedAtUtc,
    bool AcrossTenants = false,
    string SourceType = "");

public sealed record SaveAgentTaskCheckpointCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    Guid? RunId,
    Guid? ConversationId,
    string CheckpointKind,
    string CheckpointJson,
    DateTimeOffset SavedAtUtc);

public sealed record WaitAgentTaskCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    AgentTaskStatus Status,
    Guid? RunId,
    Guid? ConversationId,
    string CheckpointKind,
    string CheckpointJson,
    DateTimeOffset PausedAtUtc);

public sealed record RenewAgentTaskLeaseCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    TimeSpan LeaseDuration,
    DateTimeOffset RenewedAtUtc);

public sealed record CompleteAgentTaskCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    Guid? RunId,
    DateTimeOffset FinishedAtUtc);

public sealed record FailAgentTaskCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    string ErrorCode,
    string ErrorMessage,
    TimeSpan RetryDelay,
    DateTimeOffset FailedAtUtc);

public sealed record ResumeAgentTaskWithUserInputCommand(
    Guid TaskId,
    string TenantId,
    string UserId,
    long ExpectedLogicalRevision,
    string Input,
    DateTimeOffset ResumedAtUtc);

public sealed record SynchronizeAgentTaskRunCommand(
    Guid RunId,
    string TenantId,
    string UserId,
    AgentTaskStatus Status,
    string ErrorCode,
    DateTimeOffset FinishedAtUtc);

public static class AgentTaskErrorCodes
{
    public const string Invalid = "AGENT_TASK_INVALID";
    public const string NotFound = "AGENT_TASK_NOT_FOUND";
    public const string Conflict = "AGENT_TASK_STATE_CONFLICT";
    public const string LeaseInvalid = "AGENT_TASK_LEASE_INVALID";
    public const string IdempotencyKeyReused = "AGENT_TASK_IDEMPOTENCY_KEY_REUSED";
}

public sealed class AgentTaskException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

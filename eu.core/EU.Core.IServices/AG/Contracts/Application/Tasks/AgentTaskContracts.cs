#nullable enable

namespace EU.Core.IServices.Tasks;

/// <summary>
/// Agent 任务的生命周期状态。
/// </summary>
public enum AgentTaskStatus
{
    /// <summary>等待执行。</summary>
    Pending = 0,
    /// <summary>正在运行。</summary>
    Running = 1,
    /// <summary>等待工具调用审批。</summary>
    WaitingForApproval = 2,
    /// <summary>等待用户提供信息或确认。</summary>
    WaitingForUser = 3,
    /// <summary>已完成。</summary>
    Completed = 4,
    /// <summary>执行失败。</summary>
    Failed = 5,
    /// <summary>已取消。</summary>
    Cancelled = 6
}

/// <summary>
/// Agent 任务单次执行尝试的状态。
/// </summary>
public enum AgentTaskAttemptStatus
{
    /// <summary>正在运行。</summary>
    Running = 0,
    /// <summary>已完成。</summary>
    Completed = 1,
    /// <summary>执行失败。</summary>
    Failed = 2,
    /// <summary>已取消。</summary>
    Cancelled = 3,
    /// <summary>已暂停，等待后续恢复。</summary>
    Paused = 4
}

/// <summary>
/// 定义 Agent 任务持久化事件的类型名称。
/// </summary>
public static class AgentTaskEventKinds
{
    /// <summary>表示 <c>Created</c> 任务事件的类型名称。</summary>
    public const string Created = "created";
    /// <summary>表示 <c>CheckpointSaved</c> 任务事件的类型名称。</summary>
    public const string CheckpointSaved = "checkpoint-saved";
    /// <summary>表示 <c>WaitingForApproval</c> 任务事件的类型名称。</summary>
    public const string WaitingForApproval = "waiting-for-approval";
    /// <summary>表示 <c>WaitingForUser</c> 任务事件的类型名称。</summary>
    public const string WaitingForUser = "waiting-for-user";
    /// <summary>表示 <c>ResumedByUser</c> 任务事件的类型名称。</summary>
    public const string ResumedByUser = "resumed-by-user";
    /// <summary>表示 <c>Completed</c> 任务事件的类型名称。</summary>
    public const string Completed = "completed";
    /// <summary>表示 <c>RetryScheduled</c> 任务事件的类型名称。</summary>
    public const string RetryScheduled = "retry-scheduled";
    /// <summary>表示 <c>Failed</c> 任务事件的类型名称。</summary>
    public const string Failed = "failed";
    /// <summary>表示 <c>Cancelled</c> 任务事件的类型名称。</summary>
    public const string Cancelled = "cancelled";
    /// <summary>表示 <c>RunSynchronized</c> 任务事件的类型名称。</summary>
    public const string RunSynchronized = "run-synchronized";
}

/// <summary>
/// 可恢复 Agent 任务的当前状态记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Title">任务标题。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Input">运行、任务或节点的输入内容。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="SourceType">任务来源类型。</param>
/// <param name="SourceId">来源业务对象标识。</param>
/// <param name="IdempotencyKey">创建任务使用的幂等键。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="CurrentRunId">当前关联的运行标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="Priority">任务调度优先级。</param>
/// <param name="AttemptCount">已经开始的执行尝试次数。</param>
/// <param name="MaximumAttempts">允许的最大执行尝试次数。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="AvailableAtUtc">任务最早可被认领的 UTC 时间。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="LeaseOwner">当前租约持有者。</param>
/// <param name="LeaseExpiresAtUtc">当前租约到期的 UTC 时间。</param>
/// <param name="CheckpointKind">检查点内容类型。</param>
/// <param name="CheckpointJson">检查点数据 JSON。</param>
/// <param name="LastErrorCode">最近一次失败的错误码。</param>
/// <param name="LastErrorMessage">最近一次失败的错误说明。</param>
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

/// <summary>
/// Agent 任务单次执行尝试记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="AttemptNumber">执行尝试序号。</param>
/// <param name="RunId">运行标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="ErrorMessage">失败错误说明。</param>
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

/// <summary>
/// Agent 任务状态变化事件。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="AttemptNumber">执行尝试序号。</param>
/// <param name="RunId">运行标识。</param>
/// <param name="Kind">事件类型。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="OccurredAtUtc">事件发生的 UTC 时间。</param>
/// <param name="PayloadJson">任务事件附带的数据 JSON。</param>
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

/// <summary>
/// 创建 Agent 任务的命令。
/// </summary>
/// <param name="TenantId">租户标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Title">任务标题。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Input">运行、任务或节点的输入内容。</param>
/// <param name="SourceType">任务来源类型。</param>
/// <param name="SourceId">来源业务对象标识。</param>
/// <param name="IdempotencyKey">创建任务使用的幂等键。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="Priority">任务调度优先级。</param>
/// <param name="MaximumAttempts">允许的最大执行尝试次数。</param>
/// <param name="AvailableAtUtc">任务最早可被认领的 UTC 时间。</param>
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

/// <summary>
/// Agent 任务的查询条件。
/// </summary>
/// <param name="TenantId">租户标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="Take">最多返回的任务数量。</param>
public sealed record AgentTaskQuery(
    string TenantId,
    string UserId,
    AgentTaskStatus? Status,
    int Take);

/// <summary>
/// 工作进程认领 Agent 任务的命令。
/// </summary>
/// <param name="TenantId">租户标识。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="LeaseDuration">申请或续订的租约时长。</param>
/// <param name="ClaimedAtUtc">任务被认领的 UTC 时间。</param>
/// <param name="AcrossTenants">是否允许跨租户认领任务。</param>
/// <param name="SourceType">任务来源类型。</param>
public sealed record ClaimAgentTaskCommand(
    string TenantId,
    string WorkerId,
    TimeSpan LeaseDuration,
    DateTimeOffset ClaimedAtUtc,
    bool AcrossTenants = false,
    string SourceType = "");

/// <summary>
/// 保存 Agent 任务检查点的命令。
/// </summary>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="RunId">运行标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="CheckpointKind">检查点内容类型。</param>
/// <param name="CheckpointJson">检查点数据 JSON。</param>
/// <param name="SavedAtUtc">检查点保存的 UTC 时间。</param>
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

/// <summary>
/// 暂停 Agent 任务并进入等待状态的命令。
/// </summary>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="RunId">运行标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="CheckpointKind">检查点内容类型。</param>
/// <param name="CheckpointJson">检查点数据 JSON。</param>
/// <param name="PausedAtUtc">任务进入等待状态的 UTC 时间。</param>
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

/// <summary>
/// 续订 Agent 任务租约的命令。
/// </summary>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="LeaseDuration">申请或续订的租约时长。</param>
/// <param name="RenewedAtUtc">任务租约续订的 UTC 时间。</param>
public sealed record RenewAgentTaskLeaseCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    TimeSpan LeaseDuration,
    DateTimeOffset RenewedAtUtc);

/// <summary>
/// 完成 Agent 任务的命令。
/// </summary>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="RunId">运行标识。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
public sealed record CompleteAgentTaskCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    Guid? RunId,
    DateTimeOffset FinishedAtUtc);

/// <summary>
/// 记录 Agent 任务失败并安排重试的命令。
/// </summary>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="WorkerId">执行任务的工作进程标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="ErrorMessage">失败错误说明。</param>
/// <param name="RetryDelay">失败后再次可用前的等待时长。</param>
/// <param name="FailedAtUtc">任务失败的 UTC 时间。</param>
public sealed record FailAgentTaskCommand(
    Guid TaskId,
    string TenantId,
    string WorkerId,
    long ExpectedLogicalRevision,
    string ErrorCode,
    string ErrorMessage,
    TimeSpan RetryDelay,
    DateTimeOffset FailedAtUtc);

/// <summary>
/// 使用用户输入恢复 Agent 任务的命令。
/// </summary>
/// <param name="TaskId">Agent 任务标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Input">运行、任务或节点的输入内容。</param>
/// <param name="ResumedAtUtc">任务恢复的 UTC 时间。</param>
public sealed record ResumeAgentTaskWithUserInputCommand(
    Guid TaskId,
    string TenantId,
    string UserId,
    long ExpectedLogicalRevision,
    string Input,
    DateTimeOffset ResumedAtUtc);

/// <summary>
/// 根据运行结果同步 Agent 任务状态的命令。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
public sealed record SynchronizeAgentTaskRunCommand(
    Guid RunId,
    string TenantId,
    string UserId,
    AgentTaskStatus Status,
    string ErrorCode,
    DateTimeOffset FinishedAtUtc);

/// <summary>
/// 定义 Agent 任务领域错误码。
/// </summary>
public static class AgentTaskErrorCodes
{
    /// <summary>表示 <c>Invalid</c> 场景的错误码。</summary>
    public const string Invalid = "AGENT_TASK_INVALID";
    /// <summary>表示 <c>NotFound</c> 场景的错误码。</summary>
    public const string NotFound = "AGENT_TASK_NOT_FOUND";
    /// <summary>表示 <c>Conflict</c> 场景的错误码。</summary>
    public const string Conflict = "AGENT_TASK_STATE_CONFLICT";
    /// <summary>表示 <c>LeaseInvalid</c> 场景的错误码。</summary>
    public const string LeaseInvalid = "AGENT_TASK_LEASE_INVALID";
    /// <summary>表示 <c>IdempotencyKeyReused</c> 场景的错误码。</summary>
    public const string IdempotencyKeyReused = "AGENT_TASK_IDEMPOTENCY_KEY_REUSED";
}

/// <summary>
/// 表示 Agent 任务操作中的领域异常。
/// </summary>
/// <param name="errorCode">用于标识失败原因的领域错误码。</param>
/// <param name="message">描述异常原因的错误消息。</param>
public sealed class AgentTaskException(string errorCode, string message) : Exception(message)
{
    /// <summary>
    /// 获取领域异常对应的错误码。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}

#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>
/// 统一入口运行的状态。
/// </summary>
public enum UnifiedRunStatus
{
    /// <summary>等待执行。</summary>
    Pending,
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>等待工具调用审批。</summary>
    WaitingForApproval,
    /// <summary>已完成。</summary>
    Completed,
    /// <summary>运行失败。</summary>
    Failed,
    /// <summary>已取消。</summary>
    Cancelled,
    /// <summary>运行被策略或前置条件阻止。</summary>
    Blocked
}

/// <summary>
/// 统一入口中 Agent 运行的层级类型。
/// </summary>
public enum UnifiedAgentRunKind
{
    /// <summary>主 Agent 运行。</summary>
    Main,
    /// <summary>由主 Agent 委派的子 Agent 运行。</summary>
    Child
}

/// <summary>
/// 统一会话消息的参与方角色。
/// </summary>
public enum ConversationMessageRole
{
    /// <summary>用户。</summary>
    User,
    /// <summary>Agent 助手。</summary>
    Assistant
}

/// <summary>
/// 定义统一入口执行树的调用次数、超时和载荷限制。
/// </summary>
public sealed record UnifiedEntryLimits
{
    #region 构造（UnifiedEntryLimits）
    /// <summary>
    /// 构造（UnifiedEntryLimits）
    /// </summary>
    /// <param name="MaxAgentDepth">允许的最大 Agent 委派深度。</param>
    /// <param name="MaxChildCalls">允许的最大子 Agent 调用次数。</param>
    /// <param name="MaxOrchestrationCalls">允许的最大编排调用次数。</param>
    /// <param name="MaxMcpCalls">允许的最大 MCP 工具调用次数。</param>
    /// <param name="EntryTimeout">统一入口执行超时；未指定时使用默认值。</param>
    /// <param name="ChildTimeout">子 Agent 执行超时；未指定时使用默认值。</param>
    /// <param name="InternalPayloadUtf8Bytes">内部载荷允许的最大 UTF-8 字节数。</param>
    /// <param name="MaxMcpResultUtf8Bytes">单次 MCP 结果允许的最大 UTF-8 字节数。</param>
    public UnifiedEntryLimits(
        int MaxAgentDepth = 4,
        int MaxChildCalls = 8,
        int MaxOrchestrationCalls = 4,
        int MaxMcpCalls = 20,
        TimeSpan? EntryTimeout = null,
        TimeSpan? ChildTimeout = null,
        int InternalPayloadUtf8Bytes = 32_768,
        int MaxMcpResultUtf8Bytes = 4_194_304)
    {
        this.MaxAgentDepth = MaxAgentDepth;
        this.MaxChildCalls = MaxChildCalls;
        this.MaxOrchestrationCalls = MaxOrchestrationCalls;
        this.MaxMcpCalls = MaxMcpCalls;
        this.EntryTimeout = EntryTimeout ?? TimeSpan.FromSeconds(300);
        this.ChildTimeout = ChildTimeout ?? TimeSpan.FromSeconds(120);
        this.InternalPayloadUtf8Bytes = InternalPayloadUtf8Bytes;
        this.MaxMcpResultUtf8Bytes = MaxMcpResultUtf8Bytes;
    }
    #endregion

    /// <summary>
    /// Agent 委派允许的最大深度。
    /// </summary>
    public int MaxAgentDepth { get; init; }

    /// <summary>
    /// 允许的最大子 Agent 调用次数。
    /// </summary>
    public int MaxChildCalls { get; init; }

    /// <summary>
    /// 允许的最大编排调用次数。
    /// </summary>
    public int MaxOrchestrationCalls { get; init; }

    /// <summary>
    /// 允许的最大 MCP 工具调用次数。
    /// </summary>
    public int MaxMcpCalls { get; init; }

    /// <summary>
    /// 统一入口执行超时时间。
    /// </summary>
    public TimeSpan EntryTimeout { get; init; }

    /// <summary>
    /// 子 Agent 执行超时时间。
    /// </summary>
    public TimeSpan ChildTimeout { get; init; }

    /// <summary>
    /// 内部载荷允许的最大 UTF-8 字节数。
    /// </summary>
    public int InternalPayloadUtf8Bytes { get; init; }

    /// <summary>
    /// 单次 MCP 结果允许的最大 UTF-8 字节数。
    /// </summary>
    public int MaxMcpResultUtf8Bytes { get; init; }

    public static UnifiedEntryLimits Default { get; } = new();
}

/// <summary>
/// 定义统一入口执行领域错误码。
/// </summary>
public static class UnifiedEntryErrorCodes
{
    /// <summary>表示 <c>PayloadLimitExceeded</c> 场景的错误码。</summary>
    public const string PayloadLimitExceeded = "UNIFIED_ENTRY_PAYLOAD_LIMIT_EXCEEDED";
    /// <summary>表示 <c>PayloadInvalidEncoding</c> 场景的错误码。</summary>
    public const string PayloadInvalidEncoding = "UNIFIED_ENTRY_PAYLOAD_INVALID_ENCODING";
    /// <summary>表示 <c>SequenceExhausted</c> 场景的错误码。</summary>
    public const string SequenceExhausted = "UNIFIED_ENTRY_SEQUENCE_EXHAUSTED";
    /// <summary>表示 <c>AgentCycleDetected</c> 场景的错误码。</summary>
    public const string AgentCycleDetected = "UNIFIED_ENTRY_AGENT_CYCLE_DETECTED";
    /// <summary>表示 <c>AgentDepthLimitExceeded</c> 场景的错误码。</summary>
    public const string AgentDepthLimitExceeded = "UNIFIED_ENTRY_AGENT_DEPTH_LIMIT_EXCEEDED";
    /// <summary>表示 <c>ChildCallLimitExceeded</c> 场景的错误码。</summary>
    public const string ChildCallLimitExceeded = "UNIFIED_ENTRY_CHILD_CALL_LIMIT_EXCEEDED";
    /// <summary>表示 <c>ChildCatalogInvalid</c> 场景的错误码。</summary>
    public const string ChildCatalogInvalid = "UNIFIED_ENTRY_CHILD_CATALOG_INVALID";
    /// <summary>表示 <c>OrchestrationCallLimitExceeded</c> 场景的错误码。</summary>
    public const string OrchestrationCallLimitExceeded =
        "UNIFIED_ENTRY_ORCHESTRATION_CALL_LIMIT_EXCEEDED";
    /// <summary>表示 <c>McpCallLimitExceeded</c> 场景的错误码。</summary>
    public const string McpCallLimitExceeded = "UNIFIED_ENTRY_MCP_CALL_LIMIT_EXCEEDED";
    /// <summary>表示 <c>McpResultLimitExceeded</c> 场景的错误码。</summary>
    public const string McpResultLimitExceeded =
        "UNIFIED_ENTRY_MCP_RESULT_LIMIT_EXCEEDED";
    /// <summary>表示 <c>InvalidState</c> 场景的错误码。</summary>
    public const string InvalidState = "UNIFIED_ENTRY_INVALID_STATE";
    /// <summary>表示 <c>InternalArgumentsInvalid</c> 场景的错误码。</summary>
    public const string InternalArgumentsInvalid =
        "UNIFIED_ENTRY_INTERNAL_ARGUMENTS_INVALID";
    /// <summary>表示 <c>AgentVersionUnauthorized</c> 场景的错误码。</summary>
    public const string AgentVersionUnauthorized =
        "UNIFIED_ENTRY_AGENT_VERSION_UNAUTHORIZED";
    /// <summary>表示 <c>OrchestrationVersionUnauthorized</c> 场景的错误码。</summary>
    public const string OrchestrationVersionUnauthorized =
        "UNIFIED_ENTRY_ORCHESTRATION_VERSION_UNAUTHORIZED";
    /// <summary>表示 <c>SkillVersionUnauthorized</c> 场景的错误码。</summary>
    public const string SkillVersionUnauthorized =
        "UNIFIED_ENTRY_SKILL_VERSION_UNAUTHORIZED";
    /// <summary>表示 <c>KnowledgeBaseUnauthorized</c> 场景的错误码。</summary>
    public const string KnowledgeBaseUnauthorized =
        KnowledgeAccessDenied;
    /// <summary>表示 <c>KnowledgeAccessDenied</c> 场景的错误码。</summary>
    public const string KnowledgeAccessDenied = "KNOWLEDGE_ACCESS_DENIED";
    /// <summary>表示 <c>EntryTimeout</c> 场景的错误码。</summary>
    public const string EntryTimeout = "UNIFIED_ENTRY_TIMEOUT";
    /// <summary>表示 <c>ChildTimeout</c> 场景的错误码。</summary>
    public const string ChildTimeout = "UNIFIED_ENTRY_CHILD_TIMEOUT";
    /// <summary>表示 <c>Cancelled</c> 场景的错误码。</summary>
    public const string Cancelled = "UNIFIED_ENTRY_CANCELLED";
    /// <summary>表示 <c>ChildExecutionFailed</c> 场景的错误码。</summary>
    public const string ChildExecutionFailed =
        "UNIFIED_ENTRY_CHILD_EXECUTION_FAILED";
    /// <summary>表示 <c>OrchestrationExecutionFailed</c> 场景的错误码。</summary>
    public const string OrchestrationExecutionFailed =
        "UNIFIED_ENTRY_ORCHESTRATION_EXECUTION_FAILED";
    /// <summary>表示 <c>OrchestrationDetailsMissing</c> 场景的错误码。</summary>
    public const string OrchestrationDetailsMissing =
        "UNIFIED_ENTRY_ORCHESTRATION_DETAILS_MISSING";
    /// <summary>表示 <c>ConversationNotFound</c> 场景的错误码。</summary>
    public const string ConversationNotFound =
        "UNIFIED_ENTRY_CONVERSATION_NOT_FOUND";
    /// <summary>表示 <c>PersistenceFailed</c> 场景的错误码。</summary>
    public const string PersistenceFailed =
        "UNIFIED_ENTRY_PERSISTENCE_FAILED";
    /// <summary>表示 <c>BusinessQueryEvidenceRequired</c> 场景的错误码。</summary>
    public const string BusinessQueryEvidenceRequired =
        "BUSINESS_QUERY_EVIDENCE_REQUIRED";
    /// <summary>表示 <c>BusinessQueryCallLimitExceeded</c> 场景的错误码。</summary>
    public const string BusinessQueryCallLimitExceeded =
        "BUSINESS_QUERY_CALL_LIMIT_EXCEEDED";
    /// <summary>表示 <c>BusinessQueryResultLimitExceeded</c> 场景的错误码。</summary>
    public const string BusinessQueryResultLimitExceeded =
        "BUSINESS_QUERY_RESULT_LIMIT_EXCEEDED";
    /// <summary>表示 <c>HostInterrupted</c> 场景的错误码。</summary>
    public const string HostInterrupted =
        "UNIFIED_ENTRY_HOST_INTERRUPTED";
}

/// <summary>
/// 表示统一入口准备或执行过程中的领域异常。
/// </summary>
/// <param name="errorCode">用于标识失败原因的领域错误码。</param>
/// <param name="message">描述异常原因的错误消息。</param>
public sealed class UnifiedEntryException(string errorCode, string message)
    : Exception(message)
{
    /// <summary>
    /// 获取领域异常对应的错误码。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// 统一入口的会话记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Title">会话标题。</param>
/// <param name="CreatedAtUtc">记录创建的 UTC 时间。</param>
/// <param name="UpdatedAtUtc">记录最近更新的 UTC 时间。</param>
public sealed record ConversationRecord(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>
    /// 租户标识。
    /// </summary>
    public string TenantId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
    /// <summary>
    /// 用户标识。
    /// </summary>
    public string UserId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
}

/// <summary>
/// 统一入口的会话消息记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="Role">会话消息的参与方角色。</param>
/// <param name="Content">消息、文件或载荷内容。</param>
/// <param name="ContentSha256">内容的 SHA-256 摘要。</param>
/// <param name="ContentUtf8Bytes">内容按 UTF-8 编码后的字节数。</param>
/// <param name="CreatedAtUtc">记录创建的 UTC 时间。</param>
public sealed record ConversationMessageRecord(
    Guid Id,
    Guid ConversationId,
    ConversationMessageRole Role,
    string Content,
    string ContentSha256,
    int ContentUtf8Bytes,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>
    /// 消息或结果类型。
    /// </summary>
    public ConversationMessageKind Kind { get; init; } = ConversationMessageKind.Legacy;
    /// <summary>
    /// 业务查询标识。
    /// </summary>
    public Guid? BusinessQueryId { get; init; }
    /// <summary>
    /// 业务查询的权威回执 JSON。
    /// </summary>
    public string BusinessQueryReceiptJson { get; init; } = string.Empty;
    /// <summary>
    /// 业务查询的展示结果 JSON。
    /// </summary>
    public string BusinessQueryPresentationJson { get; init; } = string.Empty;
    /// <summary>
    /// 业务查询结果的 SHA-256 完整性摘要。
    /// </summary>
    public string BusinessQueryIntegritySha256 { get; init; } = string.Empty;
}

/// <summary>
/// 统一会话消息承载的内容类型。
/// </summary>
public enum ConversationMessageKind
{
    /// <summary>尚未细分的兼容性历史消息。</summary>
    Legacy,
    /// <summary>用户输入。</summary>
    UserInput,
    /// <summary>Agent 面向用户生成的叙述性内容。</summary>
    AssistantNarrative,
    /// <summary>业务查询工具返回的结构化结果。</summary>
    BusinessQueryResult
}

/// <summary>
/// 统一入口顶层运行记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="CorrelationId">用于关联完整执行链路的标识。</param>
/// <param name="MainAgentVersionId">主 Agent 版本标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="Duration">执行持续时间。</param>
/// <param name="Input">运行或评测用例的输入内容。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="Output">运行产生的输出内容。</param>
/// <param name="OutputSha256">输出内容的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record UnifiedEntryRunRecord(
    Guid Id,
    Guid ConversationId,
    Guid CorrelationId,
    Guid MainAgentVersionId,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    string ErrorCode)
{
    /// <summary>
    /// 租户标识。
    /// </summary>
    public string TenantId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
    /// <summary>
    /// 用户标识。
    /// </summary>
    public string UserId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
}

/// <summary>
/// 定义统一入口记录的所有权兼容规则。
/// </summary>
public static class UnifiedEntryOwnership
{
    /// <summary>历史无所有者记录使用的兼容性标识。</summary>
    public const string LegacyUnowned = "__legacy_unowned__";
}

/// <summary>
/// 业务查询敏感载荷清理结果。
/// </summary>
/// <param name="MessagesRedacted">已清理正文的会话消息数量。</param>
/// <param name="ToolCallsRedacted">已清理结果的工具调用数量。</param>
/// <param name="EventsRedacted">已清理载荷的运行事件数量。</param>
/// <param name="CutoffUtc">本次清理使用的 UTC 截止时间。</param>
public sealed record BusinessQueryCleanupResult(
    int MessagesRedacted,
    int ToolCallsRedacted,
    int EventsRedacted,
    DateTimeOffset CutoffUtc);

/// <summary>
/// 统一入口中的 Agent 运行记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="ParentRunId">父级 Agent 或编排运行标识。</param>
/// <param name="Kind">运行或事件类型。</param>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="Depth">在统一执行树中的嵌套深度。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="Duration">执行持续时间。</param>
/// <param name="Input">运行或评测用例的输入内容。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="Output">运行产生的输出内容。</param>
/// <param name="OutputSha256">输出内容的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record UnifiedAgentRunRecord(
    Guid Id,
    Guid EntryRunId,
    Guid? ParentRunId,
    UnifiedAgentRunKind Kind,
    Guid AgentId,
    Guid AgentVersionId,
    int Depth,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    string ErrorCode);

/// <summary>
/// 统一入口与编排运行的关联记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="ParentRunId">父级 Agent 或编排运行标识。</param>
/// <param name="OrchestrationRunId">编排运行标识。</param>
/// <param name="OrchestrationVersionId">编排版本标识。</param>
/// <param name="Depth">在统一执行树中的嵌套深度。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="Duration">执行持续时间。</param>
/// <param name="Input">运行或评测用例的输入内容。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="Output">运行产生的输出内容。</param>
/// <param name="OutputSha256">输出内容的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record UnifiedOrchestrationRunLink(
    Guid Id,
    Guid EntryRunId,
    Guid ParentRunId,
    Guid OrchestrationRunId,
    Guid OrchestrationVersionId,
    int Depth,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    string ErrorCode);

/// <summary>
/// 统一入口中的工具调用记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="ParentRunId">父级 Agent 或编排运行标识。</param>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="Depth">在统一执行树中的嵌套深度。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="Duration">执行持续时间。</param>
/// <param name="ArgumentsJson">工具调用参数 JSON。</param>
/// <param name="ArgumentsSha256">工具参数的 SHA-256 摘要。</param>
/// <param name="ResultContent">工具调用结果内容。</param>
/// <param name="ResultSha256">工具结果的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record UnifiedToolCallRecord(
    Guid Id,
    Guid EntryRunId,
    Guid ParentRunId,
    Guid ToolVersionId,
    int Depth,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string ArgumentsJson,
    string ArgumentsSha256,
    string ResultContent,
    string ResultSha256,
    string ErrorCode);

/// <summary>
/// 统一入口运行的持久化事件记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="Sequence">运行事件的顺序号。</param>
/// <param name="CorrelationId">用于关联完整执行链路的标识。</param>
/// <param name="Kind">运行或事件类型。</param>
/// <param name="OccurredAtUtc">事件发生的 UTC 时间。</param>
/// <param name="ParentRunId">父级 Agent 或编排运行标识。</param>
/// <param name="Depth">在统一执行树中的嵌套深度。</param>
/// <param name="PayloadJson">事件附带的数据 JSON。</param>
/// <param name="PayloadSha256">事件载荷的 SHA-256 摘要。</param>
public sealed record UnifiedRunEventRecord(
    Guid Id,
    Guid EntryRunId,
    long Sequence,
    Guid CorrelationId,
    string Kind,
    DateTimeOffset OccurredAtUtc,
    Guid? ParentRunId,
    int Depth,
    string PayloadJson,
    string PayloadSha256);

/// <summary>
/// 汇总统一入口运行及其子运行和工具调用明细。
/// </summary>
public sealed record UnifiedRunDetails
{
    #region 构造（UnifiedRunDetails）
    /// <summary>
    /// 构造（UnifiedRunDetails）
    /// </summary>
    /// <param name="entryRun">统一入口的主运行记录。</param>
    /// <param name="agentRuns">统一入口关联的 Agent 运行记录集合。</param>
    /// <param name="orchestrations">编排仓储。</param>
    /// <param name="toolCalls">工具调用记录集合。</param>
    public UnifiedRunDetails(
        UnifiedEntryRunRecord entryRun,
        IReadOnlyList<UnifiedAgentRunRecord> agentRuns,
        IReadOnlyList<UnifiedOrchestrationRunLink> orchestrations,
        IReadOnlyList<UnifiedToolCallRecord> toolCalls)
    {
        EntryRun = entryRun with { };
        AgentRuns = UnifiedEntryContractCloner.ReadOnly(
            agentRuns.Select(value => value with { }));
        Orchestrations = UnifiedEntryContractCloner.ReadOnly(
            orchestrations.Select(value => value with { }));
        ToolCalls = UnifiedEntryContractCloner.ReadOnly(
            toolCalls.Select(value => value with { }));
    }
    #endregion

    /// <summary>
    /// 获取统一入口主运行记录。
    /// </summary>
    public UnifiedEntryRunRecord EntryRun { get; }

    /// <summary>
    /// 获取关联的 Agent 运行记录。
    /// </summary>
    public IReadOnlyList<UnifiedAgentRunRecord> AgentRuns { get; }

    /// <summary>
    /// 获取关联的编排运行记录。
    /// </summary>
    public IReadOnlyList<UnifiedOrchestrationRunLink> Orchestrations { get; }

    /// <summary>
    /// 获取关联的工具调用记录。
    /// </summary>
    public IReadOnlyList<UnifiedToolCallRecord> ToolCalls { get; }

    #region 处理（WithEntryRun）
    /// <summary>
    /// 处理（WithEntryRun）
    /// </summary>
    /// <param name="entryRun">统一入口的主运行记录。</param>
    /// <returns>替换入口运行、保留原 Agent、编排和工具记录的详情副本。</returns>
    public UnifiedRunDetails WithEntryRun(UnifiedEntryRunRecord entryRun) =>
        new(entryRun, AgentRuns, Orchestrations, ToolCalls);
    #endregion
}

/// <summary>
/// 表示统一入口会话、运行、详情和事件的聚合快照。
/// </summary>
public sealed record UnifiedEntryAggregate
{
    #region 构造（UnifiedEntryAggregate）
    /// <summary>
    /// 构造（UnifiedEntryAggregate）
    /// </summary>
    /// <param name="conversation">聚合所属的会话记录。</param>
    /// <param name="messages">会话消息集合。</param>
    /// <param name="details">统一入口运行及其关联执行的明细。</param>
    /// <param name="events">事件集合。</param>
    /// <param name="persistenceRevision">聚合当前的持久化修订号。</param>
    public UnifiedEntryAggregate(
        ConversationRecord conversation,
        IReadOnlyList<ConversationMessageRecord> messages,
        UnifiedRunDetails details,
        IReadOnlyList<UnifiedRunEventRecord> events,
        long persistenceRevision = 0)
    {
        if (persistenceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(persistenceRevision),
                "The persistence revision cannot be negative.");
        }

        Conversation = conversation with { };
        Messages = UnifiedEntryContractCloner.ReadOnly(
            messages.Select(value => value with { }));
        Details = UnifiedEntryContractCloner.Clone(details);
        Events = UnifiedEntryContractCloner.ReadOnly(
            events.Select(value => value with { }));
        PersistenceRevision = persistenceRevision;
    }
    #endregion

    /// <summary>
    /// 获取统一入口会话记录。
    /// </summary>
    public ConversationRecord Conversation { get; }

    /// <summary>
    /// 获取会话消息记录。
    /// </summary>
    public IReadOnlyList<ConversationMessageRecord> Messages { get; }

    /// <summary>
    /// 获取统一入口运行明细。
    /// </summary>
    public UnifiedRunDetails Details { get; }

    /// <summary>
    /// 获取统一入口运行事件。
    /// </summary>
    public IReadOnlyList<UnifiedRunEventRecord> Events { get; }

    /// <summary>
    /// 获取聚合持久化修订号。
    /// </summary>
    public long PersistenceRevision { get; }

    #region 处理（WithDetails）
    /// <summary>
    /// 处理（WithDetails）
    /// </summary>
    /// <param name="details">统一入口运行及其关联执行的明细。</param>
    /// <returns>替换运行详情、保留会话消息、事件及持久化版本的聚合副本。</returns>
    public UnifiedEntryAggregate WithDetails(UnifiedRunDetails details) =>
        new(Conversation, Messages, details, Events, PersistenceRevision);
    #endregion

    #region 处理（WithMessage）
    /// <summary>
    /// 处理（WithMessage）
    /// </summary>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>在现有消息末尾追加指定消息后的聚合副本。</returns>
    public UnifiedEntryAggregate WithMessage(ConversationMessageRecord message) =>
        new(
            Conversation,
            Messages.Append(message).ToArray(),
            Details,
            Events,
            PersistenceRevision);
    #endregion

    #region 处理（WithEvent）
    /// <summary>
    /// 处理（WithEvent）
    /// </summary>
    /// <param name="value">本次操作使用的统一运行事件。</param>
    /// <returns>在现有事件末尾追加指定事件后的聚合副本。</returns>
    public UnifiedEntryAggregate WithEvent(UnifiedRunEventRecord value) =>
        new(
            Conversation,
            Messages,
            Details,
            Events.Append(value).ToArray(),
            PersistenceRevision);
    #endregion

    #region 处理（WithPersistenceRevision）
    /// <summary>
    /// 处理（WithPersistenceRevision）
    /// </summary>
    /// <param name="value">聚合使用的持久化版本号。</param>
    /// <returns>使用指定持久化版本、保留其余内容的聚合副本。</returns>
    public UnifiedEntryAggregate WithPersistenceRevision(long value) =>
        new(Conversation, Messages, Details, Events, value);
    #endregion
}

/// <summary>
/// 定义统一入口查询的默认值和上限。
/// </summary>
public static class UnifiedEntryReadLimits
{
    /// <summary>会话消息查询的默认返回数量。</summary>
    public const int DefaultMessageTake = 100;
    /// <summary>会话消息查询允许的最大返回数量。</summary>
    public const int MaximumMessageTake = 500;
}

/// <summary>
/// 定义统一入口会话、运行和事件的存储边界。
/// </summary>
public interface IUnifiedEntryRepository
{
    #region 获取统一入口会话。
    /// <summary>获取统一入口会话。</summary>
    /// <param name="id">会话标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的会话记录；不存在时为 null。</returns>
    Task<ConversationRecord?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 查询统一入口会话列表。
    /// <summary>查询统一入口会话列表。</summary>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>受读取数量限制的会话记录集合。</returns>
    Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(int take, CancellationToken cancellationToken = default);
    #endregion

    #region 查询统一入口会话消息。
    /// <summary>查询统一入口会话消息。</summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定会话受读取数量限制的消息集合。</returns>
    Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesAsync(
        Guid conversationId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default);
    #endregion

    #region 获取统一入口运行记录。
    /// <summary>获取统一入口运行记录。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定统一入口运行记录；不存在时为 null。</returns>
    Task<UnifiedEntryRunRecord?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询统一入口运行列表。
    /// <summary>查询统一入口运行列表。</summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定会话受读取数量限制的入口运行集合。</returns>
    Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsAsync(Guid conversationId, int take, CancellationToken cancellationToken = default);
    #endregion

    #region 获取统一入口记录详情。
    /// <summary>获取统一入口记录详情。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定入口运行及其子运行详情；不存在时为 null。</returns>
    Task<UnifiedRunDetails?> GetDetailsAsync(Guid runId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询统一入口记录事件列表。
    /// <summary>查询统一入口记录事件列表。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定入口运行的有序事件集合。</returns>
    Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsAsync(Guid runId, CancellationToken cancellationToken = default);
    #endregion

    #region 清理超过保留期限的业务查询结果。
    /// <summary>清理超过保留期限的业务查询结果。</summary>
    /// <param name="cutoffUtc">筛选截止时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>过期业务查询结果的清理统计；接口默认实现不清理数据，各计数均为零。</returns>
    Task<BusinessQueryCleanupResult> RedactExpiredBusinessQueryResultsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BusinessQueryCleanupResult(0, 0, 0, cutoffUtc));
    #endregion

    #region 按所有者边界获取统一入口会话。
    /// <summary>按所有者边界获取统一入口会话。</summary>
    /// <param name="id">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的会话；不存在或所属身份不匹配时为 null。</returns>
    Task<ConversationRecord?> GetConversationForOwnerAsync(Guid id, string tenantId, string userId, CancellationToken cancellationToken = default) =>
        GetOwnedConversationFallbackAsync(this, id, tenantId, userId, cancellationToken);
    #endregion

    #region 按所有者边界查询统一入口会话列表。
    /// <summary>按所有者边界查询统一入口会话列表。</summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的会话集合；默认兼容实现先限制读取数量再过滤身份，可能少于 take。</returns>
    Task<IReadOnlyList<ConversationRecord>> ListConversationsForOwnerAsync(
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default) =>
        ListOwnedConversationsFallbackAsync(this, tenantId, userId, take, cancellationToken);
    #endregion

    #region 按所有者边界查询会话消息。
    /// <summary>按所有者边界查询会话消息。</summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定用户会话的消息集合；会话不存在或所属身份不匹配时为空集合。</returns>
    Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default) =>
        ListOwnedMessagesFallbackAsync(
            this, conversationId, tenantId, userId, take, cancellationToken);
    #endregion

    #region 按所有者边界获取统一入口运行。
    /// <summary>按所有者边界获取统一入口运行。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的入口运行；不存在或所属身份不匹配时为 null。</returns>
    Task<UnifiedEntryRunRecord?> GetRunForOwnerAsync(Guid runId, string tenantId, string userId, CancellationToken cancellationToken = default) =>
        GetOwnedRunFallbackAsync(this, runId, tenantId, userId, cancellationToken);
    #endregion

    #region 按所有者边界查询统一入口运行列表。
    /// <summary>按所有者边界查询统一入口运行列表。</summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定会话中属于该租户和用户的运行集合；默认兼容实现先限制读取数量再过滤身份。</returns>
    Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default) =>
        ListOwnedRunsFallbackAsync(
            this, conversationId, tenantId, userId, take, cancellationToken);
    #endregion

    #region 按所有者边界获取统一入口运行详情。
    /// <summary>按所有者边界获取统一入口运行详情。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的运行详情；不存在或所属身份不匹配时为 null。</returns>
    Task<UnifiedRunDetails?> GetDetailsForOwnerAsync(Guid runId, string tenantId, string userId, CancellationToken cancellationToken = default) =>
        GetOwnedDetailsFallbackAsync(this, runId, tenantId, userId, cancellationToken);
    #endregion

    #region 按所有者边界查询统一入口运行事件。
    /// <summary>按所有者边界查询统一入口运行事件。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的运行事件；运行不存在或所属身份不匹配时为空集合。</returns>
    Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        ListOwnedEventsFallbackAsync(this, runId, tenantId, userId, cancellationToken);
    #endregion

    #region 按所有者边界获取统一入口聚合记录。
    /// <summary>按所有者边界获取统一入口聚合记录。</summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>属于指定租户和用户的运行聚合；不存在时为 null，接口默认实现始终返回 null。</returns>
    Task<UnifiedEntryAggregate?> GetAggregateForOwnerAsync(Guid runId, string tenantId, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UnifiedEntryAggregate?>(null);
    #endregion

    #region 保存统一入口记录。
    /// <summary>保存统一入口记录。</summary>
    /// <param name="value">本次操作使用的统一入口运行聚合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功持久化后的聚合及其持久化版本；并发版本冲突由实现拒绝。</returns>
    Task<UnifiedEntryAggregate> SaveAsync(UnifiedEntryAggregate value, CancellationToken cancellationToken = default);
    #endregion

    #region 获取（GetOwnedConversationFallbackAsync）
    /// <summary>
    /// 获取（GetOwnedConversationFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="id">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>兼容查询后所属身份匹配的会话；不存在或身份不匹配时为 null。</returns>
    private static async Task<ConversationRecord?> GetOwnedConversationFallbackAsync(
        IUnifiedEntryRepository repository,
        Guid id,
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        ConversationRecord? value = await repository.GetConversationAsync(id, cancellationToken);
        return value is not null && Owned(value.TenantId, value.UserId, tenantId, userId)
            ? value
            : null;
    }
    #endregion

    #region 查询列表（ListOwnedConversationsFallbackAsync）
    /// <summary>
    /// 查询列表（ListOwnedConversationsFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>对受数量限制的会话查询结果再按租户和用户过滤的集合，可能少于 take。</returns>
    private static async Task<IReadOnlyList<ConversationRecord>>
        ListOwnedConversationsFallbackAsync(
            IUnifiedEntryRepository repository,
            string tenantId,
            string userId,
            int take,
            CancellationToken cancellationToken) =>
        (await repository.ListConversationsAsync(take, cancellationToken))
            .Where(value => Owned(value.TenantId, value.UserId, tenantId, userId))
            .ToArray();
    #endregion

    #region 查询列表（ListOwnedMessagesFallbackAsync）
    /// <summary>
    /// 查询列表（ListOwnedMessagesFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>通过会话所属身份检查后读取的消息；会话不可访问时为空集合。</returns>
    private static async Task<IReadOnlyList<ConversationMessageRecord>>
        ListOwnedMessagesFallbackAsync(
            IUnifiedEntryRepository repository,
            Guid conversationId,
            string tenantId,
            string userId,
            int take,
            CancellationToken cancellationToken) =>
        // 使用兼容路径查找调用方拥有的会话。
        await GetOwnedConversationFallbackAsync(
            repository, conversationId, tenantId, userId, cancellationToken) is null
                ? []
                : await repository.ListMessagesAsync(
                    conversationId, take, cancellationToken);
    #endregion

    #region 获取（GetOwnedRunFallbackAsync）
    /// <summary>
    /// 获取（GetOwnedRunFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>兼容查询后所属身份匹配的入口运行；不存在或身份不匹配时为 null。</returns>
    private static async Task<UnifiedEntryRunRecord?> GetOwnedRunFallbackAsync(
        IUnifiedEntryRepository repository,
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        UnifiedEntryRunRecord? value = await repository.GetRunAsync(runId, cancellationToken);
        return value is not null && Owned(value.TenantId, value.UserId, tenantId, userId)
            ? value
            : null;
    }
    #endregion

    #region 查询列表（ListOwnedRunsFallbackAsync）
    /// <summary>
    /// 查询列表（ListOwnedRunsFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>对受数量限制的会话运行结果再按租户和用户过滤的集合，可能少于 take。</returns>
    private static async Task<IReadOnlyList<UnifiedEntryRunRecord>>
        ListOwnedRunsFallbackAsync(
            IUnifiedEntryRepository repository,
            Guid conversationId,
            string tenantId,
            string userId,
            int take,
            CancellationToken cancellationToken) =>
        (await repository.ListRunsAsync(conversationId, take, cancellationToken))
            .Where(value => Owned(value.TenantId, value.UserId, tenantId, userId))
            .ToArray();
    #endregion

    #region 获取（GetOwnedDetailsFallbackAsync）
    /// <summary>
    /// 获取（GetOwnedDetailsFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>通过运行所属身份检查后读取的运行详情；运行不可访问时为 null。</returns>
    private static async Task<UnifiedRunDetails?> GetOwnedDetailsFallbackAsync(
        IUnifiedEntryRepository repository,
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken) =>
        // 使用兼容路径查找调用方拥有的运行。
        await GetOwnedRunFallbackAsync(repository, runId, tenantId, userId, cancellationToken)
            is null
                ? null
                : await repository.GetDetailsAsync(runId, cancellationToken);
    #endregion

    #region 查询列表（ListOwnedEventsFallbackAsync）
    /// <summary>
    /// 查询列表（ListOwnedEventsFallbackAsync）
    /// </summary>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>通过运行所属身份检查后读取的事件集合；运行不可访问时为空集合。</returns>
    private static async Task<IReadOnlyList<UnifiedRunEventRecord>>
        ListOwnedEventsFallbackAsync(
            IUnifiedEntryRepository repository,
            Guid runId,
            string tenantId,
            string userId,
            CancellationToken cancellationToken) =>
        // 使用兼容路径查找调用方拥有的运行。
        await GetOwnedRunFallbackAsync(repository, runId, tenantId, userId, cancellationToken)
            is null
                ? []
                : await repository.ListEventsAsync(runId, cancellationToken);
    #endregion

    #region 核对记录的租户及用户归属（Owned）
    /// <summary>
    /// 核对记录的租户及用户归属（Owned）。
    /// </summary>
    /// <param name="storedTenant">记录中保存的租户标识。</param>
    /// <param name="storedUser">记录中保存的用户标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <returns>记录中的租户和用户标识均与请求值按区分大小写的方式匹配时返回 true，否则返回 false。</returns>
    private static bool Owned(string storedTenant, string storedUser, string tenantId, string userId) =>
        string.Equals(storedTenant, tenantId, StringComparison.Ordinal)
        && string.Equals(storedUser, userId, StringComparison.Ordinal);
    #endregion
}

/// <summary>
/// 定义统一入口运行的恢复和补偿能力。
/// </summary>
public interface IUnifiedEntryRecovery
{
    #region 恢复或终结中断的统一入口运行。
    /// <summary>恢复或终结中断的统一入口运行。</summary>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>本次完成宿主中断恢复的入口运行数量。</returns>
    Task<int> RecoverInterruptedAsync(DateTimeOffset recoveredAtUtc, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 提供统一入口契约对象的防御性复制。
/// </summary>
public static class UnifiedEntryContractCloner
{
    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行聚合。</param>
    /// <returns>通过聚合构造函数复制会话、消息、详情和事件并保留持久化版本的聚合副本。</returns>
    public static UnifiedEntryAggregate Clone(UnifiedEntryAggregate value) =>
        new(
            value.Conversation,
            value.Messages,
            value.Details,
            value.Events,
            value.PersistenceRevision);
    #endregion

    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行详情。</param>
    /// <returns>通过详情构造函数复制入口及各类子运行记录的详情副本。</returns>
    public static UnifiedRunDetails Clone(UnifiedRunDetails value) =>
        new(value.EntryRun, value.AgentRuns, value.Orchestrations, value.ToolCalls);
    #endregion

    #region 读取（ReadOnly）
    /// <summary>
    /// 读取（ReadOnly）
    /// </summary>
    /// <param name="values">按原顺序枚举并复制为只读集合的源数据。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>按枚举顺序复制到新数组并包装为只读的集合；元素本身不作深复制。</returns>
    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
    #endregion
}

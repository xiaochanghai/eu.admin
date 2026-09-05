#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.Orchestration;

/// <summary>
/// 编排定义的可用状态。
/// </summary>
public enum OrchestrationStatus
{
    /// <summary>已启用。</summary>
    Enabled,
    /// <summary>已停用。</summary>
    Disabled,
    /// <summary>已归档。</summary>
    Archived
}

/// <summary>
/// 编排节点获取输入内容的方式。
/// </summary>
public enum OrchestrationNodeInputMode
{
    /// <summary>使用编排运行的初始输入。</summary>
    InitialInput,
    /// <summary>使用前一个节点的输出。</summary>
    PreviousOutput,
    /// <summary>根据输入模板生成节点输入。</summary>
    Template
}

/// <summary>
/// 编排边被选择执行的条件。
/// </summary>
public enum OrchestrationEdgeCondition
{
    /// <summary>始终允许流转。</summary>
    Always,
    /// <summary>来源节点成功时流转。</summary>
    Succeeded,
    /// <summary>来源节点失败时流转。</summary>
    Failed,
    /// <summary>来源节点输出包含指定内容时流转。</summary>
    OutputContains
}

/// <summary>
/// 编排运行的状态。
/// </summary>
public enum OrchestrationRunStatus
{
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>已完成。</summary>
    Completed,
    /// <summary>运行失败。</summary>
    Failed,
    /// <summary>已取消。</summary>
    Cancelled
}

/// <summary>
/// 编排节点运行的状态。
/// </summary>
public enum OrchestrationNodeRunStatus
{
    /// <summary>等待执行。</summary>
    Pending,
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>已完成。</summary>
    Completed,
    /// <summary>运行失败。</summary>
    Failed,
    /// <summary>已取消。</summary>
    Cancelled
}

/// <summary>
/// 编排进入终态时对待执行节点采用的处理策略。
/// </summary>
public enum OrchestrationTerminalTransitionPolicy
{
    /// <summary>保留待执行节点的原状态。</summary>
    PreservePending,
    /// <summary>将待执行节点一并转换为终态。</summary>
    TerminalizePending
}

/// <summary>
/// 编排中的节点定义。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Name">显示名称。</param>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="InputMode">节点输入内容的来源方式。</param>
/// <param name="InputTemplate">生成节点输入使用的模板。</param>
/// <param name="MaximumRetries">节点失败后的最大重试次数。</param>
/// <param name="TimeoutSeconds">执行超时时间，单位为秒。</param>
public sealed record OrchestrationNode(
    string Id,
    string Name,
    Guid AgentId,
    OrchestrationNodeInputMode InputMode,
    string InputTemplate,
    int MaximumRetries,
    int TimeoutSeconds);

/// <summary>
/// 编排节点之间的有向连接。
/// </summary>
/// <param name="FromNodeId">来源节点标识。</param>
/// <param name="ToNodeId">目标节点标识。</param>
/// <param name="Condition">选择该连接的条件。</param>
/// <param name="ConditionValue">连接条件使用的匹配值。</param>
/// <param name="Order">同级连接的判断顺序。</param>
public sealed record OrchestrationEdge(
    string FromNodeId,
    string ToNodeId,
    OrchestrationEdgeCondition Condition,
    string ConditionValue,
    int Order);

/// <summary>
/// 编排引用的 Agent 版本绑定。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
public sealed record OrchestrationAgentBinding(Guid AgentId, Guid AgentVersionId);

/// <summary>
/// 运行时可用的已发布编排引用。
/// </summary>
/// <param name="OrchestrationId">编排定义标识。</param>
/// <param name="OrchestrationVersionId">编排版本标识。</param>
/// <param name="Enabled">是否启用。</param>
public sealed record PublishedOrchestrationReference(
    Guid OrchestrationId, Guid OrchestrationVersionId, bool Enabled);

/// <summary>
/// 定义运行时可用的已发布编排目录。
/// </summary>
public interface IPublishedOrchestrationCatalog
{
    /// <summary>查询已发布编排列表。</summary>
    Task<IReadOnlyList<PublishedOrchestrationReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 已发布编排版本的不可变快照。
/// </summary>
/// <param name="VersionId">版本标识。</param>
/// <param name="OrchestrationCode">编排业务编码。</param>
/// <param name="StartNodeId">编排入口节点标识。</param>
/// <param name="Nodes">编排节点或节点运行记录集合。</param>
/// <param name="Edges">编排节点连接集合。</param>
/// <param name="Agents">编排引用的 Agent 版本集合。</param>
public sealed record OrchestrationVersionSnapshot(
    Guid VersionId,
    string OrchestrationCode,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges,
    IReadOnlyList<OrchestrationAgentBinding> Agents);

/// <summary>
/// 编排版本信息。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Label">版本标签。</param>
/// <param name="IsDraft">是否为草稿版本。</param>
/// <param name="StartNodeId">编排入口节点标识。</param>
/// <param name="Nodes">编排节点或节点运行记录集合。</param>
/// <param name="Edges">编排节点连接集合。</param>
/// <param name="Snapshot">执行或发布使用的不可变版本快照。</param>
public sealed record OrchestrationVersion(
    Guid Id,
    string Label,
    bool IsDraft,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges,
    OrchestrationVersionSnapshot? Snapshot);

/// <summary>
/// 编排定义及其版本集合。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="Draft">当前草稿版本。</param>
/// <param name="PublishedVersions">已发布版本集合。</param>
public sealed record OrchestrationDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    OrchestrationStatus Status,
    long LogicalRevision,
    OrchestrationVersion Draft,
    IReadOnlyList<OrchestrationVersion> PublishedVersions);

/// <summary>
/// 编排定义列表项。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="DraftNodeCount">草稿中的节点数量。</param>
/// <param name="CurrentPublishedLabel">当前发布版本的标签。</param>
public sealed record OrchestrationListItem(
    Guid Id, string Code, string Name, string Description, OrchestrationStatus Status,
    long LogicalRevision, int DraftNodeCount, string? CurrentPublishedLabel);

/// <summary>
/// 创建编排定义的命令。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
public sealed record CreateOrchestrationCommand(string Code, string Name, string Description);

/// <summary>
/// 保存编排草稿的命令。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartNodeId">编排入口节点标识。</param>
/// <param name="Nodes">编排节点或节点运行记录集合。</param>
/// <param name="Edges">编排节点连接集合。</param>
public sealed record SaveOrchestrationDraftCommand(
    Guid Id,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    OrchestrationStatus Status,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges);

/// <summary>
/// 发布编排版本的命令。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record PublishOrchestrationCommand(Guid Id, long ExpectedLogicalRevision);

/// <summary>
/// 设置编排归档状态的命令。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetOrchestrationArchiveCommand(
    Guid Id,
    long ExpectedLogicalRevision,
    bool Archived);

/// <summary>
/// 编排节点的运行记录。
/// </summary>
/// <param name="NodeId">编排节点标识。</param>
/// <param name="NodeName">编排节点名称。</param>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="Attempts">执行尝试次数或尝试明细集合。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="OutputCharacters">输出内容的字符数量。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
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

/// <summary>
/// 编排运行记录。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="OrchestrationId">编排定义标识。</param>
/// <param name="OrchestrationVersionId">编排版本标识。</param>
/// <param name="OrchestrationCode">编排业务编码。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="Nodes">编排节点或节点运行记录集合。</param>
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

/// <summary>
/// 定义编排领域错误码。
/// </summary>
public static class OrchestrationErrorCodes
{
    /// <summary>表示 <c>NotFound</c> 场景的错误码。</summary>
    public const string NotFound = "ORCHESTRATION_NOT_FOUND";
    /// <summary>表示 <c>CodeInvalid</c> 场景的错误码。</summary>
    public const string CodeInvalid = "ORCHESTRATION_CODE_INVALID";
    /// <summary>表示 <c>CodeConflict</c> 场景的错误码。</summary>
    public const string CodeConflict = "ORCHESTRATION_CODE_CONFLICT";
    /// <summary>表示 <c>RowVersionConflict</c> 场景的错误码。</summary>
    public const string RowVersionConflict = "ORCHESTRATION_ROW_VERSION_CONFLICT";
    /// <summary>表示 <c>DefinitionInvalid</c> 场景的错误码。</summary>
    public const string DefinitionInvalid = "ORCHESTRATION_DEFINITION_INVALID";
    /// <summary>表示 <c>VersionMissing</c> 场景的错误码。</summary>
    public const string VersionMissing = "ORCHESTRATION_PUBLISHED_VERSION_MISSING";
    /// <summary>表示 <c>Disabled</c> 场景的错误码。</summary>
    public const string Disabled = "ORCHESTRATION_DISABLED";
    /// <summary>表示 <c>AgentUnavailable</c> 场景的错误码。</summary>
    public const string AgentUnavailable = "ORCHESTRATION_AGENT_VERSION_UNAVAILABLE";
    /// <summary>表示 <c>RunNotFound</c> 场景的错误码。</summary>
    public const string RunNotFound = "ORCHESTRATION_RUN_NOT_FOUND";
    /// <summary>表示 <c>RunInputInvalid</c> 场景的错误码。</summary>
    public const string RunInputInvalid = "ORCHESTRATION_RUN_INPUT_INVALID";
    /// <summary>表示 <c>PayloadLimitExceeded</c> 场景的错误码。</summary>
    public const string PayloadLimitExceeded = "ORCHESTRATION_PAYLOAD_LIMIT_EXCEEDED";
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景的错误码。</summary>
    public const string LifecycleTransitionInvalid = "ORCHESTRATION_LIFECYCLE_TRANSITION_INVALID";
    /// <summary>表示 <c>ArchiveBlocked</c> 场景的错误码。</summary>
    public const string ArchiveBlocked = "ORCHESTRATION_ARCHIVE_BLOCKED";
}

/// <summary>
/// 将编排领域错误映射为服务状态码。
/// </summary>
public static class OrchestrationServiceStatusCodes
{
    /// <summary>表示 <c>NotFound</c> 场景映射的服务状态码。</summary>
    public const int NotFound = 650001;
    /// <summary>表示 <c>CodeInvalid</c> 场景映射的服务状态码。</summary>
    public const int CodeInvalid = 650002;
    /// <summary>表示 <c>CodeConflict</c> 场景映射的服务状态码。</summary>
    public const int CodeConflict = 650003;
    /// <summary>表示 <c>RowVersionConflict</c> 场景映射的服务状态码。</summary>
    public const int RowVersionConflict = 650004;
    /// <summary>表示 <c>DefinitionInvalid</c> 场景映射的服务状态码。</summary>
    public const int DefinitionInvalid = 650005;
    /// <summary>表示 <c>VersionMissing</c> 场景映射的服务状态码。</summary>
    public const int VersionMissing = 650006;
    /// <summary>表示 <c>Disabled</c> 场景映射的服务状态码。</summary>
    public const int Disabled = 650007;
    /// <summary>表示 <c>AgentUnavailable</c> 场景映射的服务状态码。</summary>
    public const int AgentUnavailable = 650008;
    /// <summary>表示 <c>RunNotFound</c> 场景映射的服务状态码。</summary>
    public const int RunNotFound = 650009;
    /// <summary>表示 <c>RunInputInvalid</c> 场景映射的服务状态码。</summary>
    public const int RunInputInvalid = 650010;
    /// <summary>表示 <c>PayloadLimitExceeded</c> 场景映射的服务状态码。</summary>
    public const int PayloadLimitExceeded = 650011;
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景映射的服务状态码。</summary>
    public const int LifecycleTransitionInvalid = 650012;
    /// <summary>表示 <c>ArchiveBlocked</c> 场景映射的服务状态码。</summary>
    public const int ArchiveBlocked = 650013;

    public static int FromErrorCode(string code) => code switch
    {
        OrchestrationErrorCodes.NotFound => NotFound,
        OrchestrationErrorCodes.CodeInvalid => CodeInvalid,
        OrchestrationErrorCodes.CodeConflict => CodeConflict,
        OrchestrationErrorCodes.RowVersionConflict => RowVersionConflict,
        OrchestrationErrorCodes.DefinitionInvalid => DefinitionInvalid,
        OrchestrationErrorCodes.VersionMissing => VersionMissing,
        OrchestrationErrorCodes.Disabled => Disabled,
        OrchestrationErrorCodes.AgentUnavailable => AgentUnavailable,
        OrchestrationErrorCodes.RunNotFound => RunNotFound,
        OrchestrationErrorCodes.RunInputInvalid => RunInputInvalid,
        OrchestrationErrorCodes.PayloadLimitExceeded => PayloadLimitExceeded,
        OrchestrationErrorCodes.LifecycleTransitionInvalid => LifecycleTransitionInvalid,
        OrchestrationErrorCodes.ArchiveBlocked => ArchiveBlocked,
        _ => 500
    };

    public static string ToErrorCode(int status) => status switch
    {
        NotFound => OrchestrationErrorCodes.NotFound,
        CodeInvalid => OrchestrationErrorCodes.CodeInvalid,
        CodeConflict => OrchestrationErrorCodes.CodeConflict,
        RowVersionConflict => OrchestrationErrorCodes.RowVersionConflict,
        DefinitionInvalid => OrchestrationErrorCodes.DefinitionInvalid,
        VersionMissing => OrchestrationErrorCodes.VersionMissing,
        Disabled => OrchestrationErrorCodes.Disabled,
        AgentUnavailable => OrchestrationErrorCodes.AgentUnavailable,
        RunNotFound => OrchestrationErrorCodes.RunNotFound,
        RunInputInvalid => OrchestrationErrorCodes.RunInputInvalid,
        PayloadLimitExceeded => OrchestrationErrorCodes.PayloadLimitExceeded,
        LifecycleTransitionInvalid => OrchestrationErrorCodes.LifecycleTransitionInvalid,
        ArchiveBlocked => OrchestrationErrorCodes.ArchiveBlocked,
        _ => "INTERNAL_ERROR"
    };
}

/// <summary>
/// 定义编排定义及版本的存储边界。
/// </summary>
public interface IOrchestrationRepository
{
    /// <summary>按标识获取编排定义。</summary>
    Task<OrchestrationDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>查询编排定义列表。</summary>
    Task<IReadOnlyList<OrchestrationDefinition>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>尝试创建编排定义。</summary>
    Task<bool> TryCreateAsync(OrchestrationDefinition value, CancellationToken cancellationToken = default);
    /// <summary>按并发条件尝试替换编排定义。</summary>
    Task<bool> TryReplaceAsync(OrchestrationDefinition value, long expectedRevision, CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义编排运行记录的存储和状态转换边界。
/// </summary>
public interface IOrchestrationRunRepository
{
    /// <summary>保存编排运行记录。</summary>
    Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default);
    /// <summary>获取编排运行记录。</summary>
    Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>查询编排运行记录列表。</summary>
    Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
        Guid orchestrationId, int take, CancellationToken cancellationToken = default);
    /// <summary>保存编排运行执行详情。</summary>
    Task SaveDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default);
    /// <summary>获取编排运行记录详情。</summary>
    Task<OrchestrationRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
    /// <summary>尝试保存仍在运行的编排详情。</summary>
    Task<bool> TrySaveRunningDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default);
    /// <summary>尝试终结仍处于运行状态的编排。</summary>
    Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken = default);
    /// <summary>恢复或终结中断的编排运行记录。</summary>
    Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        string errorCode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 编排运行状态转换的结果。
/// </summary>
/// <param name="Run">状态转换后的编排运行记录。</param>
/// <param name="Transitioned">是否成功完成状态转换。</param>
public sealed record OrchestrationRunTransitionResult(
    OrchestrationRunRecord? Run,
    bool Transitioned);

/// <summary>
/// 提供编排契约对象的防御性复制。
/// </summary>
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

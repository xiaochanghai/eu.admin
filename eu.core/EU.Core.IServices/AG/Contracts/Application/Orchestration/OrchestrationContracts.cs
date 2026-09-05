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
    #region 列出未归档编排的已发布版本引用（ListPublishedAsync）
    /// <summary>
    /// 列出未归档编排的已发布版本引用（ListPublishedAsync）。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除、未归档定义下的非草稿版本引用；引用中同时标记编排是否启用，禁用版本不会仅因此被排除；无记录时为空集合。</returns>
    Task<IReadOnlyList<PublishedOrchestrationReference>> ListPublishedAsync(CancellationToken cancellationToken = default);
    #endregion
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

    #region 将编排错误码转换为服务状态码（FromErrorCode）
    /// <summary>
    /// 将编排错误码转换为服务状态码（FromErrorCode）。
    /// </summary>
    /// <param name="code">编排领域错误码。</param>
    /// <returns>返回已知编排错误对应的整数服务状态码；未知错误返回 500。</returns>
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
    #endregion

    #region 将服务状态码转换为编排错误码（ToErrorCode）
    /// <summary>
    /// 将服务状态码转换为编排错误码（ToErrorCode）。
    /// </summary>
    /// <param name="status">服务结果中的整数状态码，不是 HTTP 状态枚举。</param>
    /// <returns>返回已知服务状态码对应的编排领域错误码；未映射的状态返回 INTERNAL_ERROR。</returns>
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
    #endregion
}

/// <summary>
/// 定义编排定义及版本的存储边界。
/// </summary>
public interface IOrchestrationRepository
{
    #region 读取编排定义及版本（GetByIdAsync）
    /// <summary>
    /// 读取编排定义及版本（GetByIdAsync）。
    /// </summary>
    /// <param name="id">编排定义标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的编排定义及其草稿、发布版本；记录不存在时为 null。</returns>
    Task<OrchestrationDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion
    #region 列出编排定义及版本（ListAsync）
    /// <summary>
    /// 列出编排定义及版本（ListAsync）。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的编排定义及版本集合，包含归档定义；无记录时为空集合。</returns>
    Task<IReadOnlyList<OrchestrationDefinition>> ListAsync(CancellationToken cancellationToken = default);
    #endregion
    #region 创建编排定义及草稿和发布版本（TryCreateAsync）
    /// <summary>
    /// 创建编排定义及草稿和发布版本（TryCreateAsync）。
    /// </summary>
    /// <param name="value">待创建的编排定义，包含草稿及已发布版本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>编排定义及版本持久化成功时返回 true；存在相同标识或编码的未删除定义时返回 false。</returns>
    Task<bool> TryCreateAsync(OrchestrationDefinition value, CancellationToken cancellationToken = default);
    #endregion
    #region 按修订号更新编排定义并保留发布历史（TryReplaceAsync）
    /// <summary>
    /// 按修订号更新编排定义并保留发布历史（TryReplaceAsync）。
    /// </summary>
    /// <param name="value">替换后的定义；修订号须递增一，保留原草稿标识及已有发布版本标识，已有发布版本内容不会被覆盖。</param>
    /// <param name="expectedRevision">数据库当前应具有的逻辑修订号，不允许为 long.MaxValue。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>定义、草稿及新增发布版本保存成功时返回 true；修订号、编码或草稿标识不匹配，发布版本被移除或重复，或条件更新未生效时返回 false。</returns>
    Task<bool> TryReplaceAsync(OrchestrationDefinition value, long expectedRevision, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义编排运行记录的存储和状态转换边界。
/// </summary>
public interface IOrchestrationRunRepository
{
    #region 保存编排运行记录。
    /// <summary>保存编排运行记录。</summary>
    /// <param name="value">本次操作使用的编排运行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default);
    #endregion
    #region 读取编排运行及节点记录（GetAsync）
    /// <summary>
    /// 读取编排运行及节点记录（GetAsync）。
    /// </summary>
    /// <param name="id">编排运行标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的编排运行及其节点记录；不存在时为 null。</returns>
    Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion
    #region 按时间倒序列出编排运行（ListAsync）
    /// <summary>
    /// 按时间倒序列出编排运行（ListAsync）。
    /// </summary>
    /// <param name="orchestrationId">编排定义标识。</param>
    /// <param name="take">期望返回的记录数，持久化实现将其限制在 1 至 100 之间。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回指定编排下未删除的运行记录，按开始时间倒序排列；无记录时为空集合。</returns>
    Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(Guid orchestrationId, int take, CancellationToken cancellationToken = default);
    #endregion
    #region 保存编排运行执行详情。
    /// <summary>保存编排运行执行详情。</summary>
    /// <param name="value">本次操作使用的编排运行及节点尝试详情。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task SaveDetailsAsync(OrchestrationRunDetails value, CancellationToken cancellationToken = default);
    #endregion
    #region 读取编排运行详情（GetDetailsAsync）
    /// <summary>
    /// 读取编排运行详情（GetDetailsAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回未删除的运行详情及尝试、工具调用记录；详情不存在时为 null。</returns>
    Task<OrchestrationRunDetails?> GetDetailsAsync(Guid runId, CancellationToken cancellationToken = default);
    #endregion
    #region 仅为运行中的编排保存详情（TrySaveRunningDetailsAsync）
    /// <summary>
    /// 仅为运行中的编排保存详情（TrySaveRunningDetailsAsync）。
    /// </summary>
    /// <param name="value">待保存的编排运行详情，RunId 用于定位运行记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>存在未删除且状态为 Running 的运行记录并成功写入详情时返回 true；记录不存在或不再运行时返回 false。</returns>
    Task<bool> TrySaveRunningDetailsAsync(OrchestrationRunDetails value, CancellationToken cancellationToken = default);
    #endregion
    #region 原子地终结编排运行及相关执行记录（TryFinalizeRunningAsync）
    /// <summary>
    /// 原子地终结编排运行及相关执行记录（TryFinalizeRunningAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="runStatus">拟保存的编排终态，只允许受支持的结束状态。</param>
    /// <param name="nodeStatus">需要终结的节点及尝试记录使用的终态。</param>
    /// <param name="transitionPolicy">决定是否同时终结 Pending 节点和尝试。</param>
    /// <param name="finishedAtUtc">完成时间（UTC）。</param>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="detailsIfMissing">运行详情缺失时补写的详情，非 null 时 RunId 必须与目标运行一致。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回转换后的运行记录和 Transitioned 标记；实际完成转换时标记为 true，原记录已非 Running 时返回原记录及 false，记录不存在时 Run 为 null 且标记为 false。</returns>
    Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken = default);
    #endregion
    #region 将中断的编排运行终结为失败（RecoverInterruptedAsync）
    /// <summary>
    /// 将中断的编排运行终结为失败（RecoverInterruptedAsync）。
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回失败转换后的运行记录及 Transitioned 标记；原记录已非 Running 时不转换，记录不存在时 Run 为 null；不重新执行编排。</returns>
    Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        string errorCode,
        CancellationToken cancellationToken = default);
    #endregion
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
    #region 复制编排定义及嵌套集合（Clone）
    /// <summary>
    /// 复制编排定义及嵌套集合（Clone）。
    /// </summary>
    /// <param name="value">需要防御性复制的编排定义。</param>
    /// <returns>草稿和发布版本均经复制、发布版本列表重新物化为只读集合的定义副本。</returns>
    public static OrchestrationDefinition Clone(OrchestrationDefinition value) =>
        value with
        {
            Draft = Clone(value.Draft),
            PublishedVersions = ReadOnly(value.PublishedVersions.Select(Clone))
        };
    #endregion

    #region 复制编排版本及嵌套集合（Clone）
    /// <summary>
    /// 复制编排版本及嵌套集合（Clone）。
    /// </summary>
    /// <param name="value">需要防御性复制的编排版本。</param>
    /// <returns>节点、连线及其只读集合均经复制的版本副本；存在快照时复制快照，原快照为 null 时仍为 null。</returns>
    public static OrchestrationVersion Clone(OrchestrationVersion value) =>
        value with
        {
            Nodes = ReadOnly(value.Nodes.Select(node => node with { })),
            Edges = ReadOnly(value.Edges.Select(edge => edge with { })),
            Snapshot = value.Snapshot is null ? null : Clone(value.Snapshot)
        };
    #endregion

    #region 复制编排版本快照及嵌套集合（Clone）
    /// <summary>
    /// 复制编排版本快照及嵌套集合（Clone）。
    /// </summary>
    /// <param name="value">需要防御性复制的编排版本快照。</param>
    /// <returns>节点、连线、Agent 绑定均创建记录副本并装入新只读集合的快照副本。</returns>
    public static OrchestrationVersionSnapshot Clone(OrchestrationVersionSnapshot value) =>
        value with
        {
            Nodes = ReadOnly(value.Nodes.Select(node => node with { })),
            Edges = ReadOnly(value.Edges.Select(edge => edge with { })),
            Agents = ReadOnly(value.Agents.Select(agent => agent with { }))
        };
    #endregion

    #region 复制编排运行记录及嵌套集合（Clone）
    /// <summary>
    /// 复制编排运行记录及嵌套集合（Clone）。
    /// </summary>
    /// <param name="value">需要防御性复制的编排运行记录。</param>
    /// <returns>各节点创建记录副本并装入新只读集合的运行记录副本。</returns>
    public static OrchestrationRunRecord Clone(OrchestrationRunRecord value) =>
        value with { Nodes = ReadOnly(value.Nodes.Select(node => node with { })) };
    #endregion

    #region 复制编排运行详情及嵌套集合（Clone）
    /// <summary>
    /// 复制编排运行详情及嵌套集合（Clone）。
    /// </summary>
    /// <param name="value">需要防御性复制的编排运行详情。</param>
    /// <returns>尝试记录及各尝试下的工具调用均创建记录副本，并使用新只读集合的详情副本。</returns>
    public static OrchestrationRunDetails Clone(OrchestrationRunDetails value) =>
        value with
        {
            Attempts = ReadOnly(value.Attempts.Select(attempt => attempt with
            {
                ToolCalls = ReadOnly(attempt.ToolCalls.Select(tool => tool with { }))
            }))
        };
    #endregion

    #region 将序列物化为只读列表（ReadOnly）
    /// <summary>
    /// 将序列物化为只读列表（ReadOnly）。
    /// </summary>
    /// <param name="values">需要立即枚举并物化的源序列。</param>
    /// <typeparam name="T">源序列的元素类型；引用类型元素仍与源序列共享对象。</typeparam>
    /// <returns>返回由新数组承载的只读列表，保留枚举顺序；只复制集合，不复制其中的元素对象。</returns>
    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
    #endregion
}

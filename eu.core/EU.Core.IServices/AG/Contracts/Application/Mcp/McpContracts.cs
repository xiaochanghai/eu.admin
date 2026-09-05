#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.Mcp;

/// <summary>
/// MCP 服务使用的传输方式。
/// </summary>
public enum McpTransportKind
{
    /// <summary>可流式传输的 HTTP。</summary>
    StreamableHttp,
    /// <summary>服务器发送事件（SSE）。</summary>
    Sse,
    /// <summary>标准输入输出流。</summary>
    Stdio
}

/// <summary>
/// MCP 服务定义的当前状态。
/// </summary>
public enum McpServerStatus
{
    /// <summary>尚未完成工具同步。</summary>
    NotSynced,
    /// <summary>服务健康且可用。</summary>
    Healthy,
    /// <summary>服务健康检查异常。</summary>
    Unhealthy,
    /// <summary>服务已停用。</summary>
    Disabled,
    /// <summary>服务已归档。</summary>
    Archived
}

/// <summary>
/// MCP 工具调用的风险等级。
/// </summary>
public enum McpToolRisk
{
    /// <summary>风险尚未分类。</summary>
    Unknown,
    /// <summary>只读取数据，不产生业务变更。</summary>
    ReadOnly,
    /// <summary>可能修改数据或外部状态。</summary>
    Mutating,
    /// <summary>需要重点审批和审计的高风险操作。</summary>
    HighRisk
}

/// <summary>
/// 同步得到的 MCP 工具版本。
/// </summary>
/// <param name="Id">记录标识。</param>
/// <param name="ServerId">MCP 服务标识。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="InputSchemaJson">工具输入参数的 JSON Schema。</param>
/// <param name="Risk">工具风险等级。</param>
/// <param name="Sha256">内容的 SHA-256 摘要。</param>
/// <param name="DiscoveredAtUtc">工具被发现的 UTC 时间。</param>
public sealed record McpToolVersion(
    Guid Id,
    Guid ServerId,
    string Name,
    string Description,
    string InputSchemaJson,
    McpToolRisk Risk,
    string Sha256,
    DateTimeOffset DiscoveredAtUtc);

/// <summary>
/// MCP 服务定义及其工具快照。
/// </summary>
/// <param name="Id">记录标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="Status">当前状态。</param>
/// <param name="LastError">最近一次同步错误。</param>
/// <param name="LastSyncedAtUtc">最近完成同步的 UTC 时间。</param>
/// <param name="CurrentToolVersionIds">当前生效的工具版本标识集合。</param>
/// <param name="ToolVersions">已发现的工具版本集合。</param>
public sealed record McpServerDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string> Arguments,
    string CredentialAlias,
    bool Enabled,
    long LogicalRevision,
    McpServerStatus Status,
    string LastError,
    DateTimeOffset? LastSyncedAtUtc,
    IReadOnlyList<Guid> CurrentToolVersionIds,
    IReadOnlyList<McpToolVersion> ToolVersions);

/// <summary>
/// MCP 服务定义的查询条件。
/// </summary>
/// <param name="Search">按编码或名称筛选的搜索文本。</param>
/// <param name="Status">当前状态。</param>
public sealed record McpServerQuery(string? Search = null, McpServerStatus? Status = null);

/// <summary>
/// 从 MCP 服务发现的工具信息。
/// </summary>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="InputSchemaJson">工具输入参数的 JSON Schema。</param>
public sealed record DiscoveredMcpTool(
    string Name,
    string Description,
    string InputSchemaJson);

/// <summary>
/// Agent 运行时使用的已发布 MCP 工具引用。
/// </summary>
/// <param name="ServerId">MCP 服务标识。</param>
/// <param name="ServerCode">MCP 服务编码。</param>
/// <param name="ServerName">MCP 服务名称。</param>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="ToolName">工具名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="InputSchemaJson">工具输入参数的 JSON Schema。</param>
/// <param name="Risk">工具风险等级。</param>
/// <param name="Sha256">内容的 SHA-256 摘要。</param>
public sealed record PublishedMcpToolReference(
    Guid ServerId,
    string ServerCode,
    string ServerName,
    Guid ToolVersionId,
    string ToolName,
    string Description,
    string InputSchemaJson,
    McpToolRisk Risk,
    string Sha256);

/// <summary>
/// 创建 MCP 服务定义的命令。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
public sealed record CreateMcpServerCommand(
    string Code,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string>? Arguments,
    string CredentialAlias,
    bool Enabled);

/// <summary>
/// 更新 MCP 服务定义的命令。
/// </summary>
/// <param name="ServerId">MCP 服务标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
public sealed record UpdateMcpServerCommand(
    Guid ServerId,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string>? Arguments,
    string CredentialAlias,
    bool Enabled);

/// <summary>
/// 同步 MCP 服务工具的命令。
/// </summary>
/// <param name="ServerId">MCP 服务标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record SyncMcpServerCommand(Guid ServerId, long ExpectedLogicalRevision);

/// <summary>
/// 设置 MCP 工具风险等级的命令。
/// </summary>
/// <param name="ServerId">MCP 服务标识。</param>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Risk">工具风险等级。</param>
public sealed record ClassifyMcpToolCommand(
    Guid ServerId,
    Guid ToolVersionId,
    long ExpectedLogicalRevision,
    McpToolRisk Risk);

/// <summary>
/// 设置 MCP 服务归档状态的命令。
/// </summary>
/// <param name="ServerId">MCP 服务标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetMcpServerArchiveCommand(
    Guid ServerId,
    long ExpectedLogicalRevision,
    bool Archived);

/// <summary>
/// 定义 MCP 服务与工具领域错误码。
/// </summary>
public static class McpErrorCodes
{
    /// <summary>表示 <c>NotFound</c> 场景的错误码。</summary>
    public const string NotFound = "MCP_SERVER_NOT_FOUND";
    /// <summary>表示 <c>CodeInvalid</c> 场景的错误码。</summary>
    public const string CodeInvalid = "MCP_SERVER_CODE_INVALID";
    /// <summary>表示 <c>CodeConflict</c> 场景的错误码。</summary>
    public const string CodeConflict = "MCP_SERVER_CODE_CONFLICT";
    /// <summary>表示 <c>ConfigurationInvalid</c> 场景的错误码。</summary>
    public const string ConfigurationInvalid = "MCP_CONFIGURATION_INVALID";
    /// <summary>表示 <c>RevisionConflict</c> 场景的错误码。</summary>
    public const string RevisionConflict = "MCP_REVISION_CONFLICT";
    /// <summary>表示 <c>DiscoveryFailed</c> 场景的错误码。</summary>
    public const string DiscoveryFailed = "MCP_DISCOVERY_FAILED";
    /// <summary>表示 <c>ToolNotFound</c> 场景的错误码。</summary>
    public const string ToolNotFound = "MCP_TOOL_NOT_FOUND";
    /// <summary>表示 <c>RiskInvalid</c> 场景的错误码。</summary>
    public const string RiskInvalid = "MCP_TOOL_RISK_INVALID";
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景的错误码。</summary>
    public const string LifecycleTransitionInvalid = "MCP_LIFECYCLE_TRANSITION_INVALID";
    /// <summary>表示 <c>DisableBlocked</c> 场景的错误码。</summary>
    public const string DisableBlocked = "MCP_DISABLE_BLOCKED";
    /// <summary>表示 <c>ArchiveBlocked</c> 场景的错误码。</summary>
    public const string ArchiveBlocked = "MCP_ARCHIVE_BLOCKED";
}

/// <summary>
/// 将 MCP 领域错误映射为服务状态码。
/// </summary>
public static class McpServiceStatusCodes
{
    /// <summary>表示 <c>NotFound</c> 场景映射的服务状态码。</summary>
    public const int NotFound = 630001;
    /// <summary>表示 <c>CodeInvalid</c> 场景映射的服务状态码。</summary>
    public const int CodeInvalid = 630002;
    /// <summary>表示 <c>CodeConflict</c> 场景映射的服务状态码。</summary>
    public const int CodeConflict = 630003;
    /// <summary>表示 <c>ConfigurationInvalid</c> 场景映射的服务状态码。</summary>
    public const int ConfigurationInvalid = 630004;
    /// <summary>表示 <c>RevisionConflict</c> 场景映射的服务状态码。</summary>
    public const int RevisionConflict = 630005;
    /// <summary>表示 <c>DiscoveryFailed</c> 场景映射的服务状态码。</summary>
    public const int DiscoveryFailed = 630006;
    /// <summary>表示 <c>ToolNotFound</c> 场景映射的服务状态码。</summary>
    public const int ToolNotFound = 630007;
    /// <summary>表示 <c>RiskInvalid</c> 场景映射的服务状态码。</summary>
    public const int RiskInvalid = 630008;
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景映射的服务状态码。</summary>
    public const int LifecycleTransitionInvalid = 630009;
    /// <summary>表示 <c>DisableBlocked</c> 场景映射的服务状态码。</summary>
    public const int DisableBlocked = 630010;
    /// <summary>表示 <c>ArchiveBlocked</c> 场景映射的服务状态码。</summary>
    public const int ArchiveBlocked = 630011;

    #region 转换（FromErrorCode）
    /// <summary>
    /// 转换（FromErrorCode）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <returns>MCP 错误码对应的服务状态值；未知错误码抛出 ArgumentOutOfRangeException。</returns>
    public static int FromErrorCode(string errorCode) => errorCode switch
    {
        McpErrorCodes.NotFound => NotFound,
        McpErrorCodes.CodeInvalid => CodeInvalid,
        McpErrorCodes.CodeConflict => CodeConflict,
        McpErrorCodes.ConfigurationInvalid => ConfigurationInvalid,
        McpErrorCodes.RevisionConflict => RevisionConflict,
        McpErrorCodes.DiscoveryFailed => DiscoveryFailed,
        McpErrorCodes.ToolNotFound => ToolNotFound,
        McpErrorCodes.RiskInvalid => RiskInvalid,
        McpErrorCodes.LifecycleTransitionInvalid => LifecycleTransitionInvalid,
        McpErrorCodes.DisableBlocked => DisableBlocked,
        McpErrorCodes.ArchiveBlocked => ArchiveBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
    };
    #endregion

    #region 转换（ToErrorCode）
    /// <summary>
    /// 转换（ToErrorCode）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <returns>服务状态值对应的 MCP 错误码；未知状态抛出 ArgumentOutOfRangeException。</returns>
    public static string ToErrorCode(int status) => status switch
    {
        NotFound => McpErrorCodes.NotFound,
        CodeInvalid => McpErrorCodes.CodeInvalid,
        CodeConflict => McpErrorCodes.CodeConflict,
        ConfigurationInvalid => McpErrorCodes.ConfigurationInvalid,
        RevisionConflict => McpErrorCodes.RevisionConflict,
        DiscoveryFailed => McpErrorCodes.DiscoveryFailed,
        ToolNotFound => McpErrorCodes.ToolNotFound,
        RiskInvalid => McpErrorCodes.RiskInvalid,
        LifecycleTransitionInvalid => McpErrorCodes.LifecycleTransitionInvalid,
        DisableBlocked => McpErrorCodes.DisableBlocked,
        ArchiveBlocked => McpErrorCodes.ArchiveBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
    #endregion
}

/// <summary>
/// 定义 MCP 服务及工具版本的管理目录。
/// </summary>
public interface IMcpServerDefinitionCatalog
{
    #region 获取MCP 服务定义。
    /// <summary>获取MCP 服务定义。</summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的 MCP 服务定义及工具版本信息；不存在时为 null。</returns>
    Task<McpServerDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 查询MCP 服务定义列表。
    /// <summary>查询MCP 服务定义列表。</summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配查询条件的 MCP 服务定义集合；未指定状态时排除已归档服务。</returns>
    Task<IReadOnlyList<McpServerDefinition>> ListAsync(McpServerQuery query, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义从 MCP 服务发现工具的能力。
/// </summary>
public interface IMcpToolDiscovery
{
    #region 从 MCP 服务发现可用工具。
    /// <summary>从 MCP 服务发现可用工具。</summary>
    /// <param name="server">MCP 服务器定义。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>从指定 MCP 服务发现的工具名称、描述、输入 Schema 等元数据集合。</returns>
    Task<IReadOnlyList<DiscoveredMcpTool>> DiscoverAsync(McpServerDefinition server, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义 Agent 运行时可用的已发布 MCP 工具目录。
/// </summary>
public interface IPublishedMcpToolCatalog
{
    #region 查询可用的 MCP 工具版本是否存在（ExistsAsync）
    /// <summary>
    /// 查询可用的 MCP 工具版本是否存在（ExistsAsync）。
    /// </summary>
    /// <param name="toolVersionId">工具版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定工具版本及所属服务均未删除、服务未归档且工具风险不是 Unknown 时返回 true，否则返回 false。</returns>
    Task<bool> ExistsAsync(Guid toolVersionId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询已发布 MCP 工具列表。
    /// <summary>查询已发布 MCP 工具列表。</summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>可供 Agent 绑定的已发布 MCP 工具版本引用集合。</returns>
    Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 提供 MCP 契约对象的防御性复制。
/// </summary>
public static class McpContractCloner
{
    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <returns>复制启动参数、当前工具标识及工具版本记录后的 MCP 服务定义副本。</returns>
    public static McpServerDefinition Clone(McpServerDefinition definition) =>
        definition with
        {
            Arguments = ReadOnly(definition.Arguments),
            CurrentToolVersionIds = ReadOnly(definition.CurrentToolVersionIds),
            ToolVersions = ReadOnly(definition.ToolVersions.Select(version => version with { }))
        };
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

    #region 检查 MCP 工具版本历史是否保留（PreservesToolHistory）
    /// <summary>
    /// 检查 MCP 工具版本历史是否保留（PreservesToolHistory）。
    /// </summary>
    /// <param name="existing">包含原有工具版本历史的 MCP 服务定义。</param>
    /// <param name="replacement">待检查的替换服务定义。</param>
    /// <returns>替换列表不少于原列表，且原列表每个位置的版本都与替换列表对应版本相等时返回 true，否则返回 false；允许在末尾追加版本。</returns>
    public static bool PreservesToolHistory(McpServerDefinition existing, McpServerDefinition replacement)
    {
        if (replacement.ToolVersions.Count < existing.ToolVersions.Count)
        {
            return false;
        }

        return existing.ToolVersions
            .Select((version, index) => version == replacement.ToolVersions[index])
            .All(value => value);
    }
    #endregion
}

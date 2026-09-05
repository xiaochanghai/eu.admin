#nullable enable

namespace EU.Core.IServices.MainAgent;

/// <summary>
/// 当前主 Agent 的分配记录。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="UpdatedAtUtc">最近更新的 UTC 时间。</param>
public sealed record MainAgentAssignment(
    Guid AgentId,
    Guid AgentVersionId,
    long LogicalRevision,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// 定义主 Agent 分配记录的存储边界。
/// </summary>
public interface IMainAgentAssignmentRepository
{
    #region 获取主 Agent 分配记录。
    /// <summary>获取主 Agent 分配记录。</summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>当前固定版本的主 Agent 分配；尚未配置时为 null。</returns>
    Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default);
    #endregion

    #region 按修订号创建或替换主 Agent 分配（TryReplaceAsync）
    /// <summary>
    /// 按修订号创建或替换主 Agent 分配（TryReplaceAsync）。
    /// </summary>
    /// <param name="value">新的主 Agent 分配；初次创建的修订号为零，替换时为预期修订号加一。</param>
    /// <param name="expectedLogicalRevision">为 null 时仅尝试初次创建；非 null 时要求现有记录修订号匹配，不允许为 long.MaxValue。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功创建或更新一条分配记录时返回 true；新修订号不合法、初次创建时记录已存在，或更新时未匹配到预期修订号的未删除记录时返回 false。</returns>
    Task<bool> TryReplaceAsync(MainAgentAssignment value, long? expectedLogicalRevision, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 设置主 Agent 的命令。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record SetMainAgentCommand(Guid AgentId, long? ExpectedLogicalRevision);

/// <summary>
/// 定义主 Agent 分配领域错误码。
/// </summary>
public static class MainAgentErrorCodes
{
    /// <summary>表示 <c>NotConfigured</c> 场景的错误码。</summary>
    public const string NotConfigured = "MAIN_AGENT_NOT_CONFIGURED";
    /// <summary>表示 <c>AgentNotFound</c> 场景的错误码。</summary>
    public const string AgentNotFound = "MAIN_AGENT_AGENT_NOT_FOUND";
    /// <summary>表示 <c>AgentDisabled</c> 场景的错误码。</summary>
    public const string AgentDisabled = "MAIN_AGENT_AGENT_DISABLED";
    /// <summary>表示 <c>VersionMissing</c> 场景的错误码。</summary>
    public const string VersionMissing = "MAIN_AGENT_VERSION_MISSING";
    /// <summary>表示 <c>RowVersionConflict</c> 场景的错误码。</summary>
    public const string RowVersionConflict = "MAIN_AGENT_ROW_VERSION_CONFLICT";
}

/// <summary>
/// 将主 Agent 领域错误映射为服务状态码。
/// </summary>
public static class MainAgentServiceStatusCodes
{
    /// <summary>表示 <c>NotConfigured</c> 场景映射的服务状态码。</summary>
    public const int NotConfigured = 610004;
    /// <summary>表示 <c>AgentNotFound</c> 场景映射的服务状态码。</summary>
    public const int AgentNotFound = 610018;
    /// <summary>表示 <c>AgentDisabled</c> 场景映射的服务状态码。</summary>
    public const int AgentDisabled = 610019;
    /// <summary>表示 <c>VersionMissing</c> 场景映射的服务状态码。</summary>
    public const int VersionMissing = 610020;
    /// <summary>表示 <c>RowVersionConflict</c> 场景映射的服务状态码。</summary>
    public const int RowVersionConflict = 610021;

    #region 转换（FromErrorCode）
    /// <summary>
    /// 转换（FromErrorCode）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <returns>主 Agent 错误码对应的服务状态值；未知错误码使用 500。</returns>
    public static int FromErrorCode(string code) => code switch
    {
        MainAgentErrorCodes.NotConfigured => NotConfigured,
        MainAgentErrorCodes.AgentNotFound => AgentNotFound,
        MainAgentErrorCodes.AgentDisabled => AgentDisabled,
        MainAgentErrorCodes.VersionMissing => VersionMissing,
        MainAgentErrorCodes.RowVersionConflict => RowVersionConflict,
        _ => 500
    };
    #endregion

    #region 转换（ToErrorCode）
    /// <summary>
    /// 转换（ToErrorCode）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <returns>服务状态值对应的主 Agent 错误码；未知状态使用 INTERNAL_ERROR。</returns>
    public static string ToErrorCode(int status) => status switch
    {
        NotConfigured => MainAgentErrorCodes.NotConfigured,
        AgentNotFound => MainAgentErrorCodes.AgentNotFound,
        AgentDisabled => MainAgentErrorCodes.AgentDisabled,
        VersionMissing => MainAgentErrorCodes.VersionMissing,
        RowVersionConflict => MainAgentErrorCodes.RowVersionConflict,
        _ => "INTERNAL_ERROR"
    };
    #endregion
}

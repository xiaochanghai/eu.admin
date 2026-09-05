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
    /// <summary>获取主 Agent 分配记录。</summary>
    Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>按并发条件尝试替换主 Agent 分配记录。</summary>
    Task<bool> TryReplaceAsync(
        MainAgentAssignment value,
        long? expectedLogicalRevision,
        CancellationToken cancellationToken = default);
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

    public static int FromErrorCode(string code) => code switch
    {
        MainAgentErrorCodes.NotConfigured => NotConfigured,
        MainAgentErrorCodes.AgentNotFound => AgentNotFound,
        MainAgentErrorCodes.AgentDisabled => AgentDisabled,
        MainAgentErrorCodes.VersionMissing => VersionMissing,
        MainAgentErrorCodes.RowVersionConflict => RowVersionConflict,
        _ => 500
    };

    public static string ToErrorCode(int status) => status switch
    {
        NotConfigured => MainAgentErrorCodes.NotConfigured,
        AgentNotFound => MainAgentErrorCodes.AgentNotFound,
        AgentDisabled => MainAgentErrorCodes.AgentDisabled,
        VersionMissing => MainAgentErrorCodes.VersionMissing,
        RowVersionConflict => MainAgentErrorCodes.RowVersionConflict,
        _ => "INTERNAL_ERROR"
    };
}

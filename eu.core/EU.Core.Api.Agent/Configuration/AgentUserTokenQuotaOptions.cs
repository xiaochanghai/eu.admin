using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>按集团 + 公司共享的自然日/月 Token 配额；用户仅作为调用人或管理员留痕。</summary>
public sealed class AgentUserTokenQuotaOptions
{
    /// <summary>现有 appsettings.json 中的配置节。</summary>
    public const string SectionName = "AgentUserTokenQuota";
    /// <summary>该集团、公司所有用户和 Agent 的每日总 Token 上限；null 关闭日额度。</summary>
    public long? DailyTotalTokens { get; init; }
    /// <summary>该集团、公司所有用户和 Agent 的每月总 Token 上限；null 关闭月额度。</summary>
    public long? MonthlyTotalTokens { get; init; }
    /// <summary>每次真实模型请求的固定预占量；启用配额时必须显式设置。</summary>
    public long? RequestReservationTokens { get; init; }
    /// <summary>自然日/月边界时区，默认中国标准时间；运行中的周期不能随意更换。</summary>
    public string TimeZoneId { get; init; } = "Asia/Shanghai";
    /// <summary>启用数据库租户覆盖设置及管理入口；默认关闭，启用前手工部署迁移。</summary>
    public bool ManagementEnabled { get; init; }
    /// <summary>额度管理员的集团/公司/用户组合；空列表不授予任何用户管理权。</summary>
    public AgentUserTokenQuotaAdministrator[] Administrators { get; init; } = [];
    /// <summary>是否至少配置了一个周期上限。</summary>
    public bool Enabled => DailyTotalTokens.HasValue || MonthlyTotalTokens.HasValue;
}

/// <summary>仅用于额度管理授权的可信部署配置，不是登录凭据。</summary>
public sealed class AgentUserTokenQuotaAdministrator
{
    /// <summary>管理员所属集团，必须是有效 GUID。</summary>
    public Guid GroupId { get; init; }
    /// <summary>管理员所属公司，必须是有效 GUID。</summary>
    public Guid CompanyId { get; init; }
    /// <summary>现有登录用户标识，不新增第二套认证。</summary>
    public Guid UserId { get; init; }
}

/// <summary>启用前校验额度、预占粒度及自然周期时区。</summary>
public sealed class AgentUserTokenQuotaOptionsValidator : IValidateOptions<AgentUserTokenQuotaOptions>
{
    #region 校验额度配置（Validate）
    /// <summary>关闭额度无需预占量；启用时拒绝隐式默认额度及非法时区。</summary>
    /// <param name="name">选项名称。</param>
    /// <param name="options">宿主配额配置。</param>
    /// <returns>配置校验结果。</returns>
    public ValidateOptionsResult Validate(string? name, AgentUserTokenQuotaOptions options)
    {
        if (options.Administrators is null || options.Administrators.Length > 100 ||
            options.Administrators.Any(x => x is null || x.GroupId == Guid.Empty || x.CompanyId == Guid.Empty || x.UserId == Guid.Empty) ||
            options.Administrators.Select(x => (x.GroupId, x.CompanyId, x.UserId)).Distinct().Count() != options.Administrators.Length)
            return ValidateOptionsResult.Fail("AgentUserTokenQuota.Administrators requires at most 100 unique group/company/user combinations.");
        if (options.DailyTotalTokens is < 1 || options.MonthlyTotalTokens is < 1 || options.RequestReservationTokens is < 1 ||
            (options.Enabled && (options.RequestReservationTokens is null || options.RequestReservationTokens > options.DailyTotalTokens || options.RequestReservationTokens > options.MonthlyTotalTokens)))
            return ValidateOptionsResult.Fail("AgentUserTokenQuota requires positive limits and an explicit reservation not exceeding each enabled limit.");
        // 关闭功能不依赖容器是否包含时区数据库，也不解析数据库服务。
        if (!options.Enabled && !options.ManagementEnabled) return ValidateOptionsResult.Success;
        if (string.IsNullOrWhiteSpace(options.TimeZoneId) || options.TimeZoneId.Length > 128)
            return ValidateOptionsResult.Fail("AgentUserTokenQuota.TimeZoneId is required when quotas are enabled.");
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId); }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        { return ValidateOptionsResult.Fail("AgentUserTokenQuota.TimeZoneId must be an available system time zone."); }
        return ValidateOptionsResult.Success;
    }
    #endregion
}

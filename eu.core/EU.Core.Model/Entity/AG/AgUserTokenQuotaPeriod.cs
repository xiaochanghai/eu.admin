namespace EU.Core.Model.Entity;

/// <summary>集团、公司共享的日/月 Token 账本，所有用户和 Agent 共用同一周期。</summary>
[SugarTable("AgUserTokenQuotaPeriod", "租户 Token 配额周期账本")]
[SugarIndex("UX_AgUserTokenQuotaPeriod_OwnerPeriod", nameof(GroupId), OrderByType.Asc, nameof(CompanyId), OrderByType.Asc, nameof(PeriodKind), OrderByType.Asc, nameof(PeriodKey), OrderByType.Asc, true)]
public class AgUserTokenQuotaPeriod : BasePoco
{
    /// <summary>0 为自然日，1 为自然月。</summary>
    public int PeriodKind { get; set; }
    /// <summary>本地日期及配置时区组成的周期键。</summary>
    [SugarColumn(Length = 160)]
    public string PeriodKey { get; set; } = string.Empty;
    /// <summary>周期起点（含），UTC。</summary>
    public DateTime StartUtc { get; set; }
    /// <summary>周期终点（不含），UTC。</summary>
    public DateTime EndUtc { get; set; }
    /// <summary>已经按供应商完整响应结算的实际总 Token。</summary>
    public long UsedTokens { get; set; }
    /// <summary>尚未结算的预占量，重启后保留，不自动过期退还。</summary>
    public long ReservedTokens { get; set; }
    /// <summary>是否存在未知用量；为真时禁止当前周期新增调用。</summary>
    public bool HasUnknownUsage { get; set; }
    /// <summary>乐观并发版本，防止多请求丢失更新。</summary>
    public long Revision { get; set; }
}

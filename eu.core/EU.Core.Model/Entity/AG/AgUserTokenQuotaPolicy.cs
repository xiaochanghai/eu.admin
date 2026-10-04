namespace EU.Core.Model.Entity;

/// <summary>同一集团、公司内所有用户和 Agent 共用的额度覆盖设置。</summary>
[SugarTable("AgUserTokenQuotaPolicy", "租户 Token 额度设置")]
[SugarIndex("UX_AgUserTokenQuotaPolicy_Owner", nameof(GroupId), OrderByType.Asc, nameof(CompanyId), OrderByType.Asc, true)]
public class AgUserTokenQuotaPolicy : BasePoco
{
    /// <summary>使用宿主默认设置。</summary>
    public bool UseDefaults { get; set; }
    /// <summary>日额度，未启用时为空。</summary>
    [SugarColumn(IsNullable = true)]
    public long? DailyTotalTokens { get; set; }
    /// <summary>月额度，未启用时为空。</summary>
    [SugarColumn(IsNullable = true)]
    public long? MonthlyTotalTokens { get; set; }
    /// <summary>每次请求预占量。</summary>
    [SugarColumn(IsNullable = true)]
    public long? RequestReservationTokens { get; set; }
    /// <summary>乐观并发版本，每次修改递增。</summary>
    public long Revision { get; set; }
}

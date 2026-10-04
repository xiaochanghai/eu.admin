namespace EU.Core.Model.Entity;

/// <summary>一次供应商模型请求的预占及幂等结算记录。</summary>
[SugarTable("AgUserTokenQuotaReservation", "租户 Token 配额预占记录")]
[SugarIndex("IX_AgUserTokenQuotaReservation_OwnerState", nameof(GroupId), OrderByType.Asc, nameof(CompanyId), OrderByType.Asc, nameof(State), OrderByType.Asc)]
public class AgUserTokenQuotaReservation : BasePoco
{
    /// <summary>实际发起模型请求的用户，仅用于追踪，不参与额度归属。</summary>
    public Guid ConsumerUserId { get; set; }
    /// <summary>实际发出供应商请求的运行标识，不用于配额分组。</summary>
    public Guid RunId { get; set; }
    /// <summary>原始日周期，未配置日额度时为空。</summary>
    [SugarColumn(IsNullable = true)]
    public Guid? DailyPeriodId { get; set; }
    /// <summary>原始月周期，未配置月额度时为空。</summary>
    [SugarColumn(IsNullable = true)]
    public Guid? MonthlyPeriodId { get; set; }
    /// <summary>本次请求预占量。</summary>
    public long ReservedTokens { get; set; }
    /// <summary>实际报告的总 Token，未知或未完成时为空，不能当成零。</summary>
    [SugarColumn(IsNullable = true)]
    public long? ActualTokens { get; set; }
    /// <summary>0 预占，1 已开始，2 已结算，3 用量未知，4 未开始已释放，5 人工核实已结算。</summary>
    public int State { get; set; }
    /// <summary>最终结算或释放时间（UTC），处理中为空。</summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? SettledAtUtc { get; set; }
}

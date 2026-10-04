namespace EU.Core.Model.Entity;

/// <summary>额度设置及人工对账的追加式审计，和修改在同一事务提交。</summary>
[SugarTable("AgUserTokenQuotaAdjustment", "租户 Token 额度调整审计")]
[SugarIndex("IX_AgUserTokenQuotaAdjustment_Owner", nameof(GroupId), OrderByType.Asc, nameof(CompanyId), OrderByType.Asc)]
public class AgUserTokenQuotaAdjustment : BasePoco
{
    /// <summary>执行人工操作的认证用户。</summary>
    public Guid OperatorId { get; set; }
    /// <summary>Policy 或 Reconcile，不接受外部自定义操作种类。</summary>
    [SugarColumn(Length = 16)]
    public string Kind { get; set; } = "";
    /// <summary>设置记录或预占记录标识。</summary>
    public Guid TargetId { get; set; }
    /// <summary>命令哈希，检测幂等标识被不同命令复用。</summary>
    [SugarColumn(Length = 64)]
    public string CommandHash { get; set; } = "";
    /// <summary>修改前快照，内部有界结构。</summary>
    [SugarColumn(Length = 2000)]
    public string BeforeJson { get; set; } = "";
    /// <summary>修改后回执，重复提交的事实源。</summary>
    [SugarColumn(Length = 2000)]
    public string ResultJson { get; set; } = "";
    /// <summary>人工修改原因。</summary>
    [SugarColumn(Length = 500)]
    public string Reason { get; set; } = "";
    /// <summary>核查依据引用，不存放凭据或供应商账单正文。</summary>
    [SugarColumn(Length = 256)]
    public string EvidenceReference { get; set; } = "";
    /// <summary>事务应用时间（UTC）。</summary>
    public DateTime AppliedAtUtc { get; set; }
}

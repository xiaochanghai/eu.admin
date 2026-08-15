namespace EU.Core.Model.Entity;

/// <summary>
/// 评测套件定义主表。
/// </summary>
[SugarTable("AgEvaluationSuite", "评测套件定义主表"), Entity(TableCnName = "评测套件定义主表", TableName = "AgEvaluationSuite")]
public class AgEvaluationSuite : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    /// <summary>
    /// 租户内唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("租户内唯一编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// 套件显示名称
    /// </summary>
    [Display(Name = "Name"), Description("套件显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// 套件说明
    /// </summary>
    [Display(Name = "Description"), Description("套件说明"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    /// <summary>
    /// 生命周期状态
    /// </summary>
    [Display(Name = "Status"), Description("生命周期状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 逻辑修订号
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 业务创建 UTC 时间
    /// </summary>
    [Display(Name = "CreatedAtUtc"), Description("业务创建 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? CreatedAtUtc { get; set; }

    /// <summary>
    /// 业务更新 UTC 时间
    /// </summary>
    [Display(Name = "UpdatedAtUtc"), Description("业务更新 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// 创建用户标识
    /// </summary>
    [Display(Name = "CreatedByUserId"), Description("创建用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// 更新用户标识
    /// </summary>
    [Display(Name = "UpdatedByUserId"), Description("更新用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string UpdatedByUserId { get; set; }
}

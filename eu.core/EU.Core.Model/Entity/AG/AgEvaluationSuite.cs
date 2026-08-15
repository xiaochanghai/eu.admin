namespace EU.Core.Model.Entity;

/// <summary>
/// 评测套件定义主表。
/// </summary>
[SugarTable("AgEvaluationSuite", "评测套件定义主表"), Entity(TableCnName = "评测套件定义主表", TableName = "AgEvaluationSuite")]
public class AgEvaluationSuite : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    [Display(Name = "Code"), Description("租户内唯一编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    [Display(Name = "Name"), Description("套件显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    [Display(Name = "Description"), Description("套件说明"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    [Display(Name = "Status"), Description("生命周期状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    [Display(Name = "CreatedAtUtc"), Description("业务创建 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? CreatedAtUtc { get; set; }

    [Display(Name = "UpdatedAtUtc"), Description("业务更新 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAtUtc { get; set; }

    [Display(Name = "CreatedByUserId"), Description("创建用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string CreatedByUserId { get; set; }

    [Display(Name = "UpdatedByUserId"), Description("更新用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string UpdatedByUserId { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>
/// 编排定义主表。
/// </summary>
[SugarTable("AgOrchestrationDefinition", "编排定义主表"), Entity(TableCnName = "编排定义主表", TableName = "AgOrchestrationDefinition")]
public class AgOrchestrationDefinition : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "Code"), Description("编排唯一编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    [Display(Name = "Name"), Description("编排显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    [Display(Name = "Description"), Description("编排说明"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    [Display(Name = "Status"), Description("生命周期状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }
}

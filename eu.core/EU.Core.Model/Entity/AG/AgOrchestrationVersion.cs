namespace EU.Core.Model.Entity;

/// <summary>
/// 编排草稿和发布版本表。
/// </summary>
[SugarTable("AgOrchestrationVersion", "编排草稿和发布版本表"), Entity(TableCnName = "编排草稿和发布版本表", TableName = "AgOrchestrationVersion")]
public class AgOrchestrationVersion : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "OrchestrationId"), Description("所属编排主键"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    [Display(Name = "Ordinal"), Description("版本排列顺序；草稿固定为 0"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "Label"), Description("版本标签"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Label { get; set; }

    [Display(Name = "IsDraft"), Description("是否为草稿版本"), SugarColumn(IsNullable = true)]
    public bool? IsDraft { get; set; }

    [Display(Name = "StartNodeId"), Description("起始节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string StartNodeId { get; set; }
}

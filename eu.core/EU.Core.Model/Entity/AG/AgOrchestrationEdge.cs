namespace EU.Core.Model.Entity;

/// <summary>
/// 编排版本连线表。
/// </summary>
[SugarTable("AgOrchestrationEdge", "编排版本连线表"), Entity(TableCnName = "编排版本连线表", TableName = "AgOrchestrationEdge")]
public class AgOrchestrationEdge : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "OrchestrationId"), Description("所属编排主键"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    [Display(Name = "VersionId"), Description("所属编排版本主键"), SugarColumn(IsNullable = true)]
    public Guid? VersionId { get; set; }

    [Display(Name = "Ordinal"), Description("连线存储顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "FromNodeId"), Description("源节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string FromNodeId { get; set; }

    [Display(Name = "ToNodeId"), Description("目标节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ToNodeId { get; set; }

    [Display(Name = "Condition"), Description("连线条件"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Condition { get; set; }

    [Display(Name = "ConditionValue"), Description("连线条件值"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string ConditionValue { get; set; }

    [Display(Name = "SortOrder"), Description("条件匹配顺序"), SugarColumn(IsNullable = true)]
    public int? SortOrder { get; set; }
}

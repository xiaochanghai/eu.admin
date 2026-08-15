namespace EU.Core.Model.Entity;

/// <summary>
/// 编排版本连线表。
/// </summary>
[SugarTable("AgOrchestrationEdge", "编排版本连线表"), Entity(TableCnName = "编排版本连线表", TableName = "AgOrchestrationEdge")]
public class AgOrchestrationEdge : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属编排主键
    /// </summary>
    [Display(Name = "OrchestrationId"), Description("所属编排主键"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    /// <summary>
    /// 所属编排版本主键
    /// </summary>
    [Display(Name = "VersionId"), Description("所属编排版本主键"), SugarColumn(IsNullable = true)]
    public Guid? VersionId { get; set; }

    /// <summary>
    /// 连线存储顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("连线存储顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 源节点标识
    /// </summary>
    [Display(Name = "FromNodeId"), Description("源节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string FromNodeId { get; set; }

    /// <summary>
    /// 目标节点标识
    /// </summary>
    [Display(Name = "ToNodeId"), Description("目标节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ToNodeId { get; set; }

    /// <summary>
    /// 连线条件
    /// </summary>
    [Display(Name = "Condition"), Description("连线条件"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Condition { get; set; }

    /// <summary>
    /// 连线条件值
    /// </summary>
    [Display(Name = "ConditionValue"), Description("连线条件值"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string ConditionValue { get; set; }

    /// <summary>
    /// 条件匹配顺序
    /// </summary>
    [Display(Name = "SortOrder"), Description("条件匹配顺序"), SugarColumn(IsNullable = true)]
    public int? SortOrder { get; set; }
}

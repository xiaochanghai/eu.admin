namespace EU.Core.Model.Entity;

/// <summary>
/// 编排定义主表。
/// </summary>
[SugarTable("AgOrchestrationDefinition", "编排定义主表"), Entity(TableCnName = "编排定义主表", TableName = "AgOrchestrationDefinition")]
public class AgOrchestrationDefinition : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 编排唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("编排唯一编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// 编排显示名称
    /// </summary>
    [Display(Name = "Name"), Description("编排显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// 编排说明
    /// </summary>
    [Display(Name = "Description"), Description("编排说明"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
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
}

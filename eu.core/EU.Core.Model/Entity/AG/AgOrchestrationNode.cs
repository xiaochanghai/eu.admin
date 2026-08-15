namespace EU.Core.Model.Entity;

/// <summary>
/// 编排版本节点表。
/// </summary>
[SugarTable("AgOrchestrationNode", "编排版本节点表"), Entity(TableCnName = "编排版本节点表", TableName = "AgOrchestrationNode")]
public class AgOrchestrationNode : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "OrchestrationId"), Description("所属编排主键"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    [Display(Name = "VersionId"), Description("所属编排版本主键"), SugarColumn(IsNullable = true)]
    public Guid? VersionId { get; set; }

    [Display(Name = "Ordinal"), Description("节点排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "NodeId"), Description("版本内节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    [Display(Name = "Name"), Description("节点显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    [Display(Name = "AgentId"), Description("节点使用的 Agent 主键"), SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    [Display(Name = "InputMode"), Description("输入模式"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string InputMode { get; set; }

    [Display(Name = "InputTemplate"), Description("输入模板"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputTemplate { get; set; }

    [Display(Name = "MaximumRetries"), Description("最大重试次数"), SugarColumn(IsNullable = true)]
    public int? MaximumRetries { get; set; }

    [Display(Name = "TimeoutSeconds"), Description("节点超时秒数"), SugarColumn(IsNullable = true)]
    public int? TimeoutSeconds { get; set; }
}

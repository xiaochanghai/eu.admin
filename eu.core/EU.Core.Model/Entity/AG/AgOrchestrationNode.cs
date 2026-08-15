namespace EU.Core.Model.Entity;

/// <summary>
/// 编排版本节点表。
/// </summary>
[SugarTable("AgOrchestrationNode", "编排版本节点表"), Entity(TableCnName = "编排版本节点表", TableName = "AgOrchestrationNode")]
public class AgOrchestrationNode : BasePoco
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
    /// 节点排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("节点排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 版本内节点标识
    /// </summary>
    [Display(Name = "NodeId"), Description("版本内节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    /// <summary>
    /// 节点显示名称
    /// </summary>
    [Display(Name = "Name"), Description("节点显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// 节点使用的 Agent 主键
    /// </summary>
    [Display(Name = "AgentId"), Description("节点使用的 Agent 主键"), SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// 输入模式
    /// </summary>
    [Display(Name = "InputMode"), Description("输入模式"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string InputMode { get; set; }

    /// <summary>
    /// 输入模板
    /// </summary>
    [Display(Name = "InputTemplate"), Description("输入模板"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputTemplate { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [Display(Name = "MaximumRetries"), Description("最大重试次数"), SugarColumn(IsNullable = true)]
    public int? MaximumRetries { get; set; }

    /// <summary>
    /// 节点超时秒数
    /// </summary>
    [Display(Name = "TimeoutSeconds"), Description("节点超时秒数"), SugarColumn(IsNullable = true)]
    public int? TimeoutSeconds { get; set; }
}

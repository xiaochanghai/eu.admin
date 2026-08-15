namespace EU.Core.Model.Entity;

/// <summary>
/// 编排运行节点汇总表 (Model)
/// </summary>
[SugarTable("AgOrchestrationRunNode", "编排运行节点汇总表"), Entity(TableCnName = "编排运行节点汇总表", TableName = "AgOrchestrationRunNode")]
public class AgOrchestrationRunNode : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属编排运行标识
    /// </summary>
    [Display(Name = "RunId"), Description("所属编排运行标识"), SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    /// <summary>
    /// 节点排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("节点排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 编排版本内节点标识
    /// </summary>
    [Display(Name = "NodeId"), Description("编排版本内节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    /// <summary>
    /// 节点显示名称
    /// </summary>
    [Display(Name = "NodeName"), Description("节点显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string NodeName { get; set; }

    /// <summary>
    /// 节点使用的 Agent 标识
    /// </summary>
    [Display(Name = "AgentId"), Description("节点使用的 Agent 标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// 节点使用的 Agent 版本标识
    /// </summary>
    [Display(Name = "AgentVersionId"), Description("节点使用的 Agent 版本标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentVersionId { get; set; }

    /// <summary>
    /// 节点执行状态
    /// </summary>
    [Display(Name = "Status"), Description("节点执行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 节点执行尝试次数
    /// </summary>
    [Display(Name = "Attempts"), Description("节点执行尝试次数"), SugarColumn(IsNullable = true)]
    public int? Attempts { get; set; }

    /// <summary>
    /// 节点开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("节点开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// 节点结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("节点结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 节点输出字符数
    /// </summary>
    [Display(Name = "OutputCharacters"), Description("节点输出字符数"), SugarColumn(IsNullable = true)]
    public int? OutputCharacters { get; set; }

    /// <summary>
    /// 节点输入内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "InputSha256"), Description("节点输入内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    /// <summary>
    /// 节点执行错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("节点执行错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

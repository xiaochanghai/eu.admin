namespace EU.Core.Model.Entity;

/// <summary>
/// 编排节点执行尝试明细表 (Model)
/// </summary>
[SugarTable("AgOrchestrationNodeAttempt", "编排节点执行尝试明细表"), Entity(TableCnName = "编排节点执行尝试明细表", TableName = "AgOrchestrationNodeAttempt")]
public class AgOrchestrationNodeAttempt : BasePoco
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
    /// 编排版本内节点标识
    /// </summary>
    [Display(Name = "NodeId"), Description("编排版本内节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    /// <summary>
    /// 节点重试序号
    /// </summary>
    [Display(Name = "Attempt"), Description("节点重试序号"), SugarColumn(IsNullable = true)]
    public int? Attempt { get; set; }

    /// <summary>
    /// 运行内执行排列顺序
    /// </summary>
    [Display(Name = "Sequence"), Description("运行内执行排列顺序"), SugarColumn(IsNullable = true)]
    public int? Sequence { get; set; }

    /// <summary>
    /// 关联的 Agent 运行标识
    /// </summary>
    [Display(Name = "AgentRunId"), Description("关联的 Agent 运行标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentRunId { get; set; }

    /// <summary>
    /// 本次尝试输入内容
    /// </summary>
    [Display(Name = "InputText"), Description("本次尝试输入内容"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputText { get; set; }

    /// <summary>
    /// 本次尝试输入摘要
    /// </summary>
    [Display(Name = "InputSha256"), Description("本次尝试输入摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    /// <summary>
    /// 本次尝试输出内容
    /// </summary>
    [Display(Name = "OutputText"), Description("本次尝试输出内容"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string OutputText { get; set; }

    /// <summary>
    /// 本次尝试输出摘要
    /// </summary>
    [Display(Name = "OutputSha256"), Description("本次尝试输出摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutputSha256 { get; set; }

    /// <summary>
    /// 本次尝试状态
    /// </summary>
    [Display(Name = "Status"), Description("本次尝试状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 本次尝试开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("本次尝试开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// 本次尝试结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("本次尝试结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 本次尝试错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("本次尝试错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

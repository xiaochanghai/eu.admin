namespace EU.Core.Model.Entity;

/// <summary>
/// 编排节点工具调用明细表 (Model)
/// </summary>
[SugarTable("AgOrchestrationToolCall", "编排节点工具调用明细表"), Entity(TableCnName = "编排节点工具调用明细表", TableName = "AgOrchestrationToolCall")]
public class AgOrchestrationToolCall : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 工具调用业务标识
    /// </summary>
    [Display(Name = "ToolCallId"), Description("工具调用业务标识"), SugarColumn(IsNullable = true)]
    public Guid? ToolCallId { get; set; }

    /// <summary>
    /// 所属编排运行标识
    /// </summary>
    [Display(Name = "RunId"), Description("所属编排运行标识"), SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    /// <summary>
    /// 所属节点标识
    /// </summary>
    [Display(Name = "NodeId"), Description("所属节点标识"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    /// <summary>
    /// 所属节点重试序号
    /// </summary>
    [Display(Name = "Attempt"), Description("所属节点重试序号"), SugarColumn(IsNullable = true)]
    public int? Attempt { get; set; }

    /// <summary>
    /// 尝试内工具调用顺序
    /// </summary>
    [Display(Name = "Sequence"), Description("尝试内工具调用顺序"), SugarColumn(IsNullable = true)]
    public int? Sequence { get; set; }

    /// <summary>
    /// 关联的 Agent 运行标识
    /// </summary>
    [Display(Name = "AgentRunId"), Description("关联的 Agent 运行标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentRunId { get; set; }

    /// <summary>
    /// 调用的工具版本标识
    /// </summary>
    [Display(Name = "ToolVersionId"), Description("调用的工具版本标识"), SugarColumn(IsNullable = true)]
    public Guid? ToolVersionId { get; set; }

    /// <summary>
    /// 工具名称
    /// </summary>
    [Display(Name = "ToolName"), Description("工具名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string ToolName { get; set; }

    /// <summary>
    /// 工具调用状态
    /// </summary>
    [Display(Name = "Status"), Description("工具调用状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 工具调用参数 JSON
    /// </summary>
    [Display(Name = "ArgumentsJson"), Description("工具调用参数 JSON"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string ArgumentsJson { get; set; }

    /// <summary>
    /// 工具调用结果内容
    /// </summary>
    [Display(Name = "ResultContent"), Description("工具调用结果内容"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string ResultContent { get; set; }

    /// <summary>
    /// 工具调用结果摘要
    /// </summary>
    [Display(Name = "ResultSha256"), Description("工具调用结果摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ResultSha256 { get; set; }

    /// <summary>
    /// 工具调用结果字符数
    /// </summary>
    [Display(Name = "ResultCharacters"), Description("工具调用结果字符数"), SugarColumn(IsNullable = true)]
    public long? ResultCharacters { get; set; }

    /// <summary>
    /// 工具调用开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("工具调用开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// 工具调用结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("工具调用结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 工具调用错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("工具调用错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

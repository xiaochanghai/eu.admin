namespace EU.Core.Model.Entity;

/// <summary>
/// Agent 工具调用审计明细表 (Model)
/// </summary>
[SugarTable("AgAgentToolCallAudit", "Agent 工具调用审计明细表"), Entity(TableCnName = "Agent 工具调用审计明细表", TableName = "AgAgentToolCallAudit")]
public class AgAgentToolCallAudit : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属 Agent 运行标识
    /// </summary>
    [Display(Name = "RunId"), Description("所属 Agent 运行标识"), SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    /// <summary>
    /// 工具调用排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("工具调用排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

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
    /// 工具风险等级
    /// </summary>
    [Display(Name = "Risk"), Description("工具风险等级"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Risk { get; set; }

    /// <summary>
    /// 工具调用结果状态
    /// </summary>
    [Display(Name = "Status"), Description("工具调用结果状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

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

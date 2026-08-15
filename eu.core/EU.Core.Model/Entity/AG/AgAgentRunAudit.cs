namespace EU.Core.Model.Entity;

/// <summary>
/// Agent 运行审计汇总表 (Model)
/// </summary>
[SugarTable("AgAgentRunAudit", "Agent 运行审计汇总表"), Entity(TableCnName = "Agent 运行审计汇总表", TableName = "AgAgentRunAudit")]
public class AgAgentRunAudit : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 执行的 Agent 标识
    /// </summary>
    [Display(Name = "AgentId"), Description("执行的 Agent 标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// 执行使用的已发布 Agent 版本标识
    /// </summary>
    [Display(Name = "AgentVersionId"), Description("执行使用的已发布 Agent 版本标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentVersionId { get; set; }

    /// <summary>
    /// 执行时记录的 Agent 编码
    /// </summary>
    [Display(Name = "AgentCode"), Description("执行时记录的 Agent 编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string AgentCode { get; set; }

    /// <summary>
    /// Agent 运行状态
    /// </summary>
    [Display(Name = "Status"), Description("Agent 运行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// Agent 运行开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("Agent 运行开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Agent 运行结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("Agent 运行结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 运行输入内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "InputSha256"), Description("运行输入内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    /// <summary>
    /// 运行输出字符数
    /// </summary>
    [Display(Name = "OutputCharacters"), Description("运行输出字符数"), SugarColumn(IsNullable = true)]
    public int? OutputCharacters { get; set; }

    /// <summary>
    /// 工具调用次数
    /// </summary>
    [Display(Name = "ToolCallCount"), Description("工具调用次数"), SugarColumn(IsNullable = true)]
    public int? ToolCallCount { get; set; }

    /// <summary>
    /// 运行错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("运行错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

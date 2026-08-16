namespace EU.Core.Model.Entity;

/// <summary>统一入口 Agent 运行明细表。</summary>
[SugarTable("AgUnifiedAgentRun", "统一入口 Agent 运行明细表"), Entity(TableCnName = "统一入口 Agent 运行明细表", TableName = "AgUnifiedAgentRun")]
public class AgUnifiedAgentRun : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public new string CurrentNode { get; set; }
    [Display(Name = "EntryRunId"), Description("统一入口运行标识"), SugarColumn(IsNullable = true)] public Guid? EntryRunId { get; set; }
    [Display(Name = "Ordinal"), Description("明细顺序号"), SugarColumn(IsNullable = true)] public long? Ordinal { get; set; }
    [Display(Name = "ParentRunId"), Description("父运行标识"), SugarColumn(IsNullable = true)] public Guid? ParentRunId { get; set; }
    [Display(Name = "Kind"), Description("Agent 运行类型"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string Kind { get; set; }
    [Display(Name = "AgentId"), Description("Agent 标识"), SugarColumn(IsNullable = true)] public Guid? AgentId { get; set; }
    [Display(Name = "AgentVersionId"), Description("Agent 版本标识"), SugarColumn(IsNullable = true)] public Guid? AgentVersionId { get; set; }
    [Display(Name = "Depth"), Description("调用深度"), SugarColumn(IsNullable = true)] public int? Depth { get; set; }
    [Display(Name = "Status"), Description("运行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public string Status { get; set; }
    [Display(Name = "StartedAtUtc"), Description("开始时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? StartedAtUtc { get; set; }
    [Display(Name = "FinishedAtUtc"), Description("完成时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
    [Display(Name = "DurationTicks"), Description("运行耗时 Tick 数"), SugarColumn(IsNullable = true)] public long? DurationTicks { get; set; }
    [Display(Name = "InputText"), Description("运行输入"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string InputText { get; set; }
    [Display(Name = "InputSha256"), Description("输入 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string InputSha256 { get; set; }
    [Display(Name = "OutputText"), Description("运行输出"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string OutputText { get; set; }
    [Display(Name = "OutputSha256"), Description("输出 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string OutputSha256 { get; set; }
    [Display(Name = "ErrorCode"), Description("错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string ErrorCode { get; set; }
}

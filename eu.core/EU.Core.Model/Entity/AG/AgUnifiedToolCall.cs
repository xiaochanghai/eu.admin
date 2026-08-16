namespace EU.Core.Model.Entity;

/// <summary>统一入口工具调用明细表。</summary>
[SugarTable("AgUnifiedToolCall", "统一入口工具调用明细表"), Entity(TableCnName = "统一入口工具调用明细表", TableName = "AgUnifiedToolCall")]
public class AgUnifiedToolCall : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public new string CurrentNode { get; set; }
    [Display(Name = "EntryRunId"), Description("统一入口运行标识"), SugarColumn(IsNullable = true)] public Guid? EntryRunId { get; set; }
    [Display(Name = "Ordinal"), Description("明细顺序号"), SugarColumn(IsNullable = true)] public long? Ordinal { get; set; }
    [Display(Name = "ParentRunId"), Description("父运行标识"), SugarColumn(IsNullable = true)] public Guid? ParentRunId { get; set; }
    [Display(Name = "ToolVersionId"), Description("工具版本标识"), SugarColumn(IsNullable = true)] public Guid? ToolVersionId { get; set; }
    [Display(Name = "Depth"), Description("调用深度"), SugarColumn(IsNullable = true)] public int? Depth { get; set; }
    [Display(Name = "Status"), Description("调用状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public string Status { get; set; }
    [Display(Name = "StartedAtUtc"), Description("开始时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? StartedAtUtc { get; set; }
    [Display(Name = "FinishedAtUtc"), Description("完成时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
    [Display(Name = "DurationTicks"), Description("运行耗时 Tick 数"), SugarColumn(IsNullable = true)] public long? DurationTicks { get; set; }
    [Display(Name = "ArgumentsJson"), Description("调用参数 JSON"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string ArgumentsJson { get; set; }
    [Display(Name = "ArgumentsSha256"), Description("调用参数 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ArgumentsSha256 { get; set; }
    [Display(Name = "ResultContent"), Description("工具调用结果"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string ResultContent { get; set; }
    [Display(Name = "ResultSha256"), Description("工具结果 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ResultSha256 { get; set; }
    [Display(Name = "ErrorCode"), Description("错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string ErrorCode { get; set; }
}

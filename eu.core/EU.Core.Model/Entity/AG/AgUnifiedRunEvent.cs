namespace EU.Core.Model.Entity;

/// <summary>统一入口运行事件表。</summary>
[SugarTable("AgUnifiedRunEvent", "统一入口运行事件表"), Entity(TableCnName = "统一入口运行事件表", TableName = "AgUnifiedRunEvent")]
public class AgUnifiedRunEvent : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public new string CurrentNode { get; set; }
    [Display(Name = "EntryRunId"), Description("统一入口运行标识"), SugarColumn(IsNullable = true)] public Guid? EntryRunId { get; set; }
    [Display(Name = "Sequence"), Description("事件序号"), SugarColumn(IsNullable = true)] public long? Sequence { get; set; }
    [Display(Name = "CorrelationId"), Description("关联追踪标识"), SugarColumn(IsNullable = true)] public Guid? CorrelationId { get; set; }
    [Display(Name = "Kind"), Description("事件类型"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string Kind { get; set; }
    [Display(Name = "OccurredAtUtc"), Description("发生时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? OccurredAtUtc { get; set; }
    [Display(Name = "ParentRunId"), Description("父运行标识"), SugarColumn(IsNullable = true)] public Guid? ParentRunId { get; set; }
    [Display(Name = "Depth"), Description("调用深度"), SugarColumn(IsNullable = true)] public int? Depth { get; set; }
    [Display(Name = "PayloadJson"), Description("事件载荷 JSON"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string PayloadJson { get; set; }
    [Display(Name = "PayloadSha256"), Description("事件载荷 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string PayloadSha256 { get; set; }
}

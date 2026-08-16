namespace EU.Core.Model.Entity;

/// <summary>统一入口运行表。</summary>
[SugarTable("AgUnifiedEntryRun", "统一入口运行表"), Entity(TableCnName = "统一入口运行表", TableName = "AgUnifiedEntryRun")]
public class AgUnifiedEntryRun : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public new string CurrentNode { get; set; }
    [Display(Name = "ConversationId"), Description("会话标识"), SugarColumn(IsNullable = true)] public Guid? ConversationId { get; set; }
    [Display(Name = "CorrelationId"), Description("关联追踪标识"), SugarColumn(IsNullable = true)] public Guid? CorrelationId { get; set; }
    [Display(Name = "MainAgentVersionId"), Description("主 Agent 版本标识"), SugarColumn(IsNullable = true)] public Guid? MainAgentVersionId { get; set; }
    [Display(Name = "Status"), Description("运行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public string Status { get; set; }
    [Display(Name = "StartedAtUtc"), Description("开始时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? StartedAtUtc { get; set; }
    [Display(Name = "FinishedAtUtc"), Description("完成时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
    [Display(Name = "DurationTicks"), Description("运行耗时 Tick 数"), SugarColumn(IsNullable = true)] public long? DurationTicks { get; set; }
    [Display(Name = "InputText"), Description("运行输入"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string InputText { get; set; }
    [Display(Name = "InputSha256"), Description("输入 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string InputSha256 { get; set; }
    [Display(Name = "OutputText"), Description("运行输出"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string OutputText { get; set; }
    [Display(Name = "OutputSha256"), Description("输出 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string OutputSha256 { get; set; }
    [Display(Name = "ErrorCode"), Description("错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string ErrorCode { get; set; }
    [Display(Name = "PersistenceRevision"), Description("持久化修订号"), SugarColumn(IsNullable = true)] public long? PersistenceRevision { get; set; }
    [Display(Name = "StateSha256"), Description("保存操作指纹 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string StateSha256 { get; set; }
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string TenantId { get; set; }
    [Display(Name = "UserId"), Description("用户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string UserId { get; set; }
}

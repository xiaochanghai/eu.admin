namespace EU.Core.Model.Entity;

/// <summary>工具调用审批请求表。</summary>
[SugarTable("AgToolApprovalRequest", "工具调用审批请求表"), Entity(TableCnName = "工具调用审批请求表", TableName = "AgToolApprovalRequest")]
public class AgToolApprovalRequest : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string TenantId { get; set; }
    [Display(Name = "RequesterUserId"), Description("请求用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string RequesterUserId { get; set; }
    [Display(Name = "ConversationId"), Description("会话标识"), SugarColumn(IsNullable = true)] public Guid? ConversationId { get; set; }
    [Display(Name = "EntryRunId"), Description("统一入口运行标识"), SugarColumn(IsNullable = true)] public Guid? EntryRunId { get; set; }
    [Display(Name = "AgentRunId"), Description("Agent 运行标识"), SugarColumn(IsNullable = true)] public Guid? AgentRunId { get; set; }
    [Display(Name = "AgentVersionId"), Description("Agent 版本标识"), SugarColumn(IsNullable = true)] public Guid? AgentVersionId { get; set; }
    [Display(Name = "McpServerId"), Description("MCP Server 标识"), SugarColumn(IsNullable = true)] public Guid? McpServerId { get; set; }
    [Display(Name = "ToolVersionId"), Description("工具版本标识"), SugarColumn(IsNullable = true)] public Guid? ToolVersionId { get; set; }
    [Display(Name = "ToolName"), Description("工具名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string ToolName { get; set; }
    [Display(Name = "Risk"), Description("工具风险等级"), SugarColumn(IsNullable = true)] public int? Risk { get; set; }
    [Display(Name = "ToolSchemaSha256"), Description("工具 Schema SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ToolSchemaSha256 { get; set; }
    [Display(Name = "ArgumentsSha256"), Description("调用参数 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ArgumentsSha256 { get; set; }
    [Display(Name = "SafeArgumentsSummaryJson"), Description("安全参数摘要 JSON"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string SafeArgumentsSummaryJson { get; set; }
    [Display(Name = "Status"), Description("审批状态"), SugarColumn(IsNullable = true)] public int? Status { get; set; }
    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)] public long? LogicalRevision { get; set; }
    [Display(Name = "RequestedAtUtc"), Description("申请时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? RequestedAtUtc { get; set; }
    [Display(Name = "ExpiresAtUtc"), Description("过期时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? ExpiresAtUtc { get; set; }
    [Display(Name = "DecisionUserId"), Description("决策用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string DecisionUserId { get; set; }
    [Display(Name = "DecisionReason"), Description("决策原因"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string DecisionReason { get; set; }
    [Display(Name = "DecidedAtUtc"), Description("决策时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? DecidedAtUtc { get; set; }
    [Display(Name = "ClaimedAtUtc"), Description("执行领取时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? ClaimedAtUtc { get; set; }
    [Display(Name = "FinishedAtUtc"), Description("完成时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
    [Display(Name = "ErrorCode"), Description("错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string ErrorCode { get; set; }
}

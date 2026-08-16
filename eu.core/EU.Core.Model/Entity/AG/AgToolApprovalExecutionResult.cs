namespace EU.Core.Model.Entity;

/// <summary>工具调用审批执行结果表。</summary>
[SugarTable("AgToolApprovalExecutionResult", "工具调用审批执行结果表"), Entity(TableCnName = "工具调用审批执行结果表", TableName = "AgToolApprovalExecutionResult")]
public class AgToolApprovalExecutionResult : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "ApprovalId"), Description("审批请求标识"), SugarColumn(IsNullable = true)] public Guid? ApprovalId { get; set; }
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string TenantId { get; set; }
    [Display(Name = "Succeeded"), Description("执行成功标记"), SugarColumn(IsNullable = true)] public bool? Succeeded { get; set; }
    [Display(Name = "Blocked"), Description("执行阻止标记"), SugarColumn(IsNullable = true)] public bool? Blocked { get; set; }
    [Display(Name = "ProtectedContent"), Description("受保护的执行结果"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string ProtectedContent { get; set; }
    [Display(Name = "ProtectedContentSha256"), Description("受保护结果 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ProtectedContentSha256 { get; set; }
    [Display(Name = "ContentSha256"), Description("明文结果 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ContentSha256 { get; set; }
    [Display(Name = "ErrorCode"), Description("错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string ErrorCode { get; set; }
    [Display(Name = "FinishedAtUtc"), Description("完成时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
}

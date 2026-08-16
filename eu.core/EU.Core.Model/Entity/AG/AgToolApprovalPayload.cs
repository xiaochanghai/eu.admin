namespace EU.Core.Model.Entity;

/// <summary>工具调用审批加密载荷表。</summary>
[SugarTable("AgToolApprovalPayload", "工具调用审批加密载荷表"), Entity(TableCnName = "工具调用审批加密载荷表", TableName = "AgToolApprovalPayload")]
public class AgToolApprovalPayload : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "ApprovalId"), Description("审批请求标识"), SugarColumn(IsNullable = true)] public Guid? ApprovalId { get; set; }
    [Display(Name = "ProtectedPayload"), Description("受保护的恢复载荷"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string ProtectedPayload { get; set; }
    [Display(Name = "ProtectedPayloadSha256"), Description("受保护载荷 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ProtectedPayloadSha256 { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>工具调用审批决策历史表。</summary>
[SugarTable("AgToolApprovalDecision", "工具调用审批决策历史表"), Entity(TableCnName = "工具调用审批决策历史表", TableName = "AgToolApprovalDecision")]
public class AgToolApprovalDecision : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "ApprovalId"), Description("审批请求标识"), SugarColumn(IsNullable = true)] public Guid? ApprovalId { get; set; }
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string TenantId { get; set; }
    [Display(Name = "FromStatus"), Description("原状态"), SugarColumn(IsNullable = true)] public int? FromStatus { get; set; }
    [Display(Name = "ToStatus"), Description("目标状态"), SugarColumn(IsNullable = true)] public int? ToStatus { get; set; }
    [Display(Name = "DecisionUserId"), Description("决策用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string DecisionUserId { get; set; }
    [Display(Name = "DecisionReason"), Description("决策原因"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string DecisionReason { get; set; }
    [Display(Name = "DecidedAtUtc"), Description("决策时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? DecidedAtUtc { get; set; }
    [Display(Name = "ResultingLogicalRevision"), Description("决策后逻辑修订号"), SugarColumn(IsNullable = true)] public long? ResultingLogicalRevision { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>Agent 对话会话表。</summary>
[SugarTable("AgChatConversation", "Agent 对话会话表"), Entity(TableCnName = "Agent 对话会话表", TableName = "AgChatConversation")]
public class AgChatConversation : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public new string CurrentNode { get; set; }
    [Display(Name = "Title"), Description("会话标题"), Column(TypeName = "varchar(512)"), SugarColumn(IsNullable = true, Length = 512)] public string Title { get; set; }
    [Display(Name = "CreatedAtUtc"), Description("创建时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? CreatedAtUtc { get; set; }
    [Display(Name = "UpdatedAtUtc"), Description("更新时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? UpdatedAtUtc { get; set; }
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string TenantId { get; set; }
    [Display(Name = "UserId"), Description("用户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string UserId { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>Agent 对话消息表。</summary>
[SugarTable("AgChatMessage", "Agent 对话消息表"), Entity(TableCnName = "Agent 对话消息表", TableName = "AgChatMessage")]
public class AgChatMessage : BasePoco
{
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public new string CurrentNode { get; set; }
    [Display(Name = "ConversationId"), Description("会话标识"), SugarColumn(IsNullable = true)] public Guid? ConversationId { get; set; }
    [Display(Name = "Ordinal"), Description("消息顺序号"), SugarColumn(IsNullable = true)] public long? Ordinal { get; set; }
    [Display(Name = "Role"), Description("消息角色"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)] public string Role { get; set; }
    [Display(Name = "Content"), Description("消息内容"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string Content { get; set; }
    [Display(Name = "ContentSha256"), Description("消息内容 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string ContentSha256 { get; set; }
    [Display(Name = "ContentUtf8Bytes"), Description("消息内容 UTF-8 字节数"), SugarColumn(IsNullable = true)] public long? ContentUtf8Bytes { get; set; }
    [Display(Name = "CreatedAtUtc"), Description("消息创建时间（UTC）"), SugarColumn(IsNullable = true)] public DateTime? CreatedAtUtc { get; set; }
    [Display(Name = "Kind"), Description("消息类型"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string Kind { get; set; }
    [Display(Name = "BusinessQueryId"), Description("业务查询标识"), SugarColumn(IsNullable = true)] public Guid? BusinessQueryId { get; set; }
    [Display(Name = "BusinessReceiptJson"), Description("业务查询回执 JSON"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string BusinessReceiptJson { get; set; }
    [Display(Name = "BusinessPresentationJson"), Description("业务查询展示数据 JSON"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string BusinessPresentationJson { get; set; }
    [Display(Name = "BusinessIntegritySha256"), Description("业务查询完整性 SHA-256"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string BusinessIntegritySha256 { get; set; }
}

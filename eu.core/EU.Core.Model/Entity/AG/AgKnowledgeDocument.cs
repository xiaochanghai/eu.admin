namespace EU.Core.Model.Entity;

/// <summary>
/// 知识库文档表。
/// </summary>
[SugarTable("AgKnowledgeDocument", "知识库文档表"), Entity(TableCnName = "知识库文档表", TableName = "AgKnowledgeDocument")]
public class AgKnowledgeDocument : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "KnowledgeBaseId"), Description("所属知识库主键"), SugarColumn(IsNullable = true)]
    public Guid? KnowledgeBaseId { get; set; }

    [Display(Name = "Ordinal"), Description("文档排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "FileName"), Description("文件名"), Column(TypeName = "varchar(512)"), SugarColumn(IsNullable = true, Length = 512)]
    public string FileName { get; set; }

    [Display(Name = "MediaType"), Description("媒体类型"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string MediaType { get; set; }

    [Display(Name = "Sha256"), Description("文档 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string Sha256 { get; set; }

    [Display(Name = "Content"), Description("提取后的文档正文"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Content { get; set; }

    [Display(Name = "ImportedAtUtc"), Description("导入 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? ImportedAtUtc { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>
/// 知识库检索分块表。
/// </summary>
[SugarTable("AgKnowledgeChunk", "知识库检索分块表"), Entity(TableCnName = "知识库检索分块表", TableName = "AgKnowledgeChunk")]
public class AgKnowledgeChunk : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "KnowledgeBaseId"), Description("所属知识库主键"), SugarColumn(IsNullable = true)]
    public Guid? KnowledgeBaseId { get; set; }

    [Display(Name = "DocumentId"), Description("所属文档主键"), SugarColumn(IsNullable = true)]
    public Guid? DocumentId { get; set; }

    [Display(Name = "Sequence"), Description("文档内分块序号"), SugarColumn(IsNullable = true)]
    public int? Sequence { get; set; }

    [Display(Name = "Content"), Description("分块正文"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Content { get; set; }
}

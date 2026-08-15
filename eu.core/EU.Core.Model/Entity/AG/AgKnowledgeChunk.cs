namespace EU.Core.Model.Entity;

/// <summary>
/// 知识库检索分块表。
/// </summary>
[SugarTable("AgKnowledgeChunk", "知识库检索分块表"), Entity(TableCnName = "知识库检索分块表", TableName = "AgKnowledgeChunk")]
public class AgKnowledgeChunk : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属知识库主键
    /// </summary>
    [Display(Name = "KnowledgeBaseId"), Description("所属知识库主键"), SugarColumn(IsNullable = true)]
    public Guid? KnowledgeBaseId { get; set; }

    /// <summary>
    /// 所属文档主键
    /// </summary>
    [Display(Name = "DocumentId"), Description("所属文档主键"), SugarColumn(IsNullable = true)]
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// 文档内分块序号
    /// </summary>
    [Display(Name = "Sequence"), Description("文档内分块序号"), SugarColumn(IsNullable = true)]
    public int? Sequence { get; set; }

    /// <summary>
    /// 分块正文
    /// </summary>
    [Display(Name = "Content"), Description("分块正文"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Content { get; set; }
}

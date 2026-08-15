namespace EU.Core.Model.Entity;

/// <summary>
/// 评测套件草稿和发布版本表。
/// </summary>
[SugarTable("AgEvaluationSuiteVersion", "评测套件草稿和发布版本表"), Entity(TableCnName = "评测套件草稿和发布版本表", TableName = "AgEvaluationSuiteVersion")]
public class AgEvaluationSuiteVersion : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "SuiteId"), Description("所属评测套件主键"), SugarColumn(IsNullable = true)]
    public Guid? SuiteId { get; set; }

    [Display(Name = "Ordinal"), Description("版本排列顺序；草稿固定为 0"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "Label"), Description("版本标签"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Label { get; set; }

    [Display(Name = "IsDraft"), Description("是否为草稿版本"), SugarColumn(IsNullable = true)]
    public bool? IsDraft { get; set; }

    [Display(Name = "ContentSha256"), Description("版本内容摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ContentSha256 { get; set; }

    [Display(Name = "PublishedAtUtc"), Description("发布 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? PublishedAtUtc { get; set; }

    [Display(Name = "PublishedByUserId"), Description("发布用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string PublishedByUserId { get; set; }
}

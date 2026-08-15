namespace EU.Core.Model.Entity;

/// <summary>
/// 评测用例有序规则表。
/// </summary>
[SugarTable("AgEvaluationCaseRule", "评测用例有序规则表"), Entity(TableCnName = "评测用例有序规则表", TableName = "AgEvaluationCaseRule")]
public class AgEvaluationCaseRule : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属评测套件主键
    /// </summary>
    [Display(Name = "SuiteId"), Description("所属评测套件主键"), SugarColumn(IsNullable = true)]
    public Guid? SuiteId { get; set; }

    /// <summary>
    /// 所属套件版本主键
    /// </summary>
    [Display(Name = "VersionId"), Description("所属套件版本主键"), SugarColumn(IsNullable = true)]
    public Guid? VersionId { get; set; }

    /// <summary>
    /// 所属用例行主键
    /// </summary>
    [Display(Name = "EvaluationCaseId"), Description("所属用例行主键"), SugarColumn(IsNullable = true)]
    public Guid? EvaluationCaseId { get; set; }

    /// <summary>
    /// 规则类型
    /// </summary>
    [Display(Name = "RuleType"), Description("规则类型"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string RuleType { get; set; }

    /// <summary>
    /// 同类型规则排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("同类型规则排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 规则内容
    /// </summary>
    [Display(Name = "Value"), Description("规则内容"), Column(TypeName = "varchar(512)"), SugarColumn(IsNullable = true, Length = 512)]
    public string Value { get; set; }
}

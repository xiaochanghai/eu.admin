namespace EU.Core.Model.Entity;

/// <summary>
/// 模型评审用例指标结果表 (Model)
/// </summary>
[SugarTable("AgEvaluationModelJudgementMetric", "模型评审用例指标结果表"), Entity(TableCnName = "模型评审用例指标结果表", TableName = "AgEvaluationModelJudgementMetric")]
public class AgEvaluationModelJudgementMetric : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属模型评审报告标识
    /// </summary>
    [Display(Name = "JudgementId"), Description("所属模型评审报告标识"), SugarColumn(IsNullable = true)]
    public Guid? JudgementId { get; set; }

    /// <summary>
    /// 所属模型评审用例记录标识
    /// </summary>
    [Display(Name = "JudgementCaseId"), Description("所属模型评审用例记录标识"), SugarColumn(IsNullable = true)]
    public Guid? JudgementCaseId { get; set; }

    /// <summary>
    /// 指标排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("指标排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 评估指标名称
    /// </summary>
    [Display(Name = "Name"), Description("评估指标名称"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Name { get; set; }

    /// <summary>
    /// 模型评审实际得分
    /// </summary>
    [Display(Name = "Score"), Description("模型评审实际得分"), Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? Score { get; set; }

    /// <summary>
    /// 指标最低通过分数
    /// </summary>
    [Display(Name = "MinimumScore"), Description("指标最低通过分数"), Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? MinimumScore { get; set; }

    /// <summary>
    /// 指标是否通过
    /// </summary>
    [Display(Name = "Passed"), Description("指标是否通过"), SugarColumn(IsNullable = true)]
    public bool? Passed { get; set; }
}

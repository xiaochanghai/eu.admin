namespace EU.Core.Model.Entity;

/// <summary>
/// 模型评审报告使用的有序评估器表 (Model)
/// </summary>
[SugarTable("AgEvaluationModelJudgementEvaluator", "模型评审报告使用的有序评估器表"), Entity(TableCnName = "模型评审报告使用的有序评估器表", TableName = "AgEvaluationModelJudgementEvaluator")]
public class AgEvaluationModelJudgementEvaluator : BasePoco
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
    /// 评估器排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("评估器排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 评估器名称
    /// </summary>
    [Display(Name = "Name"), Description("评估器名称"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Name { get; set; }
}

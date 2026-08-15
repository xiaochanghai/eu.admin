namespace EU.Core.Model.Entity;

/// <summary>
/// 模型评审指标诊断码表 (Model)
/// </summary>
[SugarTable("AgEvaluationModelJudgementDiagnostic", "模型评审指标诊断码表"), Entity(TableCnName = "模型评审指标诊断码表", TableName = "AgEvaluationModelJudgementDiagnostic")]
public class AgEvaluationModelJudgementDiagnostic : BasePoco
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
    /// 所属模型评审指标记录标识
    /// </summary>
    [Display(Name = "JudgementMetricId"), Description("所属模型评审指标记录标识"), SugarColumn(IsNullable = true)]
    public Guid? JudgementMetricId { get; set; }

    /// <summary>
    /// 诊断码排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("诊断码排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 模型评审诊断码
    /// </summary>
    [Display(Name = "Code"), Description("模型评审诊断码"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Code { get; set; }
}

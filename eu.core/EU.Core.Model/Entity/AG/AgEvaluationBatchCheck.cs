namespace EU.Core.Model.Entity;

/// <summary>
/// 用例评估报告的有序断言检查项表 (Model)
/// </summary>
[SugarTable("AgEvaluationBatchCheck", "用例评估报告的有序断言检查项表"), Entity(TableCnName = "用例评估报告的有序断言检查项表", TableName = "AgEvaluationBatchCheck")]
public class AgEvaluationBatchCheck : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属评估批次标识
    /// </summary>
    [Display(Name = "BatchId"), Description("所属评估批次标识"), SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

    /// <summary>
    /// 所属评估批次用例记录标识
    /// </summary>
    [Display(Name = "BatchCaseId"), Description("所属评估批次用例记录标识"), SugarColumn(IsNullable = true)]
    public Guid? BatchCaseId { get; set; }

    /// <summary>
    /// 检查项在报告中的顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("检查项在报告中的顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 检查项类型编码
    /// </summary>
    [Display(Name = "Code"), Description("检查项类型编码"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string Code { get; set; }

    /// <summary>
    /// 检查项是否通过
    /// </summary>
    [Display(Name = "Passed"), Description("检查项是否通过"), SugarColumn(IsNullable = true)]
    public bool? Passed { get; set; }

    /// <summary>
    /// 检查项的预期值或预期条件
    /// </summary>
    [Display(Name = "Expected"), Description("检查项的预期值或预期条件"), Column(TypeName = "varchar(1024)"), SugarColumn(IsNullable = true, Length = 1024)]
    public string Expected { get; set; }

    /// <summary>
    /// 检查项的实际值或实际结果
    /// </summary>
    [Display(Name = "Actual"), Description("检查项的实际值或实际结果"), Column(TypeName = "varchar(1024)"), SugarColumn(IsNullable = true, Length = 1024)]
    public string Actual { get; set; }
}

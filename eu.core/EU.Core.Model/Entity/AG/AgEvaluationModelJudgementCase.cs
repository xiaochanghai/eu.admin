namespace EU.Core.Model.Entity;

/// <summary>
/// 模型评审用例结果表 (Model)
/// </summary>
[SugarTable("AgEvaluationModelJudgementCase", "模型评审用例结果表"), Entity(TableCnName = "模型评审用例结果表", TableName = "AgEvaluationModelJudgementCase")]
public class AgEvaluationModelJudgementCase : BasePoco
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
    /// 用例排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("用例排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 评估套件中的用例标识
    /// </summary>
    [Display(Name = "CaseId"), Description("评估套件中的用例标识"), SugarColumn(IsNullable = true)]
    public Guid? CaseId { get; set; }

    /// <summary>
    /// 执行时记录的用例名称
    /// </summary>
    [Display(Name = "CaseName"), Description("执行时记录的用例名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string CaseName { get; set; }

    /// <summary>
    /// 关联的统一运行标识
    /// </summary>
    [Display(Name = "UnifiedRunId"), Description("关联的统一运行标识"), SugarColumn(IsNullable = true)]
    public Guid? UnifiedRunId { get; set; }

    /// <summary>
    /// 用例输入内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "InputSha256"), Description("用例输入内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    /// <summary>
    /// 用例输出内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "OutputSha256"), Description("用例输出内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutputSha256 { get; set; }
}

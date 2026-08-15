namespace EU.Core.Model.Entity;

/// <summary>
/// 评测套件版本用例表。
/// </summary>
[SugarTable("AgEvaluationCase", "评测套件版本用例表"), Entity(TableCnName = "评测套件版本用例表", TableName = "AgEvaluationCase")]
public class AgEvaluationCase : BasePoco
{
    [Display(Name = "当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Display(Name = "SuiteId"), Description("所属评测套件主键"), SugarColumn(IsNullable = true)]
    public Guid? SuiteId { get; set; }

    [Display(Name = "VersionId"), Description("所属套件版本主键"), SugarColumn(IsNullable = true)]
    public Guid? VersionId { get; set; }

    [Display(Name = "Ordinal"), Description("用例排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "CaseId"), Description("契约内用例标识"), SugarColumn(IsNullable = true)]
    public Guid? CaseId { get; set; }

    [Display(Name = "Name"), Description("用例名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    [Display(Name = "Input"), Description("用例输入"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Input { get; set; }

    [Display(Name = "TargetAgentId"), Description("目标 Agent 主键"), SugarColumn(IsNullable = true)]
    public Guid? TargetAgentId { get; set; }

    [Display(Name = "TargetAgentVersionId"), Description("目标 Agent 发布版本主键"), SugarColumn(IsNullable = true)]
    public Guid? TargetAgentVersionId { get; set; }

    [Display(Name = "ExpectedStatus"), Description("预期运行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string ExpectedStatus { get; set; }

    [Display(Name = "MaximumToolCalls"), Description("最大工具调用数"), SugarColumn(IsNullable = true)]
    public int? MaximumToolCalls { get; set; }

    [Display(Name = "MaximumDurationMilliseconds"), Description("最大运行毫秒数"), SugarColumn(IsNullable = true)]
    public long? MaximumDurationMilliseconds { get; set; }
}

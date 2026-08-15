namespace EU.Core.Model.Entity;

/// <summary>
/// 评测套件版本用例表。
/// </summary>
[SugarTable("AgEvaluationCase", "评测套件版本用例表"), Entity(TableCnName = "评测套件版本用例表", TableName = "AgEvaluationCase")]
public class AgEvaluationCase : BasePoco
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
    /// 用例排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("用例排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 契约内用例标识
    /// </summary>
    [Display(Name = "CaseId"), Description("契约内用例标识"), SugarColumn(IsNullable = true)]
    public Guid? CaseId { get; set; }

    /// <summary>
    /// 用例名称
    /// </summary>
    [Display(Name = "Name"), Description("用例名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// 用例输入
    /// </summary>
    [Display(Name = "Input"), Description("用例输入"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Input { get; set; }

    /// <summary>
    /// 目标 Agent 主键
    /// </summary>
    [Display(Name = "TargetAgentId"), Description("目标 Agent 主键"), SugarColumn(IsNullable = true)]
    public Guid? TargetAgentId { get; set; }

    /// <summary>
    /// 目标 Agent 发布版本主键
    /// </summary>
    [Display(Name = "TargetAgentVersionId"), Description("目标 Agent 发布版本主键"), SugarColumn(IsNullable = true)]
    public Guid? TargetAgentVersionId { get; set; }

    /// <summary>
    /// 预期运行状态
    /// </summary>
    [Display(Name = "ExpectedStatus"), Description("预期运行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string ExpectedStatus { get; set; }

    /// <summary>
    /// 最大工具调用数
    /// </summary>
    [Display(Name = "MaximumToolCalls"), Description("最大工具调用数"), SugarColumn(IsNullable = true)]
    public int? MaximumToolCalls { get; set; }

    /// <summary>
    /// 最大运行毫秒数
    /// </summary>
    [Display(Name = "MaximumDurationMilliseconds"), Description("最大运行毫秒数"), SugarColumn(IsNullable = true)]
    public long? MaximumDurationMilliseconds { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>
/// 评估批次用例执行结果及评估报告汇总表 (Model)
/// </summary>
[SugarTable("AgEvaluationBatchCase", "评估批次用例执行结果及评估报告汇总表"), Entity(TableCnName = "评估批次用例执行结果及评估报告汇总表", TableName = "AgEvaluationBatchCase")]
public class AgEvaluationBatchCase : BasePoco
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
    /// 用例执行顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("用例执行顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 评估套件中的用例标识
    /// </summary>
    [Display(Name = "CaseId"), Description("评估套件中的用例标识"), SugarColumn(IsNullable = true)]
    public Guid? CaseId { get; set; }

    /// <summary>
    /// 执行时记录的用例显示名称
    /// </summary>
    [Display(Name = "CaseName"), Description("执行时记录的用例显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string CaseName { get; set; }

    /// <summary>
    /// 目标 Agent 标识
    /// </summary>
    [Display(Name = "TargetAgentId"), Description("目标 Agent 标识"), SugarColumn(IsNullable = true)]
    public Guid? TargetAgentId { get; set; }

    /// <summary>
    /// 目标 Agent 已发布版本标识
    /// </summary>
    [Display(Name = "TargetAgentVersionId"), Description("目标 Agent 已发布版本标识"), SugarColumn(IsNullable = true)]
    public Guid? TargetAgentVersionId { get; set; }

    /// <summary>
    /// 用例执行状态
    /// </summary>
    [Display(Name = "Status"), Description("用例执行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 关联的统一运行标识
    /// </summary>
    [Display(Name = "UnifiedRunId"), Description("关联的统一运行标识"), SugarColumn(IsNullable = true)]
    public Guid? UnifiedRunId { get; set; }

    /// <summary>
    /// 统一运行的实际状态
    /// </summary>
    [Display(Name = "UnifiedRunStatus"), Description("统一运行的实际状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string UnifiedRunStatus { get; set; }

    /// <summary>
    /// 用例级错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("用例级错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }

    /// <summary>
    /// 实际运行耗时（毫秒）
    /// </summary>
    [Display(Name = "DurationMilliseconds"), Description("实际运行耗时（毫秒）"), SugarColumn(IsNullable = true)]
    public long? DurationMilliseconds { get; set; }

    /// <summary>
    /// 实际工具调用次数
    /// </summary>
    [Display(Name = "ToolCallCount"), Description("实际工具调用次数"), SugarColumn(IsNullable = true)]
    public int? ToolCallCount { get; set; }

    /// <summary>
    /// 断言报告评估时间（UTC）
    /// </summary>
    [Display(Name = "ReportEvaluatedAtUtc"), Description("断言报告评估时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? ReportEvaluatedAtUtc { get; set; }

    /// <summary>
    /// 断言报告是否通过
    /// </summary>
    [Display(Name = "ReportPassed"), Description("断言报告是否通过"), SugarColumn(IsNullable = true)]
    public bool? ReportPassed { get; set; }

    /// <summary>
    /// 断言报告得分
    /// </summary>
    [Display(Name = "ReportScore"), Description("断言报告得分"), Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? ReportScore { get; set; }

    /// <summary>
    /// 运行输出内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "OutputSha256"), Description("运行输出内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutputSha256 { get; set; }

    /// <summary>
    /// 运行输出内容的 UTF-8 字节数
    /// </summary>
    [Display(Name = "OutputUtf8Bytes"), Description("运行输出内容的 UTF-8 字节数"), SugarColumn(IsNullable = true)]
    public int? OutputUtf8Bytes { get; set; }
}

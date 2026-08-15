namespace EU.Core.Model.Entity;

/// <summary>
/// 评估批次模型评审报告主表 (Model)
/// </summary>
[SugarTable("AgEvaluationModelJudgement", "评估批次模型评审报告主表"), Entity(TableCnName = "评估批次模型评审报告主表", TableName = "AgEvaluationModelJudgement")]
public class AgEvaluationModelJudgement : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    /// <summary>
    /// 发起模型评审的用户标识
    /// </summary>
    [Display(Name = "RequestedByUserId"), Description("发起模型评审的用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string RequestedByUserId { get; set; }

    /// <summary>
    /// 所属评估批次标识
    /// </summary>
    [Display(Name = "BatchId"), Description("所属评估批次标识"), SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

    /// <summary>
    /// 评估套件标识
    /// </summary>
    [Display(Name = "SuiteId"), Description("评估套件标识"), SugarColumn(IsNullable = true)]
    public Guid? SuiteId { get; set; }

    /// <summary>
    /// 已发布评估套件版本标识
    /// </summary>
    [Display(Name = "SuiteVersionId"), Description("已发布评估套件版本标识"), SugarColumn(IsNullable = true)]
    public Guid? SuiteVersionId { get; set; }

    /// <summary>
    /// 已发布评估套件版本内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "SuiteVersionContentSha256"), Description("已发布评估套件版本内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string SuiteVersionContentSha256 { get; set; }

    /// <summary>
    /// 模型评审引擎提供方
    /// </summary>
    [Display(Name = "Provider"), Description("模型评审引擎提供方"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Provider { get; set; }

    /// <summary>
    /// 模型评审组件包版本
    /// </summary>
    [Display(Name = "PackageVersion"), Description("模型评审组件包版本"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string PackageVersion { get; set; }

    /// <summary>
    /// 执行评审使用的模型配置标识
    /// </summary>
    [Display(Name = "ModelProfileId"), Description("执行评审使用的模型配置标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string ModelProfileId { get; set; }

    /// <summary>
    /// 模型评审配置的 SHA-256 摘要，用于防止相同配置重复评审
    /// </summary>
    [Display(Name = "ConfigurationSha256"), Description("模型评审配置的 SHA-256 摘要，用于防止相同配置重复评审"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ConfigurationSha256 { get; set; }

    /// <summary>
    /// 模型评审提示词版本
    /// </summary>
    [Display(Name = "PromptVersion"), Description("模型评审提示词版本"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string PromptVersion { get; set; }

    /// <summary>
    /// 模型评审开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("模型评审开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// 模型评审结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("模型评审结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 模型评审建议结果是否通过
    /// </summary>
    [Display(Name = "AdvisoryPassed"), Description("模型评审建议结果是否通过"), SugarColumn(IsNullable = true)]
    public bool? AdvisoryPassed { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>
/// 评估批次执行汇总表 (Model)
/// </summary>
[SugarTable("AgEvaluationBatch", "评估批次执行汇总表"), Entity(TableCnName = "评估批次执行汇总表", TableName = "AgEvaluationBatch")]
public class AgEvaluationBatch : BasePoco
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
    /// 发起评估批次的用户标识
    /// </summary>
    [Display(Name = "RequestedByUserId"), Description("发起评估批次的用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string RequestedByUserId { get; set; }

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
    /// 评估批次执行状态
    /// </summary>
    [Display(Name = "Status"), Description("评估批次执行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 用于乐观并发控制的逻辑修订号
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("用于乐观并发控制的逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 评估批次开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("评估批次开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// 评估批次结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("评估批次结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 评估批次级错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("评估批次级错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

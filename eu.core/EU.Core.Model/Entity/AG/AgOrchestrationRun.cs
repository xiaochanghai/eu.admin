namespace EU.Core.Model.Entity;

/// <summary>
/// 编排运行汇总表 (Model)
/// </summary>
[SugarTable("AgOrchestrationRun", "编排运行汇总表"), Entity(TableCnName = "编排运行汇总表", TableName = "AgOrchestrationRun")]
public class AgOrchestrationRun : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属编排标识
    /// </summary>
    [Display(Name = "OrchestrationId"), Description("所属编排标识"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    /// <summary>
    /// 执行使用的已发布编排版本标识
    /// </summary>
    [Display(Name = "OrchestrationVersionId"), Description("执行使用的已发布编排版本标识"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationVersionId { get; set; }

    /// <summary>
    /// 执行时记录的编排编码
    /// </summary>
    [Display(Name = "OrchestrationCode"), Description("执行时记录的编排编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string OrchestrationCode { get; set; }

    /// <summary>
    /// 编排运行状态
    /// </summary>
    [Display(Name = "Status"), Description("编排运行状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 编排运行开始时间（UTC）
    /// </summary>
    [Display(Name = "StartedAtUtc"), Description("编排运行开始时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// 编排运行结束时间（UTC）
    /// </summary>
    [Display(Name = "FinishedAtUtc"), Description("编排运行结束时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>
    /// 编排输入内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "InputSha256"), Description("编排输入内容的 SHA-256 摘要"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    /// <summary>
    /// 编排运行错误码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("编排运行错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>
/// 编排运行输入输出明细表 (Model)
/// </summary>
[SugarTable("AgOrchestrationRunDetail", "编排运行输入输出明细表"), Entity(TableCnName = "编排运行输入输出明细表", TableName = "AgOrchestrationRunDetail")]
public class AgOrchestrationRunDetail : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属编排运行标识
    /// </summary>
    [Display(Name = "RunId"), Description("所属编排运行标识"), SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    /// <summary>
    /// 所属编排标识
    /// </summary>
    [Display(Name = "OrchestrationId"), Description("所属编排标识"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    /// <summary>
    /// 编排原始输入内容
    /// </summary>
    [Display(Name = "InputText"), Description("编排原始输入内容"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputText { get; set; }

    /// <summary>
    /// 编排最终输出内容
    /// </summary>
    [Display(Name = "OutputText"), Description("编排最终输出内容"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string OutputText { get; set; }
}

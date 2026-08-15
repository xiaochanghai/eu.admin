namespace EU.Core.Model.Entity;

/// <summary>
/// 用例运行期间记录的有序事件类型及路由观测表 (Model)
/// </summary>
[SugarTable("AgEvaluationBatchObservation", "用例运行期间记录的有序事件类型及路由观测表"), Entity(TableCnName = "用例运行期间记录的有序事件类型及路由观测表", TableName = "AgEvaluationBatchObservation")]
public class AgEvaluationBatchObservation : BasePoco
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
    /// 观测类型：EventKind（事件类型）或 Route（路由）
    /// </summary>
    [Display(Name = "ObservationType"), Description("观测类型：EventKind（事件类型）或 Route（路由）"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string ObservationType { get; set; }

    /// <summary>
    /// 同类观测记录的排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("同类观测记录的排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 观测到的事件类型或路由值
    /// </summary>
    [Display(Name = "Value"), Description("观测到的事件类型或路由值"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Value { get; set; }
}

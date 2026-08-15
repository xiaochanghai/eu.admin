namespace EU.Core.Model.Entity;

/// <summary>
/// 编排发布版本的 Agent 版本绑定表。
/// </summary>
[SugarTable("AgOrchestrationAgentBinding", "编排发布版本的 Agent 版本绑定表"), Entity(TableCnName = "编排发布版本的 Agent 版本绑定表", TableName = "AgOrchestrationAgentBinding")]
public class AgOrchestrationAgentBinding : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 所属编排主键
    /// </summary>
    [Display(Name = "OrchestrationId"), Description("所属编排主键"), SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    /// <summary>
    /// 所属发布版本主键
    /// </summary>
    [Display(Name = "VersionId"), Description("所属发布版本主键"), SugarColumn(IsNullable = true)]
    public Guid? VersionId { get; set; }

    /// <summary>
    /// 绑定排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("绑定排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 绑定的 Agent 主键
    /// </summary>
    [Display(Name = "AgentId"), Description("绑定的 Agent 主键"), SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// 绑定的 Agent 发布版本主键
    /// </summary>
    [Display(Name = "AgentVersionId"), Description("绑定的 Agent 发布版本主键"), SugarColumn(IsNullable = true)]
    public Guid? AgentVersionId { get; set; }
}

namespace EU.Core.Model.Entity;

/// <summary>
/// 主 Agent 指派表 (Model)
/// </summary>
[SugarTable("AgMainAgentAssignment", "主 Agent 指派表"), Entity(TableCnName = "主 Agent 指派表", TableName = "AgMainAgentAssignment")]
public class AgMainAgentAssignment : BasePoco
{
    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>
    /// 主 Agent 指派键
    /// </summary>
    [Display(Name = "AssignmentKey"), Description("主 Agent 指派键"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string AssignmentKey { get; set; }

    /// <summary>
    /// 指派的 Agent 标识
    /// </summary>
    [Display(Name = "AgentId"), Description("指派的 Agent 标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// 指派的已发布 Agent 版本标识
    /// </summary>
    [Display(Name = "AgentVersionId"), Description("指派的已发布 Agent 版本标识"), SugarColumn(IsNullable = true)]
    public Guid? AgentVersionId { get; set; }

    /// <summary>
    /// 逻辑修订号
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 指派更新时间（UTC）
    /// </summary>
    [Display(Name = "UpdatedAtUtc"), Description("指派更新时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAtUtc { get; set; }
}

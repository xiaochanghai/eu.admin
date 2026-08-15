namespace EU.Core.Model.Entity;

[SugarTable("AgMainAgentAssignment", "Main Agent assignment"), Entity(TableCnName = "Main Agent assignment", TableName = "AgMainAgentAssignment")]
public class AgMainAgentAssignment : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string AssignmentKey { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? AgentVersionId { get; set; }

    [SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAtUtc { get; set; }
}

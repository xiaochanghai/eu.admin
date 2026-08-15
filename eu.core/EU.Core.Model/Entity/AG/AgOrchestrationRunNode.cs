namespace EU.Core.Model.Entity;

[SugarTable("AgOrchestrationRunNode", "Orchestration run node"), Entity(TableCnName = "Orchestration run node", TableName = "AgOrchestrationRunNode")]
public class AgOrchestrationRunNode : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string NodeName { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? AgentId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? AgentVersionId { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Attempts { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? OutputCharacters { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

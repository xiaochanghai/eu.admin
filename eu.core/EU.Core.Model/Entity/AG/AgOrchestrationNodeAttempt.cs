namespace EU.Core.Model.Entity;

[SugarTable("AgOrchestrationNodeAttempt", "Orchestration node attempt"), Entity(TableCnName = "Orchestration node attempt", TableName = "AgOrchestrationNodeAttempt")]
public class AgOrchestrationNodeAttempt : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Attempt { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Sequence { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? AgentRunId { get; set; }

    [Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputText { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    [Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string OutputText { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutputSha256 { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

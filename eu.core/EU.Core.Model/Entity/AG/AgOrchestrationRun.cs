namespace EU.Core.Model.Entity;

[SugarTable("AgOrchestrationRun", "Orchestration run"), Entity(TableCnName = "Orchestration run", TableName = "AgOrchestrationRun")]
public class AgOrchestrationRun : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? OrchestrationVersionId { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string OrchestrationCode { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

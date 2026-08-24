namespace EU.Core.Model.Entity;

/// <summary>One execution attempt for a durable Agent task.</summary>
[SugarTable("AgAgentTaskAttempt", "Agent task attempt"), Entity(TableCnName = "Agent task attempt", TableName = "AgAgentTaskAttempt")]
public class AgAgentTaskAttempt : BasePoco
{
    [SugarColumn(IsNullable = true)] public Guid? TaskId { get; set; }
    [SugarColumn(IsNullable = true)] public int? AttemptNumber { get; set; }
    [SugarColumn(IsNullable = true)] public Guid? RunId { get; set; }
    [SugarColumn(IsNullable = true)] public int? Status { get; set; }
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string WorkerId { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? StartedAtUtc { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string ErrorCode { get; set; }
    [Column(TypeName = "nvarchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string ErrorMessage { get; set; }
}

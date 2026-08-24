namespace EU.Core.Model.Entity;

/// <summary>Append-only lifecycle event for a durable Agent task.</summary>
[SugarTable("AgAgentTaskEvent", "Agent task event"), Entity(TableCnName = "Agent task event", TableName = "AgAgentTaskEvent")]
public class AgAgentTaskEvent : BasePoco
{
    [SugarColumn(IsNullable = true)] public Guid? TaskId { get; set; }
    [SugarColumn(IsNullable = true)] public int? AttemptNumber { get; set; }
    [SugarColumn(IsNullable = true)] public Guid? RunId { get; set; }
    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string Kind { get; set; }
    [SugarColumn(IsNullable = true)] public int? Status { get; set; }
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string WorkerId { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? OccurredAtUtc { get; set; }
    [Column(TypeName = "nvarchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string PayloadJson { get; set; }
}

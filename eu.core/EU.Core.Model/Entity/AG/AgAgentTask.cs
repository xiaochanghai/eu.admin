namespace EU.Core.Model.Entity;

/// <summary>Durable Agent task.</summary>
[SugarTable("AgAgentTask", "Agent task"), Entity(TableCnName = "Agent task", TableName = "AgAgentTask")]
public class AgAgentTask : BasePoco
{
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string TenantId { get; set; }
    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string UserId { get; set; }
    [Column(TypeName = "nvarchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string Title { get; set; }
    [Column(TypeName = "nvarchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string Description { get; set; }
    [Column(TypeName = "nvarchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string Input { get; set; }
    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string InputSha256 { get; set; }
    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string SourceType { get; set; }
    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)] public string SourceId { get; set; }
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string IdempotencyKey { get; set; }
    [SugarColumn(IsNullable = true)] public Guid? ConversationId { get; set; }
    [SugarColumn(IsNullable = true)] public Guid? CurrentRunId { get; set; }
    [SugarColumn(IsNullable = true)] public int? Status { get; set; }
    [SugarColumn(IsNullable = true)] public int? Priority { get; set; }
    [SugarColumn(IsNullable = true)] public int? AttemptCount { get; set; }
    [SugarColumn(IsNullable = true)] public int? MaximumAttempts { get; set; }
    [SugarColumn(IsNullable = true)] public long? LogicalRevision { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? AvailableAtUtc { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? StartedAtUtc { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? FinishedAtUtc { get; set; }
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string LeaseOwner { get; set; }
    [SugarColumn(IsNullable = true)] public DateTime? LeaseExpiresAtUtc { get; set; }
    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)] public string CheckpointKind { get; set; }
    [Column(TypeName = "nvarchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string CheckpointJson { get; set; }
    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)] public string LastErrorCode { get; set; }
    [Column(TypeName = "nvarchar(max)"), SugarColumn(IsNullable = true, Length = -1)] public string LastErrorMessage { get; set; }
}

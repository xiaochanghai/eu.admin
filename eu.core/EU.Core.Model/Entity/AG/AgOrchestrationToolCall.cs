namespace EU.Core.Model.Entity;

[SugarTable("AgOrchestrationToolCall", "Orchestration tool call"), Entity(TableCnName = "Orchestration tool call", TableName = "AgOrchestrationToolCall")]
public class AgOrchestrationToolCall : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? ToolCallId { get; set; }

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

    [SugarColumn(IsNullable = true)]
    public Guid? ToolVersionId { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string ToolName { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string ArgumentsJson { get; set; }

    [Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string ResultContent { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ResultSha256 { get; set; }

    [SugarColumn(IsNullable = true)]
    public long? ResultCharacters { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

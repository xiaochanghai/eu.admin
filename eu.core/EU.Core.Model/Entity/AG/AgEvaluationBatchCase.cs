namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationBatchCase", "Evaluation batch case"), Entity(TableCnName = "Evaluation batch case", TableName = "AgEvaluationBatchCase")]
public class AgEvaluationBatchCase : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? CaseId { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string CaseName { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? TargetAgentId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? TargetAgentVersionId { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? UnifiedRunId { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string UnifiedRunStatus { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }

    [SugarColumn(IsNullable = true)]
    public long? DurationMilliseconds { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? ToolCallCount { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? ReportEvaluatedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public bool? ReportPassed { get; set; }

    [Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? ReportScore { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutputSha256 { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? OutputUtf8Bytes { get; set; }
}

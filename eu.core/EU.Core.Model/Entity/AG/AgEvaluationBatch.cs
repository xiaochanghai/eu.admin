namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationBatch", "Evaluation batch"), Entity(TableCnName = "Evaluation batch", TableName = "AgEvaluationBatch")]
public class AgEvaluationBatch : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string RequestedByUserId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? SuiteId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? SuiteVersionId { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string SuiteVersionContentSha256 { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }
}

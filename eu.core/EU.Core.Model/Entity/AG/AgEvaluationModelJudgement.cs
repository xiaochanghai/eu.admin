namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationModelJudgement", "Evaluation model judgement"), Entity(TableCnName = "Evaluation model judgement", TableName = "AgEvaluationModelJudgement")]
public class AgEvaluationModelJudgement : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string RequestedByUserId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? SuiteId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? SuiteVersionId { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string SuiteVersionContentSha256 { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Provider { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string PackageVersion { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string ModelProfileId { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ConfigurationSha256 { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string PromptVersion { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? StartedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAtUtc { get; set; }

    [SugarColumn(IsNullable = true)]
    public bool? AdvisoryPassed { get; set; }
}

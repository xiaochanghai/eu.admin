namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationModelJudgementMetric", "Evaluation model judgement metric"), Entity(TableCnName = "Evaluation model judgement metric", TableName = "AgEvaluationModelJudgementMetric")]
public class AgEvaluationModelJudgementMetric : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementCaseId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Name { get; set; }

    [Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? Score { get; set; }

    [Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? MinimumScore { get; set; }

    [SugarColumn(IsNullable = true)]
    public bool? Passed { get; set; }
}

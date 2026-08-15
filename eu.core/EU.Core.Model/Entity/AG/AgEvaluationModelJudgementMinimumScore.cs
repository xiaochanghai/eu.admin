namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationModelJudgementMinimumScore", "Evaluation model judgement minimum score"), Entity(TableCnName = "Evaluation model judgement minimum score", TableName = "AgEvaluationModelJudgementMinimumScore")]
public class AgEvaluationModelJudgementMinimumScore : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Name { get; set; }

    [Column(TypeName = "decimal(9,4)"), SugarColumn(IsNullable = true, DecimalDigits = 4)]
    public decimal? Score { get; set; }
}

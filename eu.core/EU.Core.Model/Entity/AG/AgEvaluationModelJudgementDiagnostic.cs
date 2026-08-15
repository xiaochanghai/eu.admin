namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationModelJudgementDiagnostic", "Evaluation model judgement diagnostic"), Entity(TableCnName = "Evaluation model judgement diagnostic", TableName = "AgEvaluationModelJudgementDiagnostic")]
public class AgEvaluationModelJudgementDiagnostic : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementMetricId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Code { get; set; }
}

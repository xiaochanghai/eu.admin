namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationModelJudgementEvaluator", "Evaluation model judgement evaluator"), Entity(TableCnName = "Evaluation model judgement evaluator", TableName = "AgEvaluationModelJudgementEvaluator")]
public class AgEvaluationModelJudgementEvaluator : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Name { get; set; }
}

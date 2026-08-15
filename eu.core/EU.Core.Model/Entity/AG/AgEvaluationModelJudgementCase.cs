namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationModelJudgementCase", "Evaluation model judgement case"), Entity(TableCnName = "Evaluation model judgement case", TableName = "AgEvaluationModelJudgementCase")]
public class AgEvaluationModelJudgementCase : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? JudgementId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? CaseId { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string CaseName { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? UnifiedRunId { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string InputSha256 { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutputSha256 { get; set; }
}

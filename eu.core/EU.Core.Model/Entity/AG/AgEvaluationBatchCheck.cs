namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationBatchCheck", "Evaluation batch assertion check"), Entity(TableCnName = "Evaluation batch assertion check", TableName = "AgEvaluationBatchCheck")]
public class AgEvaluationBatchCheck : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? BatchCaseId { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string Code { get; set; }

    [SugarColumn(IsNullable = true)]
    public bool? Passed { get; set; }

    [Column(TypeName = "varchar(1024)"), SugarColumn(IsNullable = true, Length = 1024)]
    public string Expected { get; set; }

    [Column(TypeName = "varchar(1024)"), SugarColumn(IsNullable = true, Length = 1024)]
    public string Actual { get; set; }
}

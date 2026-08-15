namespace EU.Core.Model.Entity;

[SugarTable("AgEvaluationBatchObservation", "Evaluation batch observation"), Entity(TableCnName = "Evaluation batch observation", TableName = "AgEvaluationBatchObservation")]
public class AgEvaluationBatchObservation : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? BatchCaseId { get; set; }

    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string ObservationType { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Value { get; set; }
}

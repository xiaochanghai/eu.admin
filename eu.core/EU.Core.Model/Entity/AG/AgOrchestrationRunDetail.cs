namespace EU.Core.Model.Entity;

[SugarTable("AgOrchestrationRunDetail", "Orchestration run detail"), Entity(TableCnName = "Orchestration run detail", TableName = "AgOrchestrationRunDetail")]
public class AgOrchestrationRunDetail : BasePoco
{
    [Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? RunId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? OrchestrationId { get; set; }

    [Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputText { get; set; }

    [Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string OutputText { get; set; }
}

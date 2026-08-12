namespace EU.Core.Model.Entity;

/// <summary>
/// Ordered resource binding for an Agent version or snapshot.
/// </summary>
[SugarTable("AgAgentVersionBinding", "Agent version binding"), Entity(TableCnName = "Agent version binding", TableName = "AgAgentVersionBinding")]
public class AgAgentVersionBinding : BasePoco
{
    [SugarColumn(IsNullable = false)]
    public Guid VersionId { get; set; }

    [SugarColumn(IsNullable = false, Length = 16)]
    public string Scope { get; set; }

    [SugarColumn(IsNullable = false, Length = 32)]
    public string BindingType { get; set; }

    [SugarColumn(IsNullable = false)]
    public int Ordinal { get; set; }

    [SugarColumn(IsNullable = false)]
    public Guid ReferenceId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? ReferenceVersionId { get; set; }

    [SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    [SugarColumn(IsNullable = true, Length = 128)]
    public string ReferenceCode { get; set; }

    [SugarColumn(IsNullable = true, Length = 256)]
    public string ReferenceName { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string ReferenceDescription { get; set; }
}

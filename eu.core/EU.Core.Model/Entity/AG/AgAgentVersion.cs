namespace EU.Core.Model.Entity;

/// <summary>
/// Agent Draft and published version.
/// </summary>
[SugarTable("AgAgentVersion", "Agent version"), Entity(TableCnName = "Agent version", TableName = "AgAgentVersion")]
public class AgAgentVersion : BasePoco
{
    [SugarColumn(IsNullable = false)]
    public Guid AgentId { get; set; }

    [SugarColumn(IsNullable = false)]
    public int Ordinal { get; set; }

    [SugarColumn(IsNullable = false, Length = 128)]
    public string Label { get; set; }

    [SugarColumn(IsNullable = false)]
    public bool IsDraft { get; set; }

    [SugarColumn(IsNullable = false, ColumnDataType = "nvarchar(max)")]
    public string Instructions { get; set; }

    [SugarColumn(IsNullable = false, Length = 256)]
    public string ModelProfileId { get; set; }

    [SugarColumn(IsNullable = false, Length = 32)]
    public string OutputMode { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string OutputJsonSchema { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "char(64)", Length = 64)]
    public string OutputSchemaSha256 { get; set; }
}

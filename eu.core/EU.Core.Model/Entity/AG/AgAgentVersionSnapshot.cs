namespace EU.Core.Model.Entity;

/// <summary>
/// Immutable values frozen for a published Agent version.
/// </summary>
[SugarTable("AgAgentVersionSnapshot", "Agent version snapshot"), Entity(TableCnName = "Agent version snapshot", TableName = "AgAgentVersionSnapshot")]
public class AgAgentVersionSnapshot : BasePoco
{
    [SugarColumn(IsNullable = false)]
    public Guid VersionId { get; set; }

    [SugarColumn(IsNullable = false)]
    public Guid SnapshotVersionId { get; set; }

    [SugarColumn(IsNullable = false, Length = 128)]
    public string AgentCode { get; set; }

    [SugarColumn(IsNullable = false, ColumnDataType = "nvarchar(max)")]
    public string Instructions { get; set; }

    [SugarColumn(IsNullable = false, Length = 256)]
    public string ModelProfileId { get; set; }

    [SugarColumn(IsNullable = false, Length = 32)]
    public string OutputMode { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string OutputJsonSchema { get; set; }

    [SugarColumn(IsNullable = true, Length = 256)]
    public string AgentName { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string AgentDescription { get; set; }
}

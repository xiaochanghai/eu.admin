namespace EU.Core.Model.Entity;

/// <summary>
/// MCP 工具不可变版本历史表。
/// </summary>
[SugarTable("AgMcpToolVersion", "MCP 工具不可变版本历史表"), Entity(TableCnName = "MCP 工具不可变版本历史表", TableName = "AgMcpToolVersion")]
public class AgMcpToolVersion : BasePoco
{
    /// <summary>
    /// 所属 MCP Server 主键
    /// </summary>
    [Display(Name = "ServerId"), Description("所属 MCP Server 主键"), SugarColumn(IsNullable = true)]
    public Guid? ServerId { get; set; }

    /// <summary>
    /// 历史版本排列顺序
    /// </summary>
    [Display(Name = "HistoryOrdinal"), Description("历史版本排列顺序"), SugarColumn(IsNullable = true)]
    public int? HistoryOrdinal { get; set; }

    /// <summary>
    /// 当前工具排列顺序；非当前版本为空
    /// </summary>
    [Display(Name = "CurrentOrdinal"), Description("当前工具排列顺序；非当前版本为空"), SugarColumn(IsNullable = true)]
    public int? CurrentOrdinal { get; set; }

    /// <summary>
    /// 工具名称
    /// </summary>
    [Display(Name = "Name"), Description("工具名称"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// 工具说明
    /// </summary>
    [Display(Name = "Description"), Description("工具说明"), SugarColumn(IsNullable = true, Length = 4096)]
    public string Description { get; set; }

    /// <summary>
    /// 工具输入 JSON Schema
    /// </summary>
    [Display(Name = "InputSchemaJson"), Description("工具输入 JSON Schema"), SugarColumn(IsNullable = true, Length = -1)]
    public string InputSchemaJson { get; set; }

    /// <summary>
    /// 工具风险等级
    /// </summary>
    [Display(Name = "Risk"), Description("工具风险等级"), SugarColumn(IsNullable = true, Length = 32)]
    public string Risk { get; set; }

    /// <summary>
    /// 工具版本 SHA-256 摘要
    /// </summary>
    [Display(Name = "Sha256"), Description("工具版本 SHA-256 摘要"), SugarColumn(IsNullable = true, Length = 64)]
    public string Sha256 { get; set; }

    /// <summary>
    /// 发现 UTC 时间
    /// </summary>
    [Display(Name = "DiscoveredAtUtc"), Description("发现 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? DiscoveredAtUtc { get; set; }
}

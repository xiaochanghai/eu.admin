namespace EU.Core.Model.Entity;

/// <summary>
/// MCP Server Stdio 参数表。
/// </summary>
[SugarTable("AgMcpServerArgument", "MCP Server Stdio 参数表"), Entity(TableCnName = "MCP Server Stdio 参数表", TableName = "AgMcpServerArgument")]
public class AgMcpServerArgument : BasePoco
{
    [Display(Name = "ServerId"), Description("所属 MCP Server 主键"), SugarColumn(IsNullable = true)]
    public Guid? ServerId { get; set; }

    [Display(Name = "Ordinal"), Description("参数排列顺序"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    [Display(Name = "Value"), Description("参数值"), SugarColumn(IsNullable = true, Length = 1024)]
    public string Value { get; set; }
}

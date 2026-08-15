namespace EU.Core.Model.Entity;

/// <summary>
/// MCP Server 定义主表。
/// </summary>
[SugarTable("AgMcpServerDefinition", "MCP Server 定义主表"), Entity(TableCnName = "MCP Server 定义主表", TableName = "AgMcpServerDefinition")]
public class AgMcpServerDefinition : BasePoco
{
    [Display(Name = "Code"), Description("MCP Server 唯一编码"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    [Display(Name = "Name"), Description("MCP Server 显示名称"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    [Display(Name = "Description"), Description("MCP Server 说明"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    [Display(Name = "Transport"), Description("传输类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string Transport { get; set; }

    [Display(Name = "Endpoint"), Description("HTTP 或 SSE 端点"), SugarColumn(IsNullable = true, Length = 2048)]
    public string Endpoint { get; set; }

    [Display(Name = "Command"), Description("Stdio 启动命令"), SugarColumn(IsNullable = true, Length = 512)]
    public string Command { get; set; }

    [Display(Name = "CredentialAlias"), Description("凭据别名"), SugarColumn(IsNullable = true, Length = 200)]
    public string CredentialAlias { get; set; }

    [Display(Name = "Enabled"), Description("是否启用"), SugarColumn(IsNullable = true)]
    public bool? Enabled { get; set; }

    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    [Display(Name = "Status"), Description("生命周期状态"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    [Display(Name = "LastError"), Description("最近同步错误"), SugarColumn(IsNullable = true, Length = 4096)]
    public string LastError { get; set; }

    [Display(Name = "LastSyncedAtUtc"), Description("最近同步 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? LastSyncedAtUtc { get; set; }
}

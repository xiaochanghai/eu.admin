namespace EU.Core.Model.Entity;

/// <summary>
/// MCP Server 定义主表。
/// </summary>
[SugarTable("AgMcpServerDefinition", "MCP Server 定义主表"), Entity(TableCnName = "MCP Server 定义主表", TableName = "AgMcpServerDefinition")]
public class AgMcpServerDefinition : BasePoco
{
    /// <summary>
    /// MCP Server 唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("MCP Server 唯一编码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// MCP Server 显示名称
    /// </summary>
    [Display(Name = "Name"), Description("MCP Server 显示名称"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// MCP Server 说明
    /// </summary>
    [Display(Name = "Description"), Description("MCP Server 说明"), Column(TypeName = "varchar(max)"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    /// <summary>
    /// 传输类型
    /// </summary>
    [Display(Name = "Transport"), Description("传输类型"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Transport { get; set; }

    /// <summary>
    /// HTTP 或 SSE 端点
    /// </summary>
    [Display(Name = "Endpoint"), Description("HTTP 或 SSE 端点"), Column(TypeName = "varchar(2048)"), SugarColumn(IsNullable = true, Length = 2048)]
    public string Endpoint { get; set; }

    /// <summary>
    /// Stdio 启动命令
    /// </summary>
    [Display(Name = "Command"), Description("Stdio 启动命令"), Column(TypeName = "varchar(512)"), SugarColumn(IsNullable = true, Length = 512)]
    public string Command { get; set; }

    /// <summary>
    /// 凭据别名
    /// </summary>
    [Display(Name = "CredentialAlias"), Description("凭据别名"), Column(TypeName = "varchar(200)"), SugarColumn(IsNullable = true, Length = 200)]
    public string CredentialAlias { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [Display(Name = "Enabled"), Description("是否启用"), SugarColumn(IsNullable = true)]
    public bool? Enabled { get; set; }

    /// <summary>
    /// 逻辑修订号
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("逻辑修订号"), SugarColumn(IsNullable = true)]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 生命周期状态
    /// </summary>
    [Display(Name = "Status"), Description("生命周期状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 最近同步错误
    /// </summary>
    [Display(Name = "LastError"), Description("最近同步错误"), Column(TypeName = "varchar(4096)"), SugarColumn(IsNullable = true, Length = 4096)]
    public string LastError { get; set; }

    /// <summary>
    /// 最近同步 UTC 时间
    /// </summary>
    [Display(Name = "LastSyncedAtUtc"), Description("最近同步 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? LastSyncedAtUtc { get; set; }
}

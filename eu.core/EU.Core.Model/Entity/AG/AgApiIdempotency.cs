namespace EU.Core.Model.Entity;

/// <summary>
/// Agent API 幂等请求记录表 (Model)
/// </summary>
[SugarTable("AgApiIdempotency", "Agent API 幂等请求记录表"), Entity(TableCnName = "Agent API 幂等请求记录表", TableName = "AgApiIdempotency")]
public class AgApiIdempotency : BasePoco
{
    /// <summary>当前流程节点</summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>幂等请求作用域哈希</summary>
    [Display(Name = "ScopeSha256"), Description("幂等请求作用域哈希"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string ScopeSha256 { get; set; }

    /// <summary>请求内容哈希</summary>
    [Display(Name = "RequestSha256"), Description("请求内容哈希"), Column(TypeName = "varchar(64)"), SugarColumn(IsNullable = true, Length = 64)]
    public string RequestSha256 { get; set; }

    /// <summary>幂等请求状态</summary>
    [Display(Name = "Status"), Description("幂等请求状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>缓存的 HTTP 响应状态码</summary>
    [Display(Name = "ResponseStatusCode"), Description("缓存的 HTTP 响应状态码"), SugarColumn(IsNullable = true)]
    public int? ResponseStatusCode { get; set; }

    /// <summary>缓存的 HTTP 响应内容类型</summary>
    [Display(Name = "ResponseContentType"), Description("缓存的 HTTP 响应内容类型"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string ResponseContentType { get; set; }

    /// <summary>缓存的 HTTP Location 响应头</summary>
    [Display(Name = "ResponseLocation"), Description("缓存的 HTTP Location 响应头"), Column(TypeName = "varchar(2048)"), SugarColumn(IsNullable = true, Length = 2048)]
    public string ResponseLocation { get; set; }

    /// <summary>缓存的 HTTP 响应正文</summary>
    [Display(Name = "ResponseBody"), Description("缓存的 HTTP 响应正文"), Column(TypeName = "varbinary(max)"), SugarColumn(IsNullable = true)]
    public byte[] ResponseBody { get; set; }

    /// <summary>记录创建时间（UTC）</summary>
    [Display(Name = "CreatedAtUtc"), Description("记录创建时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? CreatedAtUtc { get; set; }

    /// <summary>记录过期时间（UTC）</summary>
    [Display(Name = "ExpiresAtUtc"), Description("记录过期时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? ExpiresAtUtc { get; set; }
}

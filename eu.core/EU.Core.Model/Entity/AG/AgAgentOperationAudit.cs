namespace EU.Core.Model.Entity;

/// <summary>
/// Agent API 操作审计表 (Model)
/// </summary>
[SugarTable("AgAgentOperationAudit", "Agent API 操作审计表"), Entity(TableCnName = "Agent API 操作审计表", TableName = "AgAgentOperationAudit")]
public class AgAgentOperationAudit : BasePoco
{
    /// <summary>当前流程节点</summary>
    [Display(Name = "CurrentNode"), Description("当前流程节点"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public new string CurrentNode { get; set; }

    /// <summary>租户标识</summary>
    [Display(Name = "TenantId"), Description("租户标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    /// <summary>操作用户标识</summary>
    [Display(Name = "UserId"), Description("操作用户标识"), Column(TypeName = "varchar(256)"), SugarColumn(IsNullable = true, Length = 256)]
    public string UserId { get; set; }

    /// <summary>请求关联标识</summary>
    [Display(Name = "CorrelationId"), Description("请求关联标识"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string CorrelationId { get; set; }

    /// <summary>请求要求的授权策略</summary>
    [Display(Name = "Policy"), Description("请求要求的授权策略"), Column(TypeName = "varchar(512)"), SugarColumn(IsNullable = true, Length = 512)]
    public string Policy { get; set; }

    /// <summary>HTTP 请求方法</summary>
    [Display(Name = "Method"), Description("HTTP 请求方法"), Column(TypeName = "varchar(16)"), SugarColumn(IsNullable = true, Length = 16)]
    public string Method { get; set; }

    /// <summary>匹配的 API 路由</summary>
    [Display(Name = "Path"), Description("匹配的 API 路由"), Column(TypeName = "varchar(2048)"), SugarColumn(IsNullable = true, Length = 2048)]
    public string Path { get; set; }

    /// <summary>HTTP 响应状态码</summary>
    [Display(Name = "StatusCode"), Description("HTTP 响应状态码"), SugarColumn(IsNullable = true)]
    public int? StatusCode { get; set; }

    /// <summary>操作执行结果</summary>
    [Display(Name = "Outcome"), Description("操作执行结果"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true, Length = 32)]
    public string Outcome { get; set; }

    /// <summary>操作错误码</summary>
    [Display(Name = "ErrorCode"), Description("操作错误码"), Column(TypeName = "varchar(128)"), SugarColumn(IsNullable = true, Length = 128)]
    public string ErrorCode { get; set; }

    /// <summary>操作耗时（毫秒）</summary>
    [Display(Name = "DurationMilliseconds"), Description("操作耗时（毫秒）"), SugarColumn(IsNullable = true)]
    public long? DurationMilliseconds { get; set; }

    /// <summary>操作发生时间（UTC）</summary>
    [Display(Name = "OccurredAtUtc"), Description("操作发生时间（UTC）"), SugarColumn(IsNullable = true)]
    public DateTime? OccurredAtUtc { get; set; }
}

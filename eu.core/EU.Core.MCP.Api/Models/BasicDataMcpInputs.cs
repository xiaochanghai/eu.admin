using System.ComponentModel.DataAnnotations;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Api.MCP.Models;

/// <summary>客户新增工具参数。</summary>
public sealed class CustomerMcpCreateInput
{
    /// <summary>待新增业务字段。</summary>
    [Required, JsonPropertyName("values")]
    [McpObjectFields(MinProperties = 1, Required = new[] { "CustomerName" })]
    public CustomerMcpValues Values { get; set; } = null!;
}

/// <summary>客户修改工具参数，名称定位与新字段分开。</summary>
public sealed class CustomerMcpUpdateInput : BasicDataMcpTarget
{
    /// <summary>只传需修改的字段，未传字段保持原值。</summary>
    [Required, JsonPropertyName("values"), McpObjectFields(MinProperties = 1)]
    public CustomerMcpValues Values { get; set; } = null!;
}

/// <summary>币别新增工具参数。</summary>
public sealed class CurrencyMcpCreateInput
{
    /// <summary>待新增业务字段。</summary>
    [Required, JsonPropertyName("values")]
    [McpObjectFields(MinProperties = 1, Required = new[] { "CurrencyName" })]
    public CurrencyMcpValues Values { get; set; } = null!;
}

/// <summary>币别修改工具参数，名称定位与新字段分开。</summary>
public sealed class CurrencyMcpUpdateInput : BasicDataMcpTarget
{
    /// <summary>只传需修改的字段，未传字段保持原值。</summary>
    [Required, JsonPropertyName("values"), McpObjectFields(MinProperties = 1)]
    public CurrencyMcpValues Values { get; set; } = null!;
}

/// <summary>计量单位新增工具参数。</summary>
public sealed class UnitMcpCreateInput
{
    /// <summary>待新增业务字段。</summary>
    [Required, JsonPropertyName("values")]
    [McpObjectFields(MinProperties = 1, Required = new[] { "UnitNames" })]
    public UnitMcpValues Values { get; set; } = null!;
}

/// <summary>计量单位修改工具参数，名称定位与新字段分开。</summary>
public sealed class UnitMcpUpdateInput : BasicDataMcpTarget
{
    /// <summary>只传需修改的字段，未传字段保持原值。</summary>
    [Required, JsonPropertyName("values"), McpObjectFields(MinProperties = 1)]
    public UnitMcpValues Values { get; set; } = null!;
}

/// <summary>结算方式新增工具参数。</summary>
public sealed class SettlementWayMcpCreateInput
{
    /// <summary>待新增业务字段。</summary>
    [Required, JsonPropertyName("values")]
    [McpObjectFields(MinProperties = 1, Required = new[] { "SettlementAccountType", "SettlementBillType" })]
    public SettlementWayMcpValues Values { get; set; } = null!;
}

/// <summary>结算方式修改工具参数，名称定位与新字段分开。</summary>
public sealed class SettlementWayMcpUpdateInput : BasicDataMcpTarget
{
    /// <summary>只传需修改的字段，未传字段保持原值。</summary>
    [Required, JsonPropertyName("values"), McpObjectFields(MinProperties = 1)]
    public SettlementWayMcpValues Values { get; set; } = null!;
}

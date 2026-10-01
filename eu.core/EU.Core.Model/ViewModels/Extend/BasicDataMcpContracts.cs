#nullable enable
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EU.Core.Model.ViewModels.Extend;

/// <summary>基础数据分页条件，不接受任意 SQL 或系统字段。</summary>
public sealed class BasicDataMcpQuery
{
    /// <summary>业务编号，精确匹配。</summary>
    [Description("业务编号，精确匹配"), MaxLength(64)]
    public string? Code { get; set; }
    /// <summary>名称或简称关键字。</summary>
    [Description("名称或简称关键字"), MaxLength(100)]
    public string? Keyword { get; set; }
    /// <summary>页码，从 1 开始。</summary>
    [Description("页码，默认1")]
    public int PageIndex { get; set; } = 1;
    /// <summary>每页条数，1 至 100。</summary>
    [Description("每页条数，默认20，最大100")]
    public int PageSize { get; set; } = 20;
}

/// <summary>按原名称或原简称定位，不接受系统 ID。</summary>
public class BasicDataMcpTarget
{
    /// <summary>原名称，精确匹配，不能填新名称。</summary>
    [JsonPropertyName("name"), Description("原名称，精确匹配；与 shortName 至少提供一个，两者都有时同时匹配"), MaxLength(64)]
    public string? Name { get; set; }
    /// <summary>原简称，仅客户支持；其他模块必须使用 name。</summary>
    [JsonPropertyName("shortName"), Description("原简称，仅客户支持；其他模块使用 name"), MaxLength(32)]
    public string? ShortName { get; set; }
}

/// <summary>客户 MCP 维护字段，不含主键、公司、租户和审计字段。</summary>
public sealed class CustomerMcpValues
{
    /// <summary>客户编号。</summary>
    [Description("客户编号"), MaxLength(32)]
    public string? CustomerNo { get; set; }

    /// <summary>客户全称。</summary>
    [Description("客户全称"), MaxLength(32)]
    public string? CustomerName { get; set; }

    /// <summary>客户简称。</summary>
    [Description("客户简称"), MaxLength(32)]
    public string? CustomerShortName { get; set; }

    /// <summary>税率，沿用业务原值。</summary>
    [Description("税率，沿用业务原值")]
    public decimal? TaxRate { get; set; }

    /// <summary>收货人。</summary>
    [Description("收货人"), MaxLength(32)]
    public string? Consignee { get; set; }

    /// <summary>收货电话。</summary>
    [Description("收货电话"), MaxLength(32)]
    public string? ConsigneePhone { get; set; }

    /// <summary>收货地址。</summary>
    [Description("收货地址"), MaxLength(128)]
    public string? ConsigneeAddress { get; set; }

    /// <summary>备注。</summary>
    [Description("备注"), MaxLength(2000)]
    public string? Remark { get; set; }

}

/// <summary>币别 MCP 维护字段，不含主键、公司、租户和审计字段。</summary>
public sealed class CurrencyMcpValues
{
    /// <summary>币别编号。</summary>
    [Description("币别编号"), MaxLength(32)]
    public string? CurrencyNo { get; set; }

    /// <summary>币别名称。</summary>
    [Description("币别名称"), MaxLength(32)]
    public string? CurrencyName { get; set; }

    /// <summary>备注。</summary>
    [Description("备注"), MaxLength(2000)]
    public string? Remark { get; set; }

}

/// <summary>计量单位 MCP 维护字段，不含主键、公司、租户和审计字段。</summary>
public sealed class UnitMcpValues
{
    /// <summary>单位编号。</summary>
    [Description("单位编号"), MaxLength(64)]
    public string? UnitNo { get; set; }

    /// <summary>单位名称。</summary>
    [Description("单位名称"), MaxLength(64)]
    public string? UnitNames { get; set; }

    /// <summary>沿用当前单位模块的 DecimalPlaces 字段值。</summary>
    [Description("沿用当前单位模块的 DecimalPlaces 字段值")]
    public int? DecimalPlaces { get; set; }

    /// <summary>备注。</summary>
    [Description("备注"), MaxLength(2000)]
    public string? Remark { get; set; }

}

/// <summary>结算方式 MCP 维护字段，不含主键、公司、租户和审计字段。</summary>
public sealed class SettlementWayMcpValues
{
    /// <summary>结算编号。</summary>
    [Description("结算编号"), MaxLength(32)]
    public string? SettlementNo { get; set; }

    /// <summary>账款类型：ImmediatePay、MonthlyPay、NextMonthlyPay、PayOnDelivery、Other。</summary>
    [Description("账款类型：ImmediatePay、MonthlyPay、NextMonthlyPay、PayOnDelivery、Other"), MaxLength(32)]
    public string? SettlementAccountType { get; set; }

    /// <summary>账期天数，不小于零。</summary>
    [Description("账期天数，不小于零")]
    public int? Days { get; set; }

    /// <summary>收付款类型：Get 收款、Out 付款。</summary>
    [Description("收付款类型：Get 收款、Out 付款"), MaxLength(32)]
    public string? SettlementBillType { get; set; }

    /// <summary>备注。</summary>
    [Description("备注"), MaxLength(2000)]
    public string? Remark { get; set; }

}

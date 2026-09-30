#nullable enable

namespace EU.Core.Model.ViewModels.Extend;

/// <summary>供应商只读列表查询条件，不允许客户端指定数据权限范围。</summary>
public sealed class SupplierQueryInput
{
    /// <summary>供应商编号，精确匹配；为空时不限定编号，最长 32 个字符。</summary>
    public string? SupplierNo { get; set; }

    /// <summary>供应商全称或简称关键字，最长 100 个字符。</summary>
    public string? Keyword { get; set; }

    /// <summary>页码，从 1 开始，默认第 1 页。</summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>每页数量，范围 1–100，默认 20。</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>供应商查询的最小返回字段，不包含联系或银行信息。</summary>
public sealed class SupplierQueryItem
{
    /// <summary>供应商唯一标识。</summary>
    public Guid Id { get; set; }

    /// <summary>供应商编号。</summary>
    public string? SupplierNo { get; set; }

    /// <summary>供应商全称。</summary>
    public string? FullName { get; set; }

    /// <summary>供应商简称。</summary>
    public string? ShortName { get; set; }

    /// <summary>税别参数值。</summary>
    public string? TaxType { get; set; }

    /// <summary>税率，沿用数据库原值，不擅自换算百分比。</summary>
    public decimal? TaxRate { get; set; }
}

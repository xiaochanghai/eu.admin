using EU.Core.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace EU.Core.Api.MCP.Models;

/// <summary>供应商新增工具参数契约，实际写入仍由业务服务校验。</summary>
public sealed class BusinessSupplierCreateInput
{
    /// <summary>供应商业务字段，全名必填，不允许系统字段。</summary>
    [Required, JsonPropertyName("values")]
    [McpObjectFields(DeclaredOnly = true, MinProperties = 1, Required = new[] { "SupplierNo", "FullName", "ShortName" })]
    public BusinessSupplierCreateValues Values { get; set; } = null!;
}

/// <summary>新增供应商开放的七个业务字段，字段范围不影响修改工具。</summary>
public sealed class BusinessSupplierCreateValues
{
    /// <summary>供应商编号。</summary>
    [Description("供应商编号"), MaxLength(32)]
    public string? SupplierNo { get; set; }

    /// <summary>供应商全称，新增必填。</summary>
    [Required, Description("供应商全称"), MaxLength(32)]
    public string FullName { get; set; } = null!;

    /// <summary>供应商简称。</summary>
    [Description("供应商简称"), MaxLength(32)]
    public string? ShortName { get; set; }

    /// <summary>税率，沿用业务系统原值。</summary>
    [Description("税率，沿用业务系统原值")]
    public decimal? TaxRate { get; set; }

    /// <summary>联系人。</summary>
    [Description("联系人"), MaxLength(32)]
    public string? Contact { get; set; }

    /// <summary>电话。</summary>
    [Description("电话"), MaxLength(32)]
    public string? Phone { get; set; }

    /// <summary>备注。</summary>
    [Description("备注"), MaxLength(2000)]
    public string? Remark { get; set; }
}

/// <summary>供应商部分更新工具参数契约。</summary>
public sealed class BusinessSupplierUpdateInput
{
    /// <summary>待修改供应商的唯一标识。</summary>
    [Required, JsonPropertyName("supplierId")]
    public Guid SupplierId { get; set; }

    /// <summary>只提供需要修改的业务字段，未传字段保持不变。</summary>
    [Required, JsonPropertyName("values")]
    [McpObjectFields(DeclaredOnly = true, MinProperties = 1)]
    public BdSupplierBase Values { get; set; } = null!;
}

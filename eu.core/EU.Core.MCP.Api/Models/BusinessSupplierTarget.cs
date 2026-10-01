namespace EU.Core.Api.MCP.Models;

/// <summary>统一业务入口供应商删除目标。</summary>
public sealed class BusinessSupplierTarget
{
    /// <summary>供应商唯一标识，与编号至少提供一项。</summary>
    [System.ComponentModel.Description("供应商唯一标识，与编号至少提供一项")]
    public string? supplierId { get; set; }

    /// <summary>供应商编号，与标识同时提供时必须匹配同一记录。</summary>
    [System.ComponentModel.Description("供应商编号，与标识同时提供时必须匹配同一记录")]
    public string? supplierNo { get; set; }
}

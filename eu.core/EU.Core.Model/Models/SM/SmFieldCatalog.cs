/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmFieldCatalog.cs
*
* 功 能： N / A
* 类 名： SmFieldCatalog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:49:42  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 系统表栏位 (Model)
/// </summary>
[SugarTable("SmFieldCatalog", "系统表栏位"), Entity(TableCnName = "系统表栏位", TableName = "SmFieldCatalog")]
public class SmFieldCatalog : BasePoco
{

    /// <summary>
    /// 表字典ID
    /// </summary>
    [Display(Name = "TableCatalogId"), Description("表字典ID"), SugarColumn(IsNullable = true)]
    public Guid? TableCatalogId { get; set; }

    /// <summary>
    /// 表代码
    /// </summary>
    [Display(Name = "TableCode"), Description("表代码"), MaxLength(32, ErrorMessage = "表代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string TableCode { get; set; }

    /// <summary>
    /// 栏位代码
    /// </summary>
    [Display(Name = "ColumnCode"), Description("栏位代码"), MaxLength(64, ErrorMessage = "栏位代码 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string ColumnCode { get; set; }

    /// <summary>
    /// 栏位名
    /// </summary>
    [Display(Name = "ColumnName"), Description("栏位名"), MaxLength(64, ErrorMessage = "栏位名 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string ColumnName { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    [Display(Name = "DataType"), Description("数据类型"), MaxLength(32, ErrorMessage = "数据类型 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DataType { get; set; }

    /// <summary>
    /// 数据长度
    /// </summary>
    [Display(Name = "DataLength"), Description("数据长度"), SugarColumn(IsNullable = true)]
    public int? DataLength { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmTableCatalog.cs
*
* 功 能： N / A
* 类 名： SmTableCatalog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:10  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 映射表 (Model)
/// </summary>
[SugarTable("SmTableCatalog", "映射表"), Entity(TableCnName = "映射表", TableName = "SmTableCatalog")]
public class SmTableCatalog : BasePoco
{

    /// <summary>
    /// 表代码
    /// </summary>
    [Display(Name = "TableCode"), Description("表代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string TableCode { get; set; }

    /// <summary>
    /// 表名
    /// </summary>
    [Display(Name = "TableName"), Description("表名"), SugarColumn(IsNullable = true, Length = 32)]
    public string TableName { get; set; }

    /// <summary>
    /// 表类型
    /// </summary>
    [Display(Name = "TypeCode"), Description("表类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string TypeCode { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

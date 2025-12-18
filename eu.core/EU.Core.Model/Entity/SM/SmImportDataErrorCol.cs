/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportDataErrorCol.cs
*
* 功 能： N / A
* 类 名： SmImportDataErrorCol
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/10/27 14:21:35  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 导入数据错误列 (Model)
/// </summary>
[SugarTable("SmImportDataErrorCol", "导入数据错误列"), Entity(TableCnName = "导入数据错误列", TableName = "SmImportDataErrorCol")]
public class SmImportDataErrorCol : BasePoco
{

    /// <summary>
    /// Execl列号
    /// </summary>
    [Display(Name = "LineNo"), Description("Execl列号"), SugarColumn(IsNullable = true)]
    public int? LineNo { get; set; }

    /// <summary>
    /// Execl列号
    /// </summary>
    [Display(Name = "ColumnNo"), Description("Execl列号"), SugarColumn(IsNullable = true)]
    public int? ColumnNo { get; set; }

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), SugarColumn(IsNullable = true, Length = 32)]
    public string SheetName { get; set; }

    /// <summary>
    /// 错误说明
    /// </summary>
    [Display(Name = "ErrorMessage"), Description("错误说明"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 错误类型
    /// </summary>
    [Display(Name = "ErrorType"), Description("错误类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string ErrorType { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

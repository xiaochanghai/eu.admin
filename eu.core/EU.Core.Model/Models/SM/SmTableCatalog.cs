/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmTableCatalog.cs
*
*功 能： N / A
* 类 名： SmTableCatalog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/22 9:47:51  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 映射表 (Model)
/// </summary>
[SugarTable("SmTableCatalog", "SmTableCatalog"), Entity(TableCnName = "映射表", TableName = "SmTableCatalog")]
public class SmTableCatalog : BasePoco
{

    /// <summary>
    /// 表代码
    /// </summary>
    [Display(Name = "TableCode"), Description("表代码"), MaxLength(32, ErrorMessage = "表代码 不能超过 32 个字符")]
    public string TableCode { get; set; }

    /// <summary>
    /// 表名
    /// </summary>
    [Display(Name = "TableName"), Description("表名"), MaxLength(32, ErrorMessage = "表名 不能超过 32 个字符")]
    public string TableName { get; set; }

    /// <summary>
    /// 表类型
    /// </summary>
    [Display(Name = "TypeCode"), Description("表类型"), MaxLength(32, ErrorMessage = "表类型 不能超过 32 个字符")]
    public string TypeCode { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int? TaxisNo { get; set; }
}

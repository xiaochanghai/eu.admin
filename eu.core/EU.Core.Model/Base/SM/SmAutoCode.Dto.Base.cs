/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmAutoCode.cs
*
*功 能： N / A
* 类 名： SmAutoCode
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/24 16:53:19  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 自动编号 (Dto.Base)
/// </summary>
public class SmAutoCodeBase
{

    /// <summary>
    /// NumberCode
    /// </summary>
    [Display(Name = "NumberCode"), Description("NumberCode"), MaxLength(50, ErrorMessage = "NumberCode 不能超过 50 个字符")]
    public string NumberCode { get; set; }

    /// <summary>
    /// Prefix
    /// </summary>
    [Display(Name = "Prefix"), Description("Prefix"), MaxLength(50, ErrorMessage = "Prefix 不能超过 50 个字符")]
    public string Prefix { get; set; }

    /// <summary>
    /// DateFormatType
    /// </summary>
    [Display(Name = "DateFormatType"), Description("DateFormatType"), MaxLength(-1, ErrorMessage = "DateFormatType 不能超过 -1 个字符")]
    public string DateFormatType { get; set; }

    /// <summary>
    /// NumberLength
    /// </summary>
    public int? NumberLength { get; set; }

    /// <summary>
    /// TableName
    /// </summary>
    [Display(Name = "TableName"), Description("TableName"), MaxLength(50, ErrorMessage = "TableName 不能超过 50 个字符")]
    public string TableName { get; set; }

    /// <summary>
    /// ColumnName
    /// </summary>
    [Display(Name = "ColumnName"), Description("ColumnName"), MaxLength(50, ErrorMessage = "ColumnName 不能超过 50 个字符")]
    public string ColumnName { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    [Display(Name = "Remark"), Description("Remark"), MaxLength(2000, ErrorMessage = "Remark 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

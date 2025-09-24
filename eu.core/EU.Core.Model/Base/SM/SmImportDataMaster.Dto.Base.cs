/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportDataMaster.cs
*
* 功 能： N / A
* 类 名： SmImportDataMaster
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/9/24 14:55:24  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 导入数据主表明细 (Dto.Base)
/// </summary>
public class SmImportDataMasterBase : BasePoco
{

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), MaxLength(32, ErrorMessage = "Sheet名 不能超过 32 个字符")]
    public string SheetName { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col"), Description("Col"), MaxLength(32, ErrorMessage = "Col 不能超过 32 个字符")]
    public string Col { get; set; }

    /// <summary>
    /// Row
    /// </summary>
    [Display(Name = "Row"), Description("Row")]
    public int? Row { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    [Display(Name = "Value"), Description("值"), MaxLength(2000, ErrorMessage = "值 不能超过 2000 个字符")]
    public string Value { get; set; }

    /// <summary>
    /// 是否错误
    /// </summary>
    [Display(Name = "IsError"), Description("是否错误")]
    public bool? IsError { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

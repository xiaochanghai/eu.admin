/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmFunctionPrivilege.cs
*
*功 能： N / A
* 类 名： SmFunctionPrivilege
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/23 22:41:49  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 功能权限 (Dto.Base)
/// </summary>
public class SmFunctionPrivilegeBase
{

    /// <summary>
    /// FunctionCode
    /// </summary>
    [Display(Name = "FunctionCode"), Description("FunctionCode"), MaxLength(50, ErrorMessage = "FunctionCode 不能超过 50 个字符")]
    public string FunctionCode { get; set; }

    /// <summary>
    /// FunctionName
    /// </summary>
    [Display(Name = "FunctionName"), Description("FunctionName"), MaxLength(50, ErrorMessage = "FunctionName 不能超过 50 个字符")]
    public string FunctionName { get; set; }

    /// <summary>
    /// SmModuleId
    /// </summary>
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// TaxisNo
    /// </summary>
    public int? TaxisNo { get; set; }

    /// <summary>
    /// DisplayPosition
    /// </summary>
    [Display(Name = "DisplayPosition"), Description("DisplayPosition"), MaxLength(50, ErrorMessage = "DisplayPosition 不能超过 50 个字符")]
    public string DisplayPosition { get; set; }

    /// <summary>
    /// Color
    /// </summary>
    [Display(Name = "Color"), Description("Color"), MaxLength(-1, ErrorMessage = "Color 不能超过 -1 个字符")]
    public string Color { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    [Display(Name = "Icon"), Description("Icon"), MaxLength(32, ErrorMessage = "Icon 不能超过 32 个字符")]
    public string Icon { get; set; }

    /// <summary>
    /// 功能脚本
    /// </summary>
    [Display(Name = "FunctionJs"), Description("功能脚本"), MaxLength(32, ErrorMessage = "功能脚本 不能超过 32 个字符")]
    public string FunctionJs { get; set; }
}

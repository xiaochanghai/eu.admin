/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmFunctionPrivilege.cs
*
* 功 能： N / A
* 类 名： SmFunctionPrivilege
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:48  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 功能权限 (Dto.Base)
/// </summary>
public class SmFunctionPrivilegeBase : BasePoco
{

    /// <summary>
    /// 功能代码
    /// </summary>
    [Display(Name = "FunctionCode"), Description("功能代码"), MaxLength(50, ErrorMessage = "功能代码 不能超过 50 个字符")]
    public string FunctionCode { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    [Display(Name = "FunctionName"), Description("功能名称"), MaxLength(50, ErrorMessage = "功能名称 不能超过 50 个字符")]
    public string FunctionName { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "SmModuleId"), Description("模块ID")]
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号")]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 显示位置
    /// </summary>
    [Display(Name = "DisplayPosition"), Description("显示位置"), MaxLength(50, ErrorMessage = "显示位置 不能超过 50 个字符")]
    public string DisplayPosition { get; set; }

    /// <summary>
    /// Color
    /// </summary>
    [Display(Name = "Color"), Description("Color"), MaxLength(50, ErrorMessage = "Color 不能超过 50 个字符")]
    public string Color { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    [Display(Name = "Icon"), Description("Icon"), MaxLength(32, ErrorMessage = "Icon 不能超过 32 个字符")]
    public string Icon { get; set; }

    /// <summary>
    /// 功能脚本
    /// </summary>
    [Display(Name = "FunctionJs"), Description("功能脚本"), MaxLength(2000, ErrorMessage = "功能脚本 不能超过 2000 个字符")]
    public string FunctionJs { get; set; }
}

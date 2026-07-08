/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmMobilePageConfig.cs
*
* 功 能： N / A
* 类 名： SmMobilePageConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/7/8  Claude   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 移动端页面配置 (Dto.Base)
/// </summary>
public class SmMobilePageConfigBase : BasePoco
{

    /// <summary>
    /// 页面编码
    /// </summary>
    [Display(Name = "PageCode"), Description("页面编码"), MaxLength(50, ErrorMessage = "页面编码 不能超过 50 个字符")]
    public string PageCode { get; set; }

    /// <summary>
    /// 页面名称
    /// </summary>
    [Display(Name = "PageName"), Description("页面名称"), MaxLength(100, ErrorMessage = "页面名称 不能超过 100 个字符")]
    public string PageName { get; set; }

    /// <summary>
    /// 应用范围
    /// </summary>
    [Display(Name = "AppScope"), Description("应用范围"), MaxLength(50, ErrorMessage = "应用范围 不能超过 50 个字符")]
    public string AppScope { get; set; }

    /// <summary>
    /// 页面类型
    /// </summary>
    [Display(Name = "PageType"), Description("页面类型"), MaxLength(20, ErrorMessage = "页面类型 不能超过 20 个字符")]
    public string PageType { get; set; }

    /// <summary>
    /// 页面标题
    /// </summary>
    [Display(Name = "Title"), Description("页面标题"), MaxLength(100, ErrorMessage = "页面标题 不能超过 100 个字符")]
    public string Title { get; set; }

    /// <summary>
    /// 配置版本号
    /// </summary>
    [Display(Name = "Version"), Description("配置版本号")]
    public int? Version { get; set; }

    /// <summary>
    /// 页面配置JSON
    /// </summary>
    [Display(Name = "ConfigJson"), Description("页面配置JSON")]
    public string ConfigJson { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    [Display(Name = "IsPublished"), Description("是否已发布")]
    public bool? IsPublished { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

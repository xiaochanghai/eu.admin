/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmConfig.cs
*
* 功 能： N / A
* 类 名： SmConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:25:54  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 系统配置 (Dto.Base)
/// </summary>
public class SmConfigBase : BasePoco
{

    /// <summary>
    /// 分组ID
    /// </summary>
    [Display(Name = "ConfigGroupId"), Description("分组ID"), SugarColumn(IsNullable = true)]
    public Guid? ConfigGroupId { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    [Display(Name = "ConfigName"), Description("参数名称"), MaxLength(32, ErrorMessage = "参数名称 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ConfigName { get; set; }

    /// <summary>
    /// 参数代码
    /// </summary>
    [Display(Name = "ConfigCode"), Description("参数代码"), MaxLength(32, ErrorMessage = "参数代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ConfigCode { get; set; }

    /// <summary>
    /// 参数值
    /// </summary>
    [Display(Name = "ConfigValue"), Description("参数值"), MaxLength(32, ErrorMessage = "参数值 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ConfigValue { get; set; }

    /// <summary>
    /// 参数类型
    /// </summary>
    [Display(Name = "InputType"), Description("参数类型"), MaxLength(32, ErrorMessage = "参数类型 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string InputType { get; set; }

    /// <summary>
    /// 配置内容
    /// </summary>
    [Display(Name = "AvailableValue"), Description("配置内容"), MaxLength(128, ErrorMessage = "配置内容 不能超过 128 个字符"), SugarColumn(IsNullable = true)]
    public string AvailableValue { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [Display(Name = "Sequence"), Description("排序"), SugarColumn(IsNullable = true)]
    public int? Sequence { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

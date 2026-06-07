/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLanguageConfig.cs
*
* 功 能： N / A
* 类 名： SmLanguageConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/6/7 11:13:05  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 多语配置 (Dto.Base)
/// </summary>
public class SmLanguageConfigBase : BasePoco
{

    /// <summary>
    /// 关联实体类型 (Module/ModuleColumn)
    /// </summary>
    [Display(Name = "RefType"), Description("关联实体类型 (Module/ModuleColumn)"), MaxLength(32, ErrorMessage = "关联实体类型 (Module/ModuleColumn) 不能超过 32 个字符")]
    public string RefType { get; set; }

    /// <summary>
    /// 关联实体ID
    /// </summary>
    [Display(Name = "RefId"), Description("关联实体ID")]
    public Guid? RefId { get; set; }

    /// <summary>
    /// 关联字段名 (ModuleName/Title/FormTitle/Placeholder/TooltipContent)
    /// </summary>
    [Display(Name = "RefField"), Description("关联字段名 (ModuleName/Title/FormTitle/Placeholder/TooltipContent)"), MaxLength(32, ErrorMessage = "关联字段名 (ModuleName/Title/FormTitle/Placeholder/TooltipContent) 不能超过 32 个字符")]
    public string RefField { get; set; }

    /// <summary>
    /// 中文值
    /// </summary>
    [Display(Name = "Value_ZH"), Description("中文值"), MaxLength(64, ErrorMessage = "中文值 不能超过 64 个字符")]
    public string Value_ZH { get; set; }

    /// <summary>
    /// 英文值
    /// </summary>
    [Display(Name = "Value_EN"), Description("英文值"), MaxLength(64, ErrorMessage = "英文值 不能超过 64 个字符")]
    public string Value_EN { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

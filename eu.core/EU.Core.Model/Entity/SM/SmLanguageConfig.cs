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

namespace EU.Core.Model.Entity;

/// <summary>
/// 多语配置 (Model)
/// </summary>
[SugarTable("SmLanguageConfig", "多语配置"), Entity(TableCnName = "多语配置", TableName = "SmLanguageConfig")]
public class SmLanguageConfig : BasePoco
{

    /// <summary>
    /// 关联实体类型 (Module/ModuleColumn)
    /// </summary>
    [Display(Name = "RefType"), Description("关联实体类型 (Module/ModuleColumn)"), SugarColumn(IsNullable = true, Length = 32)]
    public string RefType { get; set; }

    /// <summary>
    /// 关联实体ID
    /// </summary>
    [Display(Name = "RefId"), Description("关联实体ID"), SugarColumn(IsNullable = true)]
    public Guid? RefId { get; set; }

    /// <summary>
    /// 关联字段名 (ModuleName/Title/FormTitle/Placeholder/TooltipContent)
    /// </summary>
    [Display(Name = "RefField"), Description("关联字段名 (ModuleName/Title/FormTitle/Placeholder/TooltipContent)"), SugarColumn(IsNullable = true, Length = 32)]
    public string RefField { get; set; }

    /// <summary>
    /// 中文值
    /// </summary>
    [Display(Name = "Value_ZH"), Description("中文值"), SugarColumn(IsNullable = true, Length = 64)]
    public string Value_ZH { get; set; }

    /// <summary>
    /// 英文值
    /// </summary>
    [Display(Name = "Value_EN"), Description("英文值"), SugarColumn(IsNullable = true, Length = 64)]
    public string Value_EN { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

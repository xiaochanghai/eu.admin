/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgSkillDefinition.cs
*
* 功 能： N / A
* 类 名： AgSkillDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/14 18:18:16  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// Skill 定义主表，保存 Skill 身份、名称、分类、状态和草稿修订号 (Model)
/// </summary>
[SugarTable("AgSkillDefinition", "Skill 定义主表，保存 Skill 身份、名称、分类、状态和草稿修订号"), Entity(TableCnName = "Skill 定义主表，保存 Skill 身份、名称、分类、状态和草稿修订号", TableName = "AgSkillDefinition")]
public class AgSkillDefinition : BasePoco
{

    /// <summary>
    /// Skill 唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("Skill 唯一编码"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// 草稿修订号，用于乐观并发控制
    /// </summary>
    [Display(Name = "DraftRevision"), Description("草稿修订号，用于乐观并发控制")]
    public long? DraftRevision { get; set; }

    /// <summary>
    /// Skill 显示名称
    /// </summary>
    [Display(Name = "Name"), Description("Skill 显示名称"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// Skill 功能说明
    /// </summary>
    [Display(Name = "Description"), Description("Skill 功能说明"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    /// <summary>
    /// Skill 分类
    /// </summary>
    [Display(Name = "Category"), Description("Skill 分类"), SugarColumn(IsNullable = true, Length = 128)]
    public string Category { get; set; }

    /// <summary>
    /// Skill 状态：Active 或 Archived
    /// </summary>
    [Display(Name = "Status"), Description("Skill 状态：Active 或 Archived"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }
}

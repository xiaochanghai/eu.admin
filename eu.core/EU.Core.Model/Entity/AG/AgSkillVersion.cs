/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgSkillVersion.cs
*
* 功 能： N / A
* 类 名： AgSkillVersion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/14 18:20:46  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// Skill 发布版本表，保存版本标识、文件清单摘要和发布时间 (Model)
/// </summary>
[SugarTable("AgSkillVersion", "Skill 发布版本表，保存版本标识、文件清单摘要和发布时间"), Entity(TableCnName = "Skill 发布版本表，保存版本标识、文件清单摘要和发布时间", TableName = "AgSkillVersion")]
public class AgSkillVersion : BasePoco
{

    /// <summary>
    /// 所属 Skill 主键，对应 AgSkillDefinition.ID
    /// </summary>
    [Display(Name = "SkillId"), Description("所属 Skill 主键，对应 AgSkillDefinition.ID"), SugarColumn(IsNullable = true)]
    public Guid? SkillId { get; set; }

    /// <summary>
    /// 发布版本排列顺序，从 0 开始
    /// </summary>
    [Display(Name = "Ordinal"), Description("发布版本排列顺序，从 0 开始"), SugarColumn(IsNullable = true)]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 严格 SemVer 版本标签，例如 1.0.0
    /// </summary>
    [Display(Name = "Label"), Description("严格 SemVer 版本标签，例如 1.0.0"), SugarColumn(IsNullable = true, Length = 128)]
    public string Label { get; set; }

    /// <summary>
    /// 发布文件清单的 SHA-256 摘要
    /// </summary>
    [Display(Name = "ManifestSha256"), Description("发布文件清单的 SHA-256 摘要"), SugarColumn(IsNullable = true, Length = 64)]
    public string ManifestSha256 { get; set; }

    /// <summary>
    /// UTC 发布时间
    /// </summary>
    [Display(Name = "PublishedAtUtc"), Description("UTC 发布时间"), SugarColumn(IsNullable = true)]
    public DateTime? PublishedAtUtc { get; set; }
}

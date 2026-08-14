/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgSkillVersionFile.cs
*
* 功 能： N / A
* 类 名： AgSkillVersionFile
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/14 18:21:55  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// Skill 发布版本文件表，保存不可变文件清单 (Model)
/// </summary>
[SugarTable("AgSkillVersionFile", "Skill 发布版本文件表，保存不可变文件清单"), Entity(TableCnName = "Skill 发布版本文件表，保存不可变文件清单", TableName = "AgSkillVersionFile")]
public class AgSkillVersionFile : BasePoco
{

    /// <summary>
    /// 所属 Skill 发布版本主键，对应 AgSkillVersion.ID
    /// </summary>
    [Display(Name = "VersionId"), Description("所属 Skill 发布版本主键，对应 AgSkillVersion.ID")]
    public Guid? VersionId { get; set; }

    /// <summary>
    /// 文件排列顺序，从 0 开始
    /// </summary>
    [Display(Name = "Ordinal"), Description("文件排列顺序，从 0 开始")]
    public int? Ordinal { get; set; }

    /// <summary>
    /// Skill 内相对文件路径
    /// </summary>
    [Display(Name = "Path"), Description("Skill 内相对文件路径"), SugarColumn(IsNullable = true, Length = 1024)]
    public string Path { get; set; }

    /// <summary>
    /// 文件字节数
    /// </summary>
    [Display(Name = "Size"), Description("文件字节数")]
    public long? Size { get; set; }

    /// <summary>
    /// 文件内容的 SHA-256 摘要
    /// </summary>
    [Display(Name = "Sha256"), Description("文件内容的 SHA-256 摘要"), SugarColumn(IsNullable = true, Length = 64)]
    public string Sha256 { get; set; }
}

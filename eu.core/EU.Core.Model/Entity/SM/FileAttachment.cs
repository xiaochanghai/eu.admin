/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* FileAttachment.cs
*
*功 能： N / A
* 类 名： FileAttachment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/24 22:53:40  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity; 

/// <summary>
/// 附件 (Model)
/// </summary>
[SugarTable("FileAttachment", "附件"), Entity(TableCnName = "附件", TableName = "FileAttachment")]
public class FileAttachment : BasePoco
{

    /// <summary>
    /// 主键ID
    /// </summary>
    [Display(Name = "MasterId"), Description("主键ID"), MaxLength(-1, ErrorMessage = "主键ID 不能超过 -1 个字符")]
    public Guid? MasterId { get; set; }

    /// <summary>
    /// 原文件名
    /// </summary>
    [Display(Name = "OriginalFileName"), Description("原文件名"), MaxLength(-1, ErrorMessage = "原文件名 不能超过 -1 个字符"), SugarColumn(IsNullable = true)]
    public string OriginalFileName { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [Display(Name = "FileName"), Description("文件名"), MaxLength(-1, ErrorMessage = "文件名 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string FileName { get; set; }

    /// <summary>
    /// 扩展名
    /// </summary>
    [Display(Name = "FileExt"), Description("扩展名"), MaxLength(10, ErrorMessage = "FileExt 不能超过 10 个字符"), SugarColumn(IsNullable = true)]
    public string FileExt { get; set; }

    /// <summary>
    /// 路径
    /// </summary>
    [Display(Name = "Path"), Description("路径"), MaxLength(-1, ErrorMessage = "Path 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Path { get; set; }

    /// <summary>
    /// 大小
    /// </summary>
    [Display(Name = "Length"), Description("大小"), SugarColumn(IsNullable = true)]
    public long? Length { get; set; }
 

    /// <summary>
    /// 文件类型
    /// </summary>
    [Display(Name = "ImageType"), Description("文件类型"), MaxLength(50, ErrorMessage = "ImageType 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string ImageType { get; set; }
}

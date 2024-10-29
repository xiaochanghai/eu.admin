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
namespace EU.Core.Model.Models;


/// <summary>
/// FileAttachment (Dto.Base)
/// </summary>
public class FileAttachmentBase
{

    /// <summary>
    /// MasterId
    /// </summary>
    public Guid? MasterId { get; set; }

    /// <summary>
    /// OriginalFileName
    /// </summary>
    [Display(Name = "OriginalFileName"), Description("OriginalFileName"), MaxLength(-1, ErrorMessage = "OriginalFileName 不能超过 -1 个字符")]
    public string OriginalFileName { get; set; }

    /// <summary>
    /// FileName
    /// </summary>
    [Display(Name = "FileName"), Description("FileName"), MaxLength(-1, ErrorMessage = "FileName 不能超过 -1 个字符")]
    public string FileName { get; set; }

    /// <summary>
    /// FileExt
    /// </summary>
    [Display(Name = "FileExt"), Description("FileExt"), MaxLength(10, ErrorMessage = "FileExt 不能超过 10 个字符")]
    public string FileExt { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    [Display(Name = "Path"), Description("Path"), MaxLength(-1, ErrorMessage = "Path 不能超过 -1 个字符")]
    public string Path { get; set; }

    /// <summary>
    /// Length
    /// </summary>

    /// <summary>
    /// FileData
    /// </summary>

    /// <summary>
    /// ImageType
    /// </summary>
    [Display(Name = "ImageType"), Description("ImageType"), MaxLength(50, ErrorMessage = "ImageType 不能超过 50 个字符")]
    public string ImageType { get; set; }
}

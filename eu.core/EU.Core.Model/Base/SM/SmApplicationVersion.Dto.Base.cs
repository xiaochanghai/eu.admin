/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationVersion.cs
*
* 功 能： N / A
* 类 名： SmApplicationVersion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/3 17:13:20  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// APP版本 (Dto.Base)
/// </summary>
public class SmApplicationVersionBase : BasePoco
{

    /// <summary>
    /// 版本平台
    /// </summary>
    [Display(Name = "Platform"), Description("版本平台"), MaxLength(32, ErrorMessage = "版本平台 不能超过 32 个字符")]
    public string Platform { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [Display(Name = "VersionNo"), Description("版本号"), MaxLength(32, ErrorMessage = "版本号 不能超过 32 个字符")]
    public string VersionNo { get; set; }

    /// <summary>
    /// 打包序号
    /// </summary>
    [Display(Name = "BuildNum"), Description("打包序号")]
    public int? BuildNum { get; set; }

    /// <summary>
    /// 版本说明
    /// </summary>
    [Display(Name = "VersionDesc"), Description("版本说明"), MaxLength(256, ErrorMessage = "版本说明 不能超过 256 个字符")]
    public string VersionDesc { get; set; }

    /// <summary>
    /// 更新类型
    /// </summary>
    [Display(Name = "UpdateType"), Description("更新类型"), MaxLength(32, ErrorMessage = "更新类型 不能超过 32 个字符")]
    public string UpdateType { get; set; }

    /// <summary>
    /// 渠道
    /// </summary>
    [Display(Name = "Channel"), Description("渠道"), MaxLength(32, ErrorMessage = "渠道 不能超过 32 个字符")]
    public string Channel { get; set; }

    /// <summary>
    /// 文件地址
    /// </summary>
    [Display(Name = "FileUrl"), Description("文件地址"), MaxLength(256, ErrorMessage = "文件地址 不能超过 256 个字符")]
    public string FileUrl { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

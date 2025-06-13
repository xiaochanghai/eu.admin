/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* RmEmail.cs
*
* 功 能： N / A
* 类 名： RmEmail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/6/13 10:08:30  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 邮件账号 (Dto.Base)
/// </summary>
public class RmEmailBase : BasePoco
{

    /// <summary>
    /// IMAP服务地址
    /// </summary>
    [Display(Name = "ImapHost"), Description("IMAP服务地址"), MaxLength(32, ErrorMessage = "IMAP服务地址 不能超过 32 个字符")]
    public string ImapHost { get; set; }

    /// <summary>
    /// IMAP端口
    /// </summary>
    [Display(Name = "ImapPort"), Description("IMAP端口"), MaxLength(10, ErrorMessage = "IMAP端口 不能超过 10 个字符")]
    public string ImapPort { get; set; }

    /// <summary>
    /// 账号
    /// </summary>
    [Display(Name = "UserName"), Description("账号"), MaxLength(32, ErrorMessage = "账号 不能超过 32 个字符")]
    public string UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Display(Name = "Password"), Description("密码"), MaxLength(32, ErrorMessage = "密码 不能超过 32 个字符")]
    public string Password { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

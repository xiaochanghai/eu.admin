/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* RmReume.cs
*
* 功 能： N / A
* 类 名： RmReume
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/6/12 17:53:45  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 简历 (Dto.Base)
/// </summary>
public class RmReumeBase : BasePoco
{

    /// <summary>
    /// 应聘者
    /// </summary>
    [Display(Name = "StaffName"), Description("应聘者"), MaxLength(32, ErrorMessage = "应聘者 不能超过 32 个字符")]
    public string StaffName { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Display(Name = "Phone"), Description("电话"), MaxLength(32, ErrorMessage = "电话 不能超过 32 个字符")]
    public string Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [Display(Name = "Email"), Description("邮箱"), MaxLength(64, ErrorMessage = "邮箱 不能超过 64 个字符")]
    public string Email { get; set; }

    /// <summary>
    /// 年龄
    /// </summary>
    [Display(Name = "Age"), Description("年龄"), MaxLength(32, ErrorMessage = "年龄 不能超过 32 个字符")]
    public string Age { get; set; }

    /// <summary>
    /// 地区
    /// </summary>
    [Display(Name = "Distinct"), Description("地区"), MaxLength(32, ErrorMessage = "地区 不能超过 32 个字符")]
    public string Distinct { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }

    /// <summary>
    /// Uid
    /// </summary>
    [Display(Name = "Uid"), Description("Uid"), MaxLength(32, ErrorMessage = "Uid 不能超过 32 个字符")]
    public string Uid { get; set; }
}

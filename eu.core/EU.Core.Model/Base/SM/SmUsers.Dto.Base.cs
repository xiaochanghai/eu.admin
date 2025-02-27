/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUsers.cs
*
* 功 能： N / A
* 类 名： SmUsers
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:25  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 系统用户 (Dto.Base)
/// </summary>
public class SmUsersBase : BasePoco
{

    /// <summary>
    /// 用户代码
    /// </summary>
    [Display(Name = "UserAccount"), Description("用户代码"), MaxLength(50, ErrorMessage = "用户代码 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string UserAccount { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [Display(Name = "UserName"), Description("用户名"), MaxLength(50, ErrorMessage = "用户名 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Display(Name = "PassWord"), Description("密码"), MaxLength(50, ErrorMessage = "密码 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string PassWord { get; set; }

    /// <summary>
    /// 员工ID
    /// </summary>
    [Display(Name = "EmployeeId"), Description("员工ID"), SugarColumn(IsNullable = true)]
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// 用户类型
    /// </summary>
    [Display(Name = "UserType"), Description("用户类型"), MaxLength(50, ErrorMessage = "用户类型 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string UserType { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    [Display(Name = "Remark"), Description("Remark"), MaxLength(2000, ErrorMessage = "Remark 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 用户头像ID
    /// </summary>
    [Display(Name = "AvatarFileId"), Description("用户头像ID"), SugarColumn(IsNullable = true)]
    public Guid? AvatarFileId { get; set; }
}

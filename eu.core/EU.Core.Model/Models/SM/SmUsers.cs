/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUsers.cs
*
*功 能： N / A
* 类 名： SmUsers
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/18 14:58:11  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 系统用户 (Model)
/// </summary>
[SugarTable("SmUsers", "SmUsers"), Entity(TableCnName = "系统用户", TableName = "SmUsers")]
public class SmUsers : BasePoco
{

    /// <summary>
    /// 用户代码
    /// </summary>
    [Display(Name = "UserAccount"), Description("用户代码"), MaxLength(50, ErrorMessage = "用户代码 不能超过 50 个字符")]
    public string UserAccount { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [Display(Name = "UserName"), Description("用户名"), MaxLength(50, ErrorMessage = "用户名 不能超过 50 个字符")]
    public string UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Display(Name = "PassWord"), Description("密码"), MaxLength(50, ErrorMessage = "密码 不能超过 50 个字符")]
    public string PassWord { get; set; }

    /// <summary>
    /// 员工ID
    /// </summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// 用户类型
    /// </summary>
    [Display(Name = "UserType"), Description("用户类型"), MaxLength(50, ErrorMessage = "用户类型 不能超过 50 个字符")]
    public string UserType { get; set; }

    /// <summary>
    /// 用户头像ID
    /// </summary>
    public Guid? AvatarFileId { get; set; }
}

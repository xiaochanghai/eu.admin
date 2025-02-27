/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoles.cs
*
* 功 能： N / A
* 类 名： SmRoles
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:30:25  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 系统角色 (Dto.Base)
/// </summary>
public class SmRolesBase : BasePoco
{

    /// <summary>
    /// 角色代码
    /// </summary>
    [Display(Name = "RoleCode"), Description("角色代码"), MaxLength(50, ErrorMessage = "角色代码 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string RoleCode { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [Display(Name = "RoleName"), Description("角色"), MaxLength(50, ErrorMessage = "角色 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string RoleName { get; set; }
}

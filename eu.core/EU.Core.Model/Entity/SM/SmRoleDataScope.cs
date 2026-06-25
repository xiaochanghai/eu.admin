/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleDataScope.cs
*
* 功 能： 角色数据权限
* 类 名： SmRoleDataScope
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/23  EU Team   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：EU Team                              │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 角色数据权限 (Model)
/// 用于控制角色可以访问哪些公司的数据
/// </summary>
[SugarTable("SmRoleDataScope", "角色数据权限"), Entity(TableCnName = "角色数据权限", TableName = "SmRoleDataScope")]
public class SmRoleDataScope : BasePoco
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [Display(Name = "SmRoleId"), Description("角色ID"), SugarColumn(IsNullable = false)]
    public Guid SmRoleId { get; set; }
}

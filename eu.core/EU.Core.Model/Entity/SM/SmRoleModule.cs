﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleModule.cs
*
* 功 能： N / A
* 类 名： SmRoleModule
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:08  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 角色模块权限 (Model)
/// </summary>
[SugarTable("SmRoleModule", "角色模块权限"), Entity(TableCnName = "角色模块权限", TableName = "SmRoleModule")]
public class SmRoleModule : BasePoco
{

    /// <summary>
    /// 角色ID
    /// </summary>
    [Display(Name = "SmRoleId"), Description("角色ID"), SugarColumn(IsNullable = true)]
    public Guid? SmRoleId { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "SmModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? SmModuleId { get; set; }
}

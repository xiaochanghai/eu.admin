/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleFunction.cs
*
* 功 能： N / A
* 类 名： SmRoleFunction
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:06  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 角色功能权限 (Dto.Base)
/// </summary>
public class SmRoleFunctionBase : BasePoco
{

    /// <summary>
    /// 角色ID
    /// </summary>
    [Display(Name = "SmRoleId"), Description("角色ID")]
    public Guid? SmRoleId { get; set; }

    /// <summary>
    /// 功能权限定义ID
    /// </summary>
    [Display(Name = "SmFunctionId"), Description("功能权限定义ID")]
    public Guid? SmFunctionId { get; set; }

    /// <summary>
    /// NoActionCode
    /// </summary>
    [Display(Name = "NoActionCode"), Description("NoActionCode"), MaxLength(-1, ErrorMessage = "NoActionCode 不能超过 -1 个字符")]
    public string NoActionCode { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "SmModuleId"), Description("模块ID")]
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 操作代码
    /// </summary>
    [Display(Name = "ActionCode"), Description("操作代码"), MaxLength(32, ErrorMessage = "操作代码 不能超过 32 个字符")]
    public string ActionCode { get; set; }
}

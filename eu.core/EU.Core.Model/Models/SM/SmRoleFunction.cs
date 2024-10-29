/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleFunction.cs
*
*功 能： N / A
* 类 名： SmRoleFunction
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/21 1:05:41  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// SmRoleFunction (Model)
/// </summary>
[SugarTable("SmRoleFunction", "SmRoleFunction"), Entity(TableCnName = "SmRoleFunction", TableName = "SmRoleFunction")]
public class SmRoleFunction : BasePoco
{

    /// <summary>
    /// SmRoleId
    /// </summary>
    public Guid? SmRoleId { get; set; }

    /// <summary>
    /// SmFunctionId
    /// </summary>
    public Guid? SmFunctionId { get; set; }

    /// <summary>
    /// NoActionCode
    /// </summary>
    [Display(Name = "NoActionCode"), Description("NoActionCode"), MaxLength(-1, ErrorMessage = "NoActionCode 不能超过 -1 个字符")]
    public string NoActionCode { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 操作代码
    /// </summary>
    [Display(Name = "ActionCode"), Description("操作代码"), MaxLength(-1, ErrorMessage = "操作代码 不能超过 32 个字符")]
    public string ActionCode { get; set; }
}

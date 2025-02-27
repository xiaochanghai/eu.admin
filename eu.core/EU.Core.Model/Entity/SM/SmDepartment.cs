/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmDepartment.cs
*
* 功 能： N / A
* 类 名： SmDepartment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:36:53  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 部门 (Model)
/// </summary>
[SugarTable("SmDepartment", "部门"), Entity(TableCnName = "部门", TableName = "SmDepartment")]
public class SmDepartment : BasePoco
{

    /// <summary>
    /// 上级部门ID
    /// </summary>
    [Display(Name = "DepartmentId"), Description("上级部门ID"), SugarColumn(IsNullable = true)]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 部门代码
    /// </summary>
    [Display(Name = "DepartmentCode"), Description("部门代码"), MaxLength(50, ErrorMessage = "部门代码 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string DepartmentCode { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [Display(Name = "DepartmentName"), Description("部门名称"), MaxLength(50, ErrorMessage = "部门名称 不能超过 50 个字符"), SugarColumn(IsNullable = true)]
    public string DepartmentName { get; set; }
}

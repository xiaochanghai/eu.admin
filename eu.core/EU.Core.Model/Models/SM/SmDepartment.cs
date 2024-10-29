/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmDepartment.cs
*
*功 能： N / A
* 类 名： SmDepartment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/24 16:35:59  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 部门 (Model)
/// </summary>
[SugarTable("SmDepartment", "SmDepartment"), Entity(TableCnName = "部门", TableName = "SmDepartment")]
public class SmDepartment : BasePoco
{

    /// <summary>
    /// DepartmentId
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// DepartmentCode
    /// </summary>
    [Display(Name = "DepartmentCode"), Description("DepartmentCode"), MaxLength(50, ErrorMessage = "DepartmentCode 不能超过 50 个字符")]
    public string DepartmentCode { get; set; }

    /// <summary>
    /// DepartmentName
    /// </summary>
    [Display(Name = "DepartmentName"), Description("DepartmentName"), MaxLength(50, ErrorMessage = "DepartmentName 不能超过 50 个字符")]
    public string DepartmentName { get; set; }
}

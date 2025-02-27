/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmEmployee.cs
*
* 功 能： N / A
* 类 名： SmEmployee
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:45  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 员工管理 (Dto.Base)
/// </summary>
public class SmEmployeeBase : BasePoco
{

    /// <summary>
    /// 部门ID
    /// </summary>
    [Display(Name = "DepartmentId"), Description("部门ID")]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 上级ID
    /// </summary>
    [Display(Name = "ParentEmployeeId"), Description("上级ID")]
    public Guid? ParentEmployeeId { get; set; }

    /// <summary>
    /// 员工代码
    /// </summary>
    [Display(Name = "EmployeeCode"), Description("员工代码"), MaxLength(32, ErrorMessage = "员工代码 不能超过 32 个字符")]
    public string EmployeeCode { get; set; }

    /// <summary>
    /// 员工姓名
    /// </summary>
    [Display(Name = "EmployeeName"), Description("员工姓名"), MaxLength(32, ErrorMessage = "员工姓名 不能超过 32 个字符")]
    public string EmployeeName { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [Display(Name = "Sex"), Description("性别"), MaxLength(32, ErrorMessage = "性别 不能超过 32 个字符")]
    public string Sex { get; set; }

    /// <summary>
    /// 英文名
    /// </summary>
    [Display(Name = "EName"), Description("英文名"), MaxLength(32, ErrorMessage = "英文名 不能超过 32 个字符")]
    public string EName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [Display(Name = "NickName"), Description("昵称"), MaxLength(32, ErrorMessage = "昵称 不能超过 32 个字符")]
    public string NickName { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Display(Name = "Phone"), Description("电话"), MaxLength(32, ErrorMessage = "电话 不能超过 32 个字符")]
    public string Phone { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    [Display(Name = "HireDate"), Description("入职日期")]
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// 离职日期
    /// </summary>
    [Display(Name = "TermDate"), Description("离职日期")]
    public DateTime? TermDate { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [Display(Name = "HeadUrl"), Description("头像"), MaxLength(64, ErrorMessage = "头像 不能超过 64 个字符")]
    public string HeadUrl { get; set; }

    /// <summary>
    /// 月销售目标
    /// </summary>
    [Display(Name = "MonthsSalesAmount"), Description("月销售目标"), Column(TypeName = "decimal(20,2)")]
    public decimal? MonthsSalesAmount { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmComponent.cs
*
* 功 能： N / A
* 类 名： SmComponent
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:25:53  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 组件测试 (Model)
/// </summary>
[SugarTable("SmComponent", "组件测试"), Entity(TableCnName = "组件测试", TableName = "SmComponent")]
public class SmComponent : BasePoco
{

    /// <summary>
    /// 用户名
    /// </summary>
    [Display(Name = "UserName"), Description("用户名"), MaxLength(32, ErrorMessage = "用户名 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string UserName { get; set; }

    /// <summary>
    /// 销售额
    /// </summary>
    [Display(Name = "SalesAmount"), Description("销售额"), Column(TypeName = "decimal(20,4)"), SugarColumn(IsNullable = true)]
    public decimal? SalesAmount { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    [Display(Name = "OrderStatus"), Description("订单状态"), MaxLength(32, ErrorMessage = "订单状态 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 员工ID
    /// </summary>
    [Display(Name = "EmployeeId"), Description("员工ID"), SugarColumn(IsNullable = true)]
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// 员工状态
    /// </summary>
    [Display(Name = "EmployeeStatus"), Description("员工状态"), SugarColumn(IsNullable = true)]
    public bool? EmployeeStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

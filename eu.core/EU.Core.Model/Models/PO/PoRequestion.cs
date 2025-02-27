/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoRequestion.cs
*
* 功 能： N / A
* 类 名： PoRequestion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:09:23  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 请购单 (Model)
/// </summary>
[SugarTable("PoRequestion", "请购单"), Entity(TableCnName = "请购单", TableName = "PoRequestion")]
public class PoRequestion : BasePoco
{

    /// <summary>
    /// 请购单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("请购单号"), MaxLength(32, ErrorMessage = "请购单号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderNo { get; set; }

    /// <summary>
    /// 请购类型
    /// </summary>
    [Display(Name = "RequestionType"), Description("请购类型"), MaxLength(32, ErrorMessage = "请购类型 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string RequestionType { get; set; }

    /// <summary>
    /// 请购人
    /// </summary>
    [Display(Name = "UserId"), Description("请购人"), SugarColumn(IsNullable = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 需求日期
    /// </summary>
    [Display(Name = "RequestionDate"), Description("需求日期"), SugarColumn(IsNullable = true)]
    public DateTime? RequestionDate { get; set; }

    /// <summary>
    /// 请购时间
    /// </summary>
    [Display(Name = "OrderDate"), Description("请购时间"), SugarColumn(IsNullable = true)]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 请购部门
    /// </summary>
    [Display(Name = "DepartmentId"), Description("请购部门"), SugarColumn(IsNullable = true)]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    [Display(Name = "OrderStatus"), Description("订单状态"), MaxLength(32, ErrorMessage = "订单状态 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

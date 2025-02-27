/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoArrivalOrder.cs
*
* 功 能： N / A
* 类 名： PoArrivalOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:08:24  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 采购到货通知单 (Dto.Base)
/// </summary>
public class PoArrivalOrderBase : BasePoco
{

    /// <summary>
    /// 单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderNo { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("作业日期"), SugarColumn(IsNullable = true)]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 采购员ID
    /// </summary>
    [Display(Name = "UserId"), Description("采购员ID"), SugarColumn(IsNullable = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [Display(Name = "DepartmentId"), Description("部门ID"), SugarColumn(IsNullable = true)]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    [Display(Name = "SupplierId"), Description("供应商ID"), SugarColumn(IsNullable = true)]
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 到货日期 
    /// </summary>
    [Display(Name = "ArrivalTime"), Description("到货日期 "), SugarColumn(IsNullable = true)]
    public DateTime? ArrivalTime { get; set; }

    /// <summary>
    /// 采购合同号
    /// </summary>
    [Display(Name = "ContractNo"), Description("采购合同号"), MaxLength(32, ErrorMessage = "采购合同号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ContractNo { get; set; }

    /// <summary>
    /// 送货单号
    /// </summary>
    [Display(Name = "DeliveryOrderNo"), Description("送货单号"), MaxLength(32, ErrorMessage = "送货单号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DeliveryOrderNo { get; set; }

    /// <summary>
    /// 条码
    /// </summary>
    [Display(Name = "BarCode"), Description("条码"), MaxLength(32, ErrorMessage = "条码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string BarCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    [Display(Name = "OrderStatus"), Description("订单状态"), MaxLength(32, ErrorMessage = "订单状态 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderStatus { get; set; }
}

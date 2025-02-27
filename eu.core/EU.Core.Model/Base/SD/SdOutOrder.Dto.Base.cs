/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrder.cs
*
* 功 能： N / A
* 类 名： SdOutOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:14:54  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 出库单 (Dto.Base)
/// </summary>
public class SdOutOrderBase : BasePoco
{

    /// <summary>
    /// 订单来源
    /// </summary>
    [Display(Name = "OrderSource"), Description("订单来源"), MaxLength(32, ErrorMessage = "订单来源 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderSource { get; set; }

    /// <summary>
    /// 订单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("订单号"), MaxLength(32, ErrorMessage = "订单号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderNo { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("作业日期"), SugarColumn(IsNullable = true)]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    [Display(Name = "CustomerId"), Description("客户ID"), SugarColumn(IsNullable = true)]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 业务员ID
    /// </summary>
    [Display(Name = "SalesmanId"), Description("业务员ID"), SugarColumn(IsNullable = true)]
    public Guid? SalesmanId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    [Display(Name = "StockId"), Description("仓库ID"), SugarColumn(IsNullable = true)]
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    [Display(Name = "GoodsLocationId"), Description("货位ID"), SugarColumn(IsNullable = true)]
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 送货方式ID
    /// </summary>
    [Display(Name = "DeliveryWayId"), Description("送货方式ID"), SugarColumn(IsNullable = true)]
    public Guid? DeliveryWayId { get; set; }

    /// <summary>
    /// 物流单号
    /// </summary>
    [Display(Name = "ExpressNo"), Description("物流单号"), MaxLength(64, ErrorMessage = "物流单号 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string ExpressNo { get; set; }

    /// <summary>
    /// 收货人
    /// </summary>
    [Display(Name = "Contact"), Description("收货人"), MaxLength(32, ErrorMessage = "收货人 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Contact { get; set; }

    /// <summary>
    /// 收货地址
    /// </summary>
    [Display(Name = "Address"), Description("收货地址"), MaxLength(128, ErrorMessage = "收货地址 不能超过 128 个字符"), SugarColumn(IsNullable = true)]
    public string Address { get; set; }

    /// <summary>
    /// 收货电话
    /// </summary>
    [Display(Name = "Phone"), Description("收货电话"), MaxLength(32, ErrorMessage = "收货电话 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Phone { get; set; }

    /// <summary>
    /// 出库日期
    /// </summary>
    [Display(Name = "OutDate"), Description("出库日期"), SugarColumn(IsNullable = true)]
    public DateTime? OutDate { get; set; }

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

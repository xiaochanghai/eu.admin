/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrderDetail.cs
*
* 功 能： N / A
* 类 名： SdOutOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:14:56  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 出库单明细 (Dto.Base)
/// </summary>
public class SdOutOrderDetailBase : BasePoco
{

    /// <summary>
    /// 订单ID
    /// </summary>
    [Display(Name = "OrderId"), Description("订单ID"), SugarColumn(IsNullable = true)]
    public Guid? OrderId { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    [Display(Name = "SerialNumber"), Description("序号"), SugarColumn(IsNullable = true)]
    public int? SerialNumber { get; set; }

    /// <summary>
    /// 出货来源
    /// </summary>
    [Display(Name = "OrderSource"), Description("出货来源"), MaxLength(32, ErrorMessage = "出货来源 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderSource { get; set; }

    /// <summary>
    /// 销售订单ID
    /// </summary>
    [Display(Name = "SalesOrderId"), Description("销售订单ID"), SugarColumn(IsNullable = true)]
    public Guid? SalesOrderId { get; set; }

    /// <summary>
    /// 销售订单明细ID
    /// </summary>
    [Display(Name = "SalesOrderDetailId"), Description("销售订单明细ID"), SugarColumn(IsNullable = true)]
    public Guid? SalesOrderDetailId { get; set; }

    /// <summary>
    /// 发货订单ID
    /// </summary>
    [Display(Name = "ShipOrderId"), Description("发货订单ID"), SugarColumn(IsNullable = true)]
    public Guid? ShipOrderId { get; set; }

    /// <summary>
    /// 发货订单明细ID
    /// </summary>
    [Display(Name = "ShipOrderDetailId"), Description("发货订单明细ID"), SugarColumn(IsNullable = true)]
    public Guid? ShipOrderDetailId { get; set; }

    /// <summary>
    /// 货品ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("货品ID"), SugarColumn(IsNullable = true)]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 出库数量
    /// </summary>
    [Display(Name = "OutQTY"), Description("出库数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? OutQTY { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    [Display(Name = "ReturnQTY"), Description("退货数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? ReturnQTY { get; set; }

    /// <summary>
    /// 批次号ID
    /// </summary>
    [Display(Name = "BatchId"), Description("批次号ID"), SugarColumn(IsNullable = true)]
    public Guid? BatchId { get; set; }

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
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

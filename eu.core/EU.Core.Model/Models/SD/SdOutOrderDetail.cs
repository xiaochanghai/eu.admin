/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrderDetail.cs
*
*功 能： N / A
* 类 名： SdOutOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/8/14 15:23:48  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 出库单明细 (Model)
/// </summary>
[SugarTable("SdOutOrderDetail", "SdOutOrderDetail"), Entity(TableCnName = "出库单明细", TableName = "SdOutOrderDetail")]
public class SdOutOrderDetail : BasePoco
{

    /// <summary>
    /// 订单ID
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    public int? SerialNumber { get; set; }

    /// <summary>
    /// 出货来源
    /// </summary>
    [Display(Name = "OrderSource"), Description("出货来源"), MaxLength(32, ErrorMessage = "出货来源 不能超过 32 个字符")]
    public string OrderSource { get; set; }

    /// <summary>
    /// 销售订单ID
    /// </summary>
    public Guid? SalesOrderId { get; set; }

    /// <summary>
    /// 销售订单明细ID
    /// </summary>
    public Guid? SalesOrderDetailId { get; set; }

    /// <summary>
    /// 发货订单ID
    /// </summary>
    public Guid? ShipOrderId { get; set; }

    /// <summary>
    /// 发货订单明细ID
    /// </summary>
    public Guid? ShipOrderDetailId { get; set; }

    /// <summary>
    /// 货品ID
    /// </summary>
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 出库数量
    /// </summary>
    [Display(Name = "OutQTY"), Description("出库数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? OutQTY { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    [Display(Name = "ReturnQTY"), Description("退货数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? ReturnQTY { get; set; }

    /// <summary>
    /// 批次号ID
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

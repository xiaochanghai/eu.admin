/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoInOrder.cs
*
* 功 能： N / A
* 类 名： PoInOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:05  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 采购入库单 (Dto.Base)
/// </summary>
public class PoInOrderBase : BasePoco
{

    /// <summary>
    /// 销售单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("销售单号"), MaxLength(32, ErrorMessage = "销售单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 入库日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("入库日期")]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 入库人员ID
    /// </summary>
    [Display(Name = "UserId"), Description("入库人员ID")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    [Display(Name = "SupplierId"), Description("供应商ID")]
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    [Display(Name = "StockId"), Description("仓库ID")]
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    [Display(Name = "GoodsLocationId"), Description("货位ID")]
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    [Display(Name = "OrderStatus"), Description("订单状态"), MaxLength(32, ErrorMessage = "订单状态 不能超过 32 个字符")]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }

    /// <summary>
    /// 单据来源，采购单、到货通知单
    /// </summary>
    [Display(Name = "OrderSource"), Description("单据来源，采购单、到货通知单"), MaxLength(32, ErrorMessage = "单据来源，采购单、到货通知单 不能超过 32 个字符")]
    public string OrderSource { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoReturnOrder.cs
*
* 功 能： N / A
* 类 名： PoReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2024/10/8 14:03:46  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 采购退货单 (Model)
/// </summary>
[SugarTable("PoReturnOrder", "采购退货单"), Entity(TableCnName = "采购退货单", TableName = "PoReturnOrder")]
public class PoReturnOrder : BasePoco
{

    /// <summary>
    /// 退货单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("退货单号"), MaxLength(32, ErrorMessage = "退货单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 退货人ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
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
}

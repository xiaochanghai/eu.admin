/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrder.cs
*
* 功 能： N / A
* 类 名： SdReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:26  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 退库单 (Model)
/// </summary>
[SugarTable("SdReturnOrder", "退库单"), Entity(TableCnName = "退库单", TableName = "SdReturnOrder")]
public class SdReturnOrder : BasePoco
{

    /// <summary>
    /// 订单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("订单号"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderNo { get; set; }

    /// <summary>
    /// 订单类型 正式订单、样品订单、其他订单（默认正式订单）
    /// </summary>
    [Display(Name = "OrderCategory"), Description("订单类型 正式订单、样品订单、其他订单（默认正式订单）"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderCategory { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("作业日期"), SugarColumn(IsNullable = true)]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 客户单号
    /// </summary>
    [Display(Name = "CustomerOrderNo"), Description("客户单号"), SugarColumn(IsNullable = true, Length = 64)]
    public string CustomerOrderNo { get; set; }

    /// <summary>
    /// 预定交期
    /// </summary>
    [Display(Name = "DeliveryDate"), Description("预定交期"), SugarColumn(IsNullable = true)]
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 订单等级
    /// </summary>
    [Display(Name = "OrderLevel"), Description("订单等级"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderLevel { get; set; }

    /// <summary>
    /// 业务员ID
    /// </summary>
    [Display(Name = "SalesmanId"), Description("业务员ID"), SugarColumn(IsNullable = true)]
    public Guid? SalesmanId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    [Display(Name = "CustomerId"), Description("客户ID"), SugarColumn(IsNullable = true)]
    public Guid? CustomerId { get; set; }

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
    /// 退回原因
    /// </summary>
    [Display(Name = "ReturnReason"), Description("退回原因"), SugarColumn(IsNullable = true, Length = 32)]
    public string ReturnReason { get; set; }

    /// <summary>
    /// 退货日期
    /// </summary>
    [Display(Name = "ReturnDate"), Description("退货日期"), SugarColumn(IsNullable = true)]
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// 退货状态--待退回、已退回（保存后，单据状态为待退回，增加退回入库按钮，点击确认后，单据状态为已退回）
    /// </summary>
    [Display(Name = "OrderStatus"), Description("退货状态--待退回、已退回（保存后，单据状态为待退回，增加退回入库按钮，点击确认后，单据状态为已退回）"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

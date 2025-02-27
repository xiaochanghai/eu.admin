/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdShipOrder.cs
*
* 功 能： N / A
* 类 名： SdShipOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:29  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 发货单 (Model)
/// </summary>
[SugarTable("SdShipOrder", "发货单"), Entity(TableCnName = "发货单", TableName = "SdShipOrder")]
public class SdShipOrder : BasePoco
{

    /// <summary>
    /// 发货单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("发货单号"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderNo { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("作业日期"), SugarColumn(IsNullable = true)]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 发货仓库
    /// </summary>
    [Display(Name = "StockId"), Description("发货仓库"), SugarColumn(IsNullable = true)]
    public Guid? StockId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    [Display(Name = "CustomerId"), Description("客户ID"), SugarColumn(IsNullable = true)]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 收货人
    /// </summary>
    [Display(Name = "Contact"), Description("收货人"), SugarColumn(IsNullable = true, Length = 32)]
    public string Contact { get; set; }

    /// <summary>
    /// 收货地址
    /// </summary>
    [Display(Name = "Address"), Description("收货地址"), SugarColumn(IsNullable = true, Length = 128)]
    public string Address { get; set; }

    /// <summary>
    /// 收货电话
    /// </summary>
    [Display(Name = "Phone"), Description("收货电话"), SugarColumn(IsNullable = true, Length = 32)]
    public string Phone { get; set; }

    /// <summary>
    /// 发货日期
    /// </summary>
    [Display(Name = "ShipDate"), Description("发货日期"), SugarColumn(IsNullable = true)]
    public DateTime? ShipDate { get; set; }

    /// <summary>
    /// 订单状态
    /// </summary>
    [Display(Name = "OrderStatus"), Description("订单状态"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

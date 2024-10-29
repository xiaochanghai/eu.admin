/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrder.cs
*
*功 能： N / A
* 类 名： SdOutOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/8/23 17:48:24  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 出库单 (Model)
/// </summary>
[SugarTable("SdOutOrder", "SdOutOrder"), Entity(TableCnName = "出库单", TableName = "SdOutOrder")]
public class SdOutOrder : BasePoco
{

    /// <summary>
    /// 订单来源
    /// </summary>
    [Display(Name = "OrderSource"), Description("订单来源"), MaxLength(32, ErrorMessage = "订单来源 不能超过 32 个字符")]
    public string OrderSource { get; set; }

    /// <summary>
    /// 订单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("订单号"), MaxLength(32, ErrorMessage = "订单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 业务员ID
    /// </summary>
    public Guid? SalesmanId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 送货方式ID
    /// </summary>
    public Guid? DeliveryWayId { get; set; }

    /// <summary>
    /// 物流单号
    /// </summary>
    [Display(Name = "ExpressNo"), Description("物流单号"), MaxLength(64, ErrorMessage = "物流单号 不能超过 64 个字符")]
    public string ExpressNo { get; set; }

    /// <summary>
    /// 收货人
    /// </summary>
    [Display(Name = "Contact"), Description("收货人"), MaxLength(32, ErrorMessage = "收货人 不能超过 32 个字符")]
    public string Contact { get; set; }

    /// <summary>
    /// 收货地址
    /// </summary>
    [Display(Name = "Address"), Description("收货地址"), MaxLength(128, ErrorMessage = "收货地址 不能超过 128 个字符")]
    public string Address { get; set; }

    /// <summary>
    /// 收货电话
    /// </summary>
    [Display(Name = "Phone"), Description("收货电话"), MaxLength(32, ErrorMessage = "收货电话 不能超过 32 个字符")]
    public string Phone { get; set; }

    /// <summary>
    /// 出库日期
    /// </summary>
    public DateTime? OutDate { get; set; }

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

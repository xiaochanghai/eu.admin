/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdShipOrder.cs
*
*功 能： N / A
* 类 名： SdShipOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:50:13  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 发货单 (Dto.Base)
/// </summary>
public class SdShipOrderBase
{
     

    /// <summary>
    /// 发货单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("发货单号"), MaxLength(32, ErrorMessage = "发货单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 发货仓库
    /// </summary>
    public Guid? StockId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public Guid? CustomerId { get; set; }

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
    /// 发货日期
    /// </summary>
    public DateTime? ShipDate { get; set; }

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

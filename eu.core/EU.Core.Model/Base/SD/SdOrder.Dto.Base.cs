/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOrder.cs
*
*功 能： N / A
* 类 名： SdOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/10/21 16:56:07  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 销售单 (Dto.Base)
/// </summary>
public class SdOrderBase
{

    /// <summary>
    /// 销售单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("销售单号"), MaxLength(32, ErrorMessage = "销售单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 订单日期/订购日期
    /// </summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 交货日期
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 订单等级
    /// </summary>
    [Display(Name = "OrderLevel"), Description("订单等级"), MaxLength(32, ErrorMessage = "订单等级 不能超过 32 个字符")]
    public string OrderLevel { get; set; }

    /// <summary>
    /// 订单类别 正式订单、样品订单、其他订单（默认正式订单）
    /// </summary>
    [Display(Name = "OrderCategory"), Description("订单类别 正式订单、样品订单、其他订单（默认正式订单）"), MaxLength(32, ErrorMessage = "订单类别 正式订单、样品订单、其他订单（默认正式订单） 不能超过 32 个字符")]
    public string OrderCategory { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 客户单号
    /// </summary>
    [Display(Name = "CustomerOrderNo"), Description("客户单号"), MaxLength(64, ErrorMessage = "客户单号 不能超过 64 个字符")]
    public string CustomerOrderNo { get; set; }

    /// <summary>
    /// 业务员
    /// </summary>
    public Guid? SalesmanId { get; set; }

    /// <summary>
    /// 税别，参数值：TaxType
    /// </summary>
    [Display(Name = "TaxType"), Description("税别，参数值：TaxType"), MaxLength(32, ErrorMessage = "税别，参数值：TaxType 不能超过 32 个字符")]
    public string TaxType { get; set; }

    /// <summary>
    /// 税率
    /// </summary>
    [Display(Name = "TaxRate"), Description("税率"), Column(TypeName = "decimal(20,6)")]
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// 未税金额
    /// </summary>
    [Display(Name = "NoTaxAmount"), Description("未税金额"), Column(TypeName = "decimal(20,2)")]
    public decimal? NoTaxAmount { get; set; }

    /// <summary>
    /// 税额
    /// </summary>
    [Display(Name = "TaxAmount"), Description("税额"), Column(TypeName = "decimal(20,2)")]
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// 含税金额
    /// </summary>
    [Display(Name = "TaxIncludedAmount"), Description("含税金额"), Column(TypeName = "decimal(20,2)")]
    public decimal? TaxIncludedAmount { get; set; }

    /// <summary>
    /// 结算方式
    /// </summary>
    public Guid? SettlementWayId { get; set; }

    /// <summary>
    /// 结算方式
    /// </summary>
    [Display(Name = "SettlementWay"), Description("结算方式"), MaxLength(32, ErrorMessage = "结算方式 不能超过 32 个字符")]
    public string SettlementWay { get; set; }

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
    /// 币别
    /// </summary>
    public Guid? CurrencyId { get; set; }

    /// <summary>
    /// 订单状态（作废）
    /// </summary>
    [Display(Name = "SalesOrderStatus"), Description("订单状态（作废）"), MaxLength(32, ErrorMessage = "订单状态（作废） 不能超过 32 个字符")]
    public string SalesOrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

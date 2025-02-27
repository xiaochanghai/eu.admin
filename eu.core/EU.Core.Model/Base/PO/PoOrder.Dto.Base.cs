/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoOrder.cs
*
* 功 能： N / A
* 类 名： PoOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:09:21  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 采购单 (Dto.Base)
/// </summary>
public class PoOrderBase : BasePoco
{

    /// <summary>
    /// 单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderNo { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    [Display(Name = "SupplierId"), Description("供应商ID"), SugarColumn(IsNullable = true)]
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 采购类型，主材采购/耗材采购/样品采购/事物采购
    /// </summary>
    [Display(Name = "PurchaseType"), Description("采购类型，主材采购/耗材采购/样品采购/事物采购"), MaxLength(32, ErrorMessage = "采购类型，主材采购/耗材采购/样品采购/事物采购 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string PurchaseType { get; set; }

    /// <summary>
    /// 采购日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("采购日期"), SugarColumn(IsNullable = true)]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 送达日期
    /// </summary>
    [Display(Name = "DeliveryDate"), Description("送达日期"), SugarColumn(IsNullable = true)]
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 采购员ID
    /// </summary>
    [Display(Name = "UserId"), Description("采购员ID"), SugarColumn(IsNullable = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 税别，参数值：TaxType
    /// </summary>
    [Display(Name = "TaxType"), Description("税别，参数值：TaxType"), MaxLength(32, ErrorMessage = "税别，参数值：TaxType 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string TaxType { get; set; }

    /// <summary>
    /// 税率
    /// </summary>
    [Display(Name = "TaxRate"), Description("税率"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true)]
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// 结算方式
    /// </summary>
    [Display(Name = "SettlementWayId"), Description("结算方式"), SugarColumn(IsNullable = true)]
    public Guid? SettlementWayId { get; set; }

    /// <summary>
    /// 订单状态,未到货、部分到货、全部到货
    /// </summary>
    [Display(Name = "OrderStatus"), Description("订单状态,未到货、部分到货、全部到货"), MaxLength(32, ErrorMessage = "订单状态,未到货、部分到货、全部到货 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

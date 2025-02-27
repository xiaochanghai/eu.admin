/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCustomer.cs
*
* 功 能： N / A
* 类 名： BdCustomer
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:29:37  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 客户 (Model)
/// </summary>
[SugarTable("BdCustomer", "客户"), Entity(TableCnName = "客户", TableName = "BdCustomer")]
public class BdCustomer : BasePoco
{

    /// <summary>
    /// 客户编号
    /// </summary>
    [Display(Name = "CustomerNo"), Description("客户编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string CustomerNo { get; set; }

    /// <summary>
    /// 客户名称
    /// </summary>
    [Display(Name = "CustomerName"), Description("客户名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string CustomerName { get; set; }

    /// <summary>
    /// 客户简称
    /// </summary>
    [Display(Name = "CustomerShortName"), Description("客户简称"), SugarColumn(IsNullable = true, Length = 32)]
    public string CustomerShortName { get; set; }

    /// <summary>
    /// 客户等级ID
    /// </summary>
    [Display(Name = "CustomerLevelId"), Description("客户等级ID"), SugarColumn(IsNullable = true)]
    public Guid? CustomerLevelId { get; set; }

    /// <summary>
    /// 客户分类ID
    /// </summary>
    [Display(Name = "CustomerClassId"), Description("客户分类ID"), SugarColumn(IsNullable = true)]
    public Guid? CustomerClassId { get; set; }

    /// <summary>
    /// 地区ID
    /// </summary>
    [Display(Name = "DistrictId"), Description("地区ID"), SugarColumn(IsNullable = true)]
    public Guid? DistrictId { get; set; }

    /// <summary>
    /// 结算方式ID
    /// </summary>
    [Display(Name = "SettlementWayId"), Description("结算方式ID"), SugarColumn(IsNullable = true)]
    public Guid? SettlementWayId { get; set; }

    /// <summary>
    /// 送货方式ID
    /// </summary>
    [Display(Name = "DeliveryWayId"), Description("送货方式ID"), SugarColumn(IsNullable = true)]
    public Guid? DeliveryWayId { get; set; }

    /// <summary>
    /// 业务员ID
    /// </summary>
    [Display(Name = "EmployeeId"), Description("业务员ID"), SugarColumn(IsNullable = true)]
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// 币别ID
    /// </summary>
    [Display(Name = "CurrencyId"), Description("币别ID"), SugarColumn(IsNullable = true)]
    public Guid? CurrencyId { get; set; }

    /// <summary>
    /// 税别，参数值：TaxType
    /// </summary>
    [Display(Name = "TaxType"), Description("税别，参数值：TaxType"), SugarColumn(IsNullable = true, Length = 32)]
    public string TaxType { get; set; }

    /// <summary>
    /// 税率
    /// </summary>
    [Display(Name = "TaxRate"), Description("税率"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 6)]
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// 收货人
    /// </summary>
    [Display(Name = "Consignee"), Description("收货人"), SugarColumn(IsNullable = true, Length = 32)]
    public string Consignee { get; set; }

    /// <summary>
    /// 收货电话
    /// </summary>
    [Display(Name = "ConsigneePhone"), Description("收货电话"), SugarColumn(IsNullable = true, Length = 32)]
    public string ConsigneePhone { get; set; }

    /// <summary>
    /// 收货地址
    /// </summary>
    [Display(Name = "ConsigneeAddress"), Description("收货地址"), SugarColumn(IsNullable = true, Length = 128)]
    public string ConsigneeAddress { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

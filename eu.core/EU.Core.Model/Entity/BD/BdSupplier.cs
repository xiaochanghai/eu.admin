/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSupplier.cs
*
* 功 能： N / A
* 类 名： BdSupplier
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:29:49  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 供应商 (Model)
/// </summary>
[SugarTable("BdSupplier", "供应商"), Entity(TableCnName = "供应商", TableName = "BdSupplier")]
public class BdSupplier : BasePoco
{

    /// <summary>
    /// 供应商编号
    /// </summary>
    [Display(Name = "SupplierNo"), Description("供应商编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string SupplierNo { get; set; }

    /// <summary>
    /// 供应商名称
    /// </summary>
    [Display(Name = "FullName"), Description("供应商名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string FullName { get; set; }

    /// <summary>
    /// 供应商名称
    /// </summary>
    [Display(Name = "ShortName"), Description("供应商名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string ShortName { get; set; }

    /// <summary>
    /// 供应商等级ID
    /// </summary>
    [Display(Name = "SupplierLevelId"), Description("供应商等级ID"), SugarColumn(IsNullable = true)]
    public Guid? SupplierLevelId { get; set; }

    /// <summary>
    /// 供应商分类ID
    /// </summary>
    [Display(Name = "SupplierClassId"), Description("供应商分类ID"), SugarColumn(IsNullable = true)]
    public Guid? SupplierClassId { get; set; }

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
    /// 联系人
    /// </summary>
    [Display(Name = "Contact"), Description("联系人"), SugarColumn(IsNullable = true, Length = 32)]
    public string Contact { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Display(Name = "Phone"), Description("电话"), SugarColumn(IsNullable = true, Length = 32)]
    public string Phone { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

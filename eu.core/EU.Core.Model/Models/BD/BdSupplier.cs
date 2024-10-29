/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSupplier.cs
*
*功 能： N / A
* 类 名： BdSupplier
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/25 19:20:49  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 供应商 (Model)
/// </summary>
[SugarTable("BdSupplier", "BdSupplier"), Entity(TableCnName = "供应商", TableName = "BdSupplier")]
public class BdSupplier : BasePoco
{

    /// <summary>
    /// 供应商编号
    /// </summary>
    [Display(Name = "SupplierNo"), Description("供应商编号"), MaxLength(32, ErrorMessage = "供应商编号 不能超过 32 个字符")]
    public string SupplierNo { get; set; }

    /// <summary>
    /// 供应商名称
    /// </summary>
    [Display(Name = "FullName"), Description("供应商名称"), MaxLength(32, ErrorMessage = "供应商名称 不能超过 32 个字符")]
    public string FullName { get; set; }

    /// <summary>
    /// 供应商名称
    /// </summary>
    [Display(Name = "ShortName"), Description("供应商名称"), MaxLength(32, ErrorMessage = "供应商名称 不能超过 32 个字符")]
    public string ShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    [Display(Name = "Contact"), Description("联系人"), MaxLength(32, ErrorMessage = "联系人 不能超过 32 个字符")]
    public string Contact { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Display(Name = "Phone"), Description("电话"), MaxLength(32, ErrorMessage = "电话 不能超过 32 个字符")]
    public string Phone { get; set; }

    /// <summary>
    /// 采购员
    /// </summary>
    public Guid? EmployeeId { get; set; }

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
    /// 结算方式
    /// </summary>
    public Guid? SettlementWayId { get; set; }

    /// <summary>
    /// 送货方式
    /// </summary>
    public Guid? DeliveryWayId { get; set; }

    /// <summary>
    /// 币别
    /// </summary>
    public Guid? CurrencyId { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [Display(Name = "Address"), Description("地址"), MaxLength(64, ErrorMessage = "地址 不能超过 64 个字符")]
    public string Address { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

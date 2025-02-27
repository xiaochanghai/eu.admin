/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCustomerDeliveryAddress.cs
*
* 功 能： N / A
* 类 名： BdCustomerDeliveryAddress
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 12:32:28  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 客户地址 (Model)
/// </summary>
[SugarTable("BdCustomerDeliveryAddress", "客户地址"), Entity(TableCnName = "客户地址", TableName = "BdCustomerDeliveryAddress")]
public class BdCustomerDeliveryAddress : BasePoco
{

    /// <summary>
    /// 客户ID
    /// </summary>
    [Display(Name = "CustomerId"), Description("客户ID"), SugarColumn(IsNullable = true)]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [Display(Name = "Address"), Description("地址"), MaxLength(128, ErrorMessage = "地址 不能超过 128 个字符"), SugarColumn(IsNullable = true)]
    public string Address { get; set; }

    /// <summary>
    /// 负责人
    /// </summary>
    [Display(Name = "Contact"), Description("负责人"), MaxLength(32, ErrorMessage = "负责人 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Contact { get; set; }

    /// <summary>
    /// 手机
    /// </summary>
    [Display(Name = "Phone"), Description("手机"), MaxLength(32, ErrorMessage = "手机 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Phone { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Display(Name = "Telephone"), Description("电话"), MaxLength(32, ErrorMessage = "电话 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Telephone { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    [Display(Name = "IsDefault"), Description("是否默认"), SugarColumn(IsNullable = true)]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

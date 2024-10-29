/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCustomerDeliveryAddress.cs
*
*功 能： N / A
* 类 名： BdCustomerDeliveryAddress
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/25 17:23:44  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 客户地址 (Model)
/// </summary>
[SugarTable("BdCustomerDeliveryAddress", "BdCustomerDeliveryAddress"), Entity(TableCnName = "客户地址", TableName = "BdCustomerDeliveryAddress")]
public class BdCustomerDeliveryAddress : BasePoco
{

    /// <summary>
    /// 客户ID
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [Display(Name = "Address"), Description("地址"), MaxLength(128, ErrorMessage = "地址 不能超过 128 个字符")]
    public string Address { get; set; }

    /// <summary>
    /// 负责人
    /// </summary>
    [Display(Name = "Contact"), Description("负责人"), MaxLength(32, ErrorMessage = "负责人 不能超过 32 个字符")]
    public string Contact { get; set; }

    /// <summary>
    /// 手机
    /// </summary>
    [Display(Name = "Phone"), Description("手机"), MaxLength(32, ErrorMessage = "手机 不能超过 32 个字符")]
    public string Phone { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Display(Name = "Telephone"), Description("电话"), MaxLength(32, ErrorMessage = "电话 不能超过 32 个字符")]
    public string Telephone { get; set; }

    /// <summary>
    /// 是否默认
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

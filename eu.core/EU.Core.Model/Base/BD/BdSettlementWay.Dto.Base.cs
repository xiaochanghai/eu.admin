/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSettlementWay.cs
*
* 功 能： N / A
* 类 名： BdSettlementWay
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/26 21:52:26  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 结算方式 (Dto.Base)
/// </summary>
public class BdSettlementWayBase : BasePoco
{

    /// <summary>
    /// 结算编号
    /// </summary>
    [Display(Name = "SettlementNo"), Description("结算编号"), MaxLength(32, ErrorMessage = "结算编号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string SettlementNo { get; set; }

    /// <summary>
    /// 账款类型, 现付-ImmediatePay;月结-MonthlyPay;次月结-NextMonthlyPay;   货到付款-PayOnDelivery;其他-Other
    /// </summary>
    [Display(Name = "SettlementAccountType"), Description("账款类型, 现付-ImmediatePay;月结-MonthlyPay;次月结-NextMonthlyPay;货到付款-PayOnDelivery;其他-Other"), MaxLength(32, ErrorMessage = "账款类型, 现付-ImmediatePay;月结-MonthlyPay;次月结-NextMonthlyPay;货到付款-PayOnDelivery;其他-Other 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string SettlementAccountType { get; set; }

    /// <summary>
    /// 账期天数
    /// </summary>
    [Display(Name = "Days"), Description("账期天数"), SugarColumn(IsNullable = true)]
    public int? Days { get; set; }

    /// <summary>
    /// 收付款（收-Get、付-Out选择）
    /// </summary>
    [Display(Name = "SettlementBillType"), Description("收付款（收-Get、付-Out选择）"), MaxLength(32, ErrorMessage = "收付款（收-Get、付-Out选择） 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string SettlementBillType { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 结算名称
    /// </summary>
    [Display(Name = "SettlementName"), Description("结算名称"), MaxLength(32, ErrorMessage = "结算名称 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string SettlementName { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCurrency.cs
*
* 功 能： N / A
* 类 名： BdCurrency
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/28 10:54:29  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 币别 (Dto.Base)
/// </summary>
public class BdCurrencyBase : BasePoco
{

    /// <summary>
    /// 币别编号
    /// </summary>
    [Display(Name = "CurrencyNo"), Description("币别编号"), MaxLength(32, ErrorMessage = "币别编号 不能超过 32 个字符")]
    public string CurrencyNo { get; set; }

    /// <summary>
    /// 币别名称
    /// </summary>
    [Display(Name = "CurrencyName"), Description("币别名称"), MaxLength(32, ErrorMessage = "币别名称 不能超过 32 个字符")]
    public string CurrencyName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

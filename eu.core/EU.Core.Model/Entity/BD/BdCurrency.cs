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

namespace EU.Core.Model.Entity;

/// <summary>
/// 币别 (Model)
/// </summary>
[SugarTable("BdCurrency", "币别"), Entity(TableCnName = "币别", TableName = "BdCurrency")]
public class BdCurrency : BasePoco
{

    /// <summary>
    /// 币别编号
    /// </summary>
    [Display(Name = "CurrencyNo"), Description("币别编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string CurrencyNo { get; set; }

    /// <summary>
    /// 币别名称
    /// </summary>
    [Display(Name = "CurrencyName"), Description("币别名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string CurrencyName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

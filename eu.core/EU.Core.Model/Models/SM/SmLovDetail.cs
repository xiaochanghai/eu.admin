/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLovDetail.cs
*
*功 能： N / A
* 类 名： SmLovDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/26 14:20:44  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// SmLovDetail (Model)
/// </summary>
[SugarTable("SmLovDetail", "SmLovDetail"), Entity(TableCnName = "SmLovDetail", TableName = "SmLovDetail")]
public class SmLovDetail : BasePoco
{

    /// <summary>
    /// SmLovId
    /// </summary>
    public Guid? SmLovId { get; set; }

    /// <summary>
    /// TaxisNo
    /// </summary>
    public int? TaxisNo { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    [Display(Name = "Value"), Description("Value"), MaxLength(50, ErrorMessage = "Value 不能超过 50 个字符")]
    public string Value { get; set; }

    /// <summary>
    /// Text
    /// </summary>
    [Display(Name = "Text"), Description("Text"), MaxLength(50, ErrorMessage = "Text 不能超过 50 个字符")]
    public string Text { get; set; }

    /// <summary>
    /// InureTime
    /// </summary>
    public DateTime? InureTime { get; set; }

    /// <summary>
    /// AbateTime
    /// </summary>
    public DateTime? AbateTime { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdStock.cs
*
*功 能： N / A
* 类 名： BdStock
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/8/6 15:55:33  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 仓库 (Model)
/// </summary>
[SugarTable("BdStock", "BdStock"), Entity(TableCnName = "仓库", TableName = "BdStock")]
public class BdStock : BasePoco
{

    /// <summary>
    /// 仓库编号
    /// </summary>
    [Display(Name = "StockNo"), Description("仓库编号"), MaxLength(32, ErrorMessage = "仓库编号 不能超过 32 个字符")]
    public string StockNo { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    [Display(Name = "StockNames"), Description("仓库名称"), MaxLength(32, ErrorMessage = "仓库名称 不能超过 32 个字符")]
    public string StockNames { get; set; }

    /// <summary>
    /// 仓库类别
    /// </summary>
    [Display(Name = "StockCategory"), Description("仓库类别"), MaxLength(32, ErrorMessage = "仓库类别 不能超过 32 个字符")]
    public string StockCategory { get; set; }

    /// <summary>
    /// 仓管员ID
    /// </summary>
    public Guid? StockKeeperId { get; set; }

    /// <summary>
    /// 是否虚拟
    /// </summary>
    public bool? IsVirtual { get; set; }

    /// <summary>
    /// 仓库地址
    /// </summary>
    [Display(Name = "Address"), Description("仓库地址"), MaxLength(32, ErrorMessage = "仓库地址 不能超过 32 个字符")]
    public string Address { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvIn.cs
*
* 功 能： N / A
* 类 名： IvIn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2024/12/18 15:40:26  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 库存入库单 (Model)
/// </summary>
[SugarTable("IvIn", "库存入库单"), Entity(TableCnName = "库存入库单", TableName = "IvIn")]
public class IvIn : BasePoco
{

    /// <summary>
    /// 单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("日期")]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    [Display(Name = "StockId"), Description("仓库ID")]
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    [Display(Name = "GoodsLocationId"), Description("货位ID")]
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    [Display(Name = "SupplyId"), Description("供应商ID")]
    public Guid? SupplyId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

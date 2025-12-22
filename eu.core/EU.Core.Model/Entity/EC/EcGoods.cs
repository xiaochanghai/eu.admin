/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EcGoods.cs
*
* 功 能： N / A
* 类 名： EcGoods
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/21 21:24:01  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 商品管理 (Model)
/// </summary>
[SugarTable("EcGoods", "商品管理"), Entity(TableCnName = "商品管理", TableName = "EcGoods")]
public class EcGoods : BasePoco
{

    /// <summary>
    /// 类型ID
    /// </summary>
    [Display(Name = "TypeId"), Description("类型ID"), SugarColumn(IsNullable = true, Length = 32)]
    public string TypeId { get; set; }

    /// <summary>
    /// 类型明细ID
    /// </summary>
    [Display(Name = "TypeDetailId"), Description("类型明细ID"), SugarColumn(IsNullable = true, Length = 32)]
    public string TypeDetailId { get; set; }

    /// <summary>
    /// 商品代码
    /// </summary>
    [Display(Name = "GoodsCode"), Description("商品代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string GoodsCode { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    [Display(Name = "GoodsName"), Description("商品名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string GoodsName { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    [Display(Name = "Brand"), Description("品牌"), SugarColumn(IsNullable = true, Length = 32)]
    public string Brand { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    [Display(Name = "ViewCount"), Description("访问次数"), SugarColumn(IsNullable = true)]
    public int? ViewCount { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    [Display(Name = "ImageUrl"), Description("图片URL"), SugarColumn(IsNullable = true, Length = 64)]
    public string ImageUrl { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

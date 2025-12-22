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

namespace EU.Core.Model.Base;

/// <summary>
/// 商品管理 (Dto.Base)
/// </summary>
public class EcGoodsBase : BasePoco
{

    /// <summary>
    /// 类型ID
    /// </summary>
    [Display(Name = "TypeId"), Description("类型ID"), MaxLength(32, ErrorMessage = "类型ID 不能超过 32 个字符")]
    public string TypeId { get; set; }

    /// <summary>
    /// 类型明细ID
    /// </summary>
    [Display(Name = "TypeDetailId"), Description("类型明细ID"), MaxLength(32, ErrorMessage = "类型明细ID 不能超过 32 个字符")]
    public string TypeDetailId { get; set; }

    /// <summary>
    /// 商品代码
    /// </summary>
    [Display(Name = "GoodsCode"), Description("商品代码"), MaxLength(32, ErrorMessage = "商品代码 不能超过 32 个字符")]
    public string GoodsCode { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    [Display(Name = "GoodsName"), Description("商品名称"), MaxLength(64, ErrorMessage = "商品名称 不能超过 64 个字符")]
    public string GoodsName { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    [Display(Name = "Brand"), Description("品牌"), MaxLength(32, ErrorMessage = "品牌 不能超过 32 个字符")]
    public string Brand { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    [Display(Name = "ViewCount"), Description("访问次数")]
    public int? ViewCount { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    [Display(Name = "ImageUrl"), Description("图片URL"), MaxLength(64, ErrorMessage = "图片URL 不能超过 64 个字符")]
    public string ImageUrl { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号")]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvCheckDetail.cs
*
*功 能： N / A
* 类 名： IvCheckDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/12/18 15:50:11  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 库存盘点单明细 (Dto.Base)
/// </summary>
public class IvCheckDetailBase
{

    /// <summary>
    /// 订单ID
    /// </summary>
    [Display(Name = "OrderId"), Description("订单ID")]
    public Guid? OrderId { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    [Display(Name = "SerialNumber"), Description("序号")]
    public int? SerialNumber { get; set; }

    /// <summary>
    /// 物料ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("物料ID")]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 初始数量
    /// </summary>
    [Display(Name = "QTY"), Description("初始数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? QTY { get; set; }

    /// <summary>
    /// 盘点数量
    /// </summary>
    [Display(Name = "Check_QTY"), Description("盘点数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? Check_QTY { get; set; }

    /// <summary>
    /// 批号/炉号
    /// </summary>
    [Display(Name = "BatchNo"), Description("批号/炉号"), MaxLength(32, ErrorMessage = "批号/炉号 不能超过 32 个字符")]
    public string BatchNo { get; set; }

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
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

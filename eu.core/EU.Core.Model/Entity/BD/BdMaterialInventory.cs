/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialInventory.cs
*
* 功 能： N / A
* 类 名： BdMaterialInventory
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/26 21:52:22  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 物料库存 (Model)
/// </summary>
[SugarTable("BdMaterialInventory", "物料库存"), Entity(TableCnName = "物料库存", TableName = "BdMaterialInventory")]
public class BdMaterialInventory : BasePoco
{

    /// <summary>
    /// 材质编号
    /// </summary>
    [Display(Name = "MaterialId"), Description("材质编号"), SugarColumn(IsNullable = true)]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    [Display(Name = "StockId"), Description("仓库ID"), SugarColumn(IsNullable = true)]
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    [Display(Name = "GoodsLocationId"), Description("货位ID"), SugarColumn(IsNullable = true)]
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 批号/炉号
    /// </summary>
    [Display(Name = "BatchNo"), Description("批号/炉号"), MaxLength(32, ErrorMessage = "批号/炉号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string BatchNo { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    [Display(Name = "QTY"), Description("数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? QTY { get; set; }

    /// <summary>
    /// 最近入库日期
    /// </summary>
    [Display(Name = "LastInTime"), Description("最近入库日期"), SugarColumn(IsNullable = true)]
    public DateTime? LastInTime { get; set; }

    /// <summary>
    /// 最近出库日期
    /// </summary>
    [Display(Name = "LastOutTime"), Description("最近出库日期"), SugarColumn(IsNullable = true)]
    public DateTime? LastOutTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

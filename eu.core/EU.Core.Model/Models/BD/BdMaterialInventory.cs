/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialInventory.cs
*
*功 能： N / A
* 类 名： BdMaterialInventory
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/25 17:57:14  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 物料库存 (Model)
/// </summary>
[SugarTable("BdMaterialInventory", "BdMaterialInventory"), Entity(TableCnName = "物料库存", TableName = "BdMaterialInventory")]
public class BdMaterialInventory : BasePoco
{

    /// <summary>
    /// 材质编号
    /// </summary>
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    [Display(Name = "QTY"), Description("数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? QTY { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

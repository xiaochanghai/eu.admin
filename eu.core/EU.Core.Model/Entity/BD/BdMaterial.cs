/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterial.cs
*
* 功 能： N / A
* 类 名： BdMaterial
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:29:41  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 物料管理 (Model)
/// </summary>
[SugarTable("BdMaterial", "物料管理"), Entity(TableCnName = "物料管理", TableName = "BdMaterial")]
public class BdMaterial : BasePoco
{

    /// <summary>
    /// 材质编号
    /// </summary>
    [Display(Name = "MaterialNo"), Description("材质编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string MaterialNo { get; set; }

    /// <summary>
    /// 材质名称
    /// </summary>
    [Display(Name = "MaterialNames"), Description("材质名称"), SugarColumn(IsNullable = true, Length = 128)]
    public string MaterialNames { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    [Display(Name = "Specifications"), Description("规格"), SugarColumn(IsNullable = true, Length = 512)]
    public string Specifications { get; set; }

    /// <summary>
    /// 材质ID
    /// </summary>
    [Display(Name = "TextureId"), Description("材质ID"), SugarColumn(IsNullable = true)]
    public Guid? TextureId { get; set; }

    /// <summary>
    /// 物料类型
    /// </summary>
    [Display(Name = "MaterialTypeId"), Description("物料类型"), SugarColumn(IsNullable = true)]
    public Guid? MaterialTypeId { get; set; }

    /// <summary>
    /// 物料类型
    /// </summary>
    [Display(Name = "MaterialTypeIds"), Description("物料类型"), SugarColumn(IsNullable = true, Length = 2000)]
    public string MaterialTypeIds { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [Display(Name = "Description"), Description("描述"), SugarColumn(IsNullable = true, Length = 500)]
    public string Description { get; set; }

    /// <summary>
    /// 存货计价
    /// </summary>
    [Display(Name = "InventoryValuation"), Description("存货计价"), Column(TypeName = "decimal(20,2)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 2)]
    public decimal? InventoryValuation { get; set; }

    /// <summary>
    /// 长
    /// </summary>
    [Display(Name = "Length"), Description("长"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 6)]
    public decimal? Length { get; set; }

    /// <summary>
    /// 宽
    /// </summary>
    [Display(Name = "Width"), Description("宽"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 6)]
    public decimal? Width { get; set; }

    /// <summary>
    /// 高
    /// </summary>
    [Display(Name = "Height"), Description("高"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 6)]
    public decimal? Height { get; set; }

    /// <summary>
    /// 颜色ID
    /// </summary>
    [Display(Name = "ColorId"), Description("颜色ID"), SugarColumn(IsNullable = true)]
    public Guid? ColorId { get; set; }

    /// <summary>
    /// 单重
    /// </summary>
    [Display(Name = "SingleWeight"), Description("单重"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 6)]
    public decimal? SingleWeight { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    [Display(Name = "UnitId"), Description("单位"), SugarColumn(IsNullable = true)]
    public Guid? UnitId { get; set; }

    /// <summary>
    /// 图号
    /// </summary>
    [Display(Name = "DrawingNo"), Description("图号"), SugarColumn(IsNullable = true, Length = 32)]
    public string DrawingNo { get; set; }

    /// <summary>
    /// 最小起订量
    /// </summary>
    [Display(Name = "MinOrder"), Description("最小起订量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 8)]
    public decimal? MinOrder { get; set; }

    /// <summary>
    /// 安全库存量
    /// </summary>
    [Display(Name = "SafetStock"), Description("安全库存量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 8)]
    public decimal? SafetStock { get; set; }

    /// <summary>
    /// 最小采购量
    /// </summary>
    [Display(Name = "MinPurchase"), Description("最小采购量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 8)]
    public decimal? MinPurchase { get; set; }

    /// <summary>
    /// 保质期
    /// </summary>
    [Display(Name = "ExpirationDate"), Description("保质期"), SugarColumn(IsNullable = true)]
    public int? ExpirationDate { get; set; }

    /// <summary>
    /// 是否批号管
    /// </summary>
    [Display(Name = "IsBatchControl"), Description("是否批号管"), SugarColumn(IsNullable = true)]
    public bool? IsBatchControl { get; set; }

    /// <summary>
    /// 检验方式（免检、抽检、全检）
    /// </summary>
    [Display(Name = "CheckMethod"), Description("检验方式（免检、抽检、全检）"), SugarColumn(IsNullable = true, Length = 32)]
    public string CheckMethod { get; set; }

    /// <summary>
    /// 采购价
    /// </summary>
    [Display(Name = "PurchasePrice"), Description("采购价"), Column(TypeName = "decimal(20,2)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 2)]
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// 最低销售价
    /// </summary>
    [Display(Name = "MinSalesPrice"), Description("最低销售价"), Column(TypeName = "decimal(20,2)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 2)]
    public decimal? MinSalesPrice { get; set; }

    /// <summary>
    /// 生产采购前置天数
    /// </summary>
    [Display(Name = "ProductionPurchasePreDays"), Description("生产采购前置天数"), SugarColumn(IsNullable = true)]
    public int? ProductionPurchasePreDays { get; set; }

    /// <summary>
    /// 生产采购周期
    /// </summary>
    [Display(Name = "ProductionPurchasePeriod"), Description("生产采购周期"), SugarColumn(IsNullable = true)]
    public int? ProductionPurchasePeriod { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    [Display(Name = "ImageUrl"), Description("图片URL"), SugarColumn(IsNullable = true, Length = 64)]
    public string ImageUrl { get; set; }

    /// <summary>
    /// 物料分类
    /// </summary>
    [Display(Name = "MaterialClassId"), Description("物料分类"), SugarColumn(IsNullable = true)]
    public Guid? MaterialClassId { get; set; }

    /// <summary>
    /// 重量
    /// </summary>
    [Display(Name = "Weight"), Description("重量"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 6)]
    public decimal? Weight { get; set; }

    /// <summary>
    /// 重量单位ID
    /// </summary>
    [Display(Name = "WeightUnitId"), Description("重量单位ID"), SugarColumn(IsNullable = true)]
    public Guid? WeightUnitId { get; set; }

    /// <summary>
    /// 来源类型
    /// </summary>
    [Display(Name = "Source"), Description("来源类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string Source { get; set; }
}

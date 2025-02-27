/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoReturnOrderDetail.cs
*
* 功 能： N / A
* 类 名： PoReturnOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:16  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 采购退货单明细 (Model)
/// </summary>
[SugarTable("PoReturnOrderDetail", "采购退货单明细"), Entity(TableCnName = "采购退货单明细", TableName = "PoReturnOrderDetail")]
public class PoReturnOrderDetail : BasePoco
{

    /// <summary>
    /// 订单ID
    /// </summary>
    [Display(Name = "OrderId"), Description("订单ID"), SugarColumn(IsNullable = true)]
    public Guid? OrderId { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    [Display(Name = "SerialNumber"), Description("序号"), SugarColumn(IsNullable = true)]
    public int? SerialNumber { get; set; }

    /// <summary>
    /// 单据来源
    /// </summary>
    [Display(Name = "OrderSource"), Description("单据来源"), SugarColumn(IsNullable = true, Length = 32)]
    public string OrderSource { get; set; }

    /// <summary>
    /// 请购单/销售单
    /// </summary>
    [Display(Name = "SourceOrderId"), Description("请购单/销售单"), SugarColumn(IsNullable = true)]
    public Guid? SourceOrderId { get; set; }

    /// <summary>
    /// 来源订单明细ID
    /// </summary>
    [Display(Name = "SourceOrderDetailId"), Description("来源订单明细ID"), SugarColumn(IsNullable = true)]
    public Guid? SourceOrderDetailId { get; set; }

    /// <summary>
    /// 货品ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("货品ID"), SugarColumn(IsNullable = true)]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    [Display(Name = "QTY"), Description("退货数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 8)]
    public decimal? QTY { get; set; }

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
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark4 { get; set; }
}

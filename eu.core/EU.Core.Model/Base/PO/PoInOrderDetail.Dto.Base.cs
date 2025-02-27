/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoInOrderDetail.cs
*
* 功 能： N / A
* 类 名： PoInOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:07  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 采购入库单明细 (Dto.Base)
/// </summary>
public class PoInOrderDetailBase : BasePoco
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
    /// 单据来源，采购单、到货通知单
    /// </summary>
    [Display(Name = "OrderSource"), Description("单据来源，采购单、到货通知单"), MaxLength(32, ErrorMessage = "单据来源，采购单、到货通知单 不能超过 32 个字符")]
    public string OrderSource { get; set; }

    /// <summary>
    /// 来源订单ID
    /// </summary>
    [Display(Name = "SourceOrderId"), Description("来源订单ID")]
    public Guid? SourceOrderId { get; set; }

    /// <summary>
    /// 来源订单明细ID
    /// </summary>
    [Display(Name = "SourceOrderDetailId"), Description("来源订单明细ID")]
    public Guid? SourceOrderDetailId { get; set; }

    /// <summary>
    /// 物料ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("物料ID")]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 入库数量
    /// </summary>
    [Display(Name = "InQTY"), Description("入库数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? InQTY { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    [Display(Name = "ReturnQTY"), Description("退货数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? ReturnQTY { get; set; }

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
    /// 批次号ID
    /// </summary>
    [Display(Name = "BatchNo"), Description("批次号ID"), MaxLength(32, ErrorMessage = "批次号ID 不能超过 32 个字符")]
    public string BatchNo { get; set; }

    /// <summary>
    /// 炉号
    /// </summary>
    [Display(Name = "FurnaceNo"), Description("炉号"), MaxLength(32, ErrorMessage = "炉号 不能超过 32 个字符")]
    public string FurnaceNo { get; set; }

    /// <summary>
    /// 托盘
    /// </summary>
    [Display(Name = "TrayNo"), Description("托盘"), MaxLength(32, ErrorMessage = "托盘 不能超过 32 个字符")]
    public string TrayNo { get; set; }

    /// <summary>
    /// 预定交期
    /// </summary>
    [Display(Name = "DeliveryDate"), Description("预定交期")]
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark4 { get; set; }
}

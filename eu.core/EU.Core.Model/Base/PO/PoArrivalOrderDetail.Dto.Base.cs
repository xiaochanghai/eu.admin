/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoArrivalOrderDetail.cs
*
* 功 能： N / A
* 类 名： PoArrivalOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:08:25  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 采购到货通知单明细 (Dto.Base)
/// </summary>
public class PoArrivalOrderDetailBase : BasePoco
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
    /// 单据来源，采购单
    /// </summary>
    [Display(Name = "OrderSource"), Description("单据来源，采购单"), MaxLength(32, ErrorMessage = "单据来源，采购单 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string OrderSource { get; set; }

    /// <summary>
    /// 来源订单ID
    /// </summary>
    [Display(Name = "SourceOrderId"), Description("来源订单ID"), SugarColumn(IsNullable = true)]
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
    /// 到货数量
    /// </summary>
    [Display(Name = "NoticeQTY"), Description("到货数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? NoticeQTY { get; set; }

    /// <summary>
    /// 送达日期
    /// </summary>
    [Display(Name = "DeliveryDate"), Description("送达日期"), SugarColumn(IsNullable = true)]
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 投检数量
    /// </summary>
    [Display(Name = "CheckQTY"), Description("投检数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? CheckQTY { get; set; }

    /// <summary>
    /// 入库数量
    /// </summary>
    [Display(Name = "InQTY"), Description("入库数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? InQTY { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark4 { get; set; }
}

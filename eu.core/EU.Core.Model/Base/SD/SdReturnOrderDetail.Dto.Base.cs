/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrderDetail.cs
*
* 功 能： N / A
* 类 名： SdReturnOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:27  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 退货单明细 (Dto.Base)
/// </summary>
public class SdReturnOrderDetailBase : BasePoco
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
    /// 出库订单ID
    /// </summary>
    [Display(Name = "OutOrderId"), Description("出库订单ID")]
    public Guid? OutOrderId { get; set; }

    /// <summary>
    /// 出库订单明细ID
    /// </summary>
    [Display(Name = "OutOrderDetailId"), Description("出库订单明细ID")]
    public Guid? OutOrderDetailId { get; set; }

    /// <summary>
    /// 销售订单ID
    /// </summary>
    [Display(Name = "SalesOrderId"), Description("销售订单ID")]
    public Guid? SalesOrderId { get; set; }

    /// <summary>
    /// 销售订单明细ID
    /// </summary>
    [Display(Name = "SalesOrderDetailId"), Description("销售订单明细ID")]
    public Guid? SalesOrderDetailId { get; set; }

    /// <summary>
    /// 货品ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("货品ID")]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    [Display(Name = "ReturnQTY"), Description("退货数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? ReturnQTY { get; set; }

    /// <summary>
    /// 客户物料编码
    /// </summary>
    [Display(Name = "CustomerMaterialCode"), Description("客户物料编码"), MaxLength(64, ErrorMessage = "客户物料编码 不能超过 64 个字符")]
    public string CustomerMaterialCode { get; set; }

    /// <summary>
    /// 批次号ID
    /// </summary>
    [Display(Name = "BatchId"), Description("批次号ID")]
    public Guid? BatchId { get; set; }

    /// <summary>
    /// 退回仓库ID
    /// </summary>
    [Display(Name = "StockId"), Description("退回仓库ID")]
    public Guid? StockId { get; set; }

    /// <summary>
    /// 退回货位ID
    /// </summary>
    [Display(Name = "GoodsLocationId"), Description("退回货位ID")]
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 退货日期
    /// </summary>
    [Display(Name = "ReturnDate"), Description("退货日期")]
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// 退货状态--待退回、已退回
    /// </summary>
    [Display(Name = "ReturnStatus"), Description("退货状态--待退回、已退回"), MaxLength(32, ErrorMessage = "退货状态--待退回、已退回 不能超过 32 个字符")]
    public string ReturnStatus { get; set; }

    /// <summary>
    /// 是否实物退货
    /// </summary>
    [Display(Name = "IsEntity"), Description("是否实物退货")]
    public bool? IsEntity { get; set; }

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

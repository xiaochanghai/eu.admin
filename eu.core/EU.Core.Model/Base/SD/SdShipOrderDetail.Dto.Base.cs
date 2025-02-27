/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdShipOrderDetail.cs
*
* 功 能： N / A
* 类 名： SdShipOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:30  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 发货单明细 (Dto.Base)
/// </summary>
public class SdShipOrderDetailBase : BasePoco
{

    /// <summary>
    /// 序号
    /// </summary>
    [Display(Name = "SerialNumber"), Description("序号")]
    public int? SerialNumber { get; set; }

    /// <summary>
    /// 订单ID
    /// </summary>
    [Display(Name = "OrderId"), Description("订单ID")]
    public Guid? OrderId { get; set; }

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
    /// 物料ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("物料ID")]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 发货数量
    /// </summary>
    [Display(Name = "ShipQTY"), Description("发货数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? ShipQTY { get; set; }

    /// <summary>
    /// 未发数量
    /// </summary>
    [Display(Name = "OutQTY"), Description("未发数量"), Column(TypeName = "decimal(20,8)")]
    public decimal? OutQTY { get; set; }

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

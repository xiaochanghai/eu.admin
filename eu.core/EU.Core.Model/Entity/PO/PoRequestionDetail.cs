/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoRequestionDetail.cs
*
* 功 能： N / A
* 类 名： PoRequestionDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:12:20  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 请购单明细 (Model)
/// </summary>
[SugarTable("PoRequestionDetail", "请购单明细"), Entity(TableCnName = "请购单明细", TableName = "PoRequestionDetail")]
public class PoRequestionDetail : BasePoco
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
    /// 请购来源
    /// </summary>
    [Display(Name = "Source"), Description("请购来源"), MaxLength(32, ErrorMessage = "请购来源 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Source { get; set; }

    /// <summary>
    /// 请购来源单ID
    /// </summary>
    [Display(Name = "SourceOrderId"), Description("请购来源单ID"), SugarColumn(IsNullable = true)]
    public Guid? SourceOrderId { get; set; }

    /// <summary>
    /// 请购来源单明细ID
    /// </summary>
    [Display(Name = "SourceOrderDetailId"), Description("请购来源单明细ID"), SugarColumn(IsNullable = true)]
    public Guid? SourceOrderDetailId { get; set; }

    /// <summary>
    /// 物料ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("物料ID"), SugarColumn(IsNullable = true)]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 请购数量
    /// </summary>
    [Display(Name = "QTY"), Description("请购数量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? QTY { get; set; }

    /// <summary>
    /// 需求日期
    /// </summary>
    [Display(Name = "RequestionDate"), Description("需求日期"), SugarColumn(IsNullable = true)]
    public DateTime? RequestionDate { get; set; }

    /// <summary>
    /// 已采购数
    /// </summary>
    [Display(Name = "PurchaseQTY"), Description("已采购数"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true)]
    public decimal? PurchaseQTY { get; set; }

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

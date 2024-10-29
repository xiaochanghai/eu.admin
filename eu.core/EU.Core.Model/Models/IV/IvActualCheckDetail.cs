/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvActualCheckDetail.cs
*
*功 能： N / A
* 类 名： IvActualCheckDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 11:55:17  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/ 
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace EU.Core.Model.Models
{

    /// <summary>
    /// 实际盘点单明细 (Model)
    /// </summary>
    [SugarTable("IvActualCheckDetail", "IvActualCheckDetail"), Entity(TableCnName = "实际盘点单明细", TableName = "IvActualCheckDetail")]
    public class IvActualCheckDetail : BasePoco
    {

        /// <summary>
        /// 订单ID
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int? SerialNumber { get; set; }

        /// <summary>
        /// 来源订单ID
        /// </summary>
        public Guid? SourceOrderId { get; set; }

        /// <summary>
        /// 来源单号
        /// </summary>
        [Display(Name = "SourceOrderNo"), Description("来源单号"), MaxLength(32, ErrorMessage = "来源单号 不能超过 32 个字符")]
        public string SourceOrderNo { get; set; }

        /// <summary>
        /// 来源订单明细ID
        /// </summary>
        public Guid? SourceOrderDetailId { get; set; }

        /// <summary>
        /// 盘点名称
        /// </summary>
        [Display(Name = "CheckName"), Description("盘点名称"), MaxLength(32, ErrorMessage = "盘点名称 不能超过 32 个字符")]
        public string CheckName { get; set; }

        /// <summary>
        /// 货品ID
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
        /// 库存数量
        /// </summary>
        [Display(Name = "QTY"), Description("库存数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 实盘数量
        /// </summary>
        [Display(Name = "ActualQTY"), Description("实盘数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? ActualQTY { get; set; }

        /// <summary>
        /// 盘点盈亏，实际盘点数量-账面库存数量，结果为正数，则表示盘盈，结果为负数，则表示盘亏
        /// </summary>
        [Display(Name = "DiffQTY"), Description("盘点盈亏，实际盘点数量-账面库存数量，结果为正数，则表示盘盈，结果为负数，则表示盘亏"), Column(TypeName = "decimal(20,8)")]
        public decimal? DiffQTY { get; set; }

        /// <summary>
        /// 盘盈/盘亏
        /// </summary>
        [Display(Name = "ProfitLoss"), Description("盘盈/盘亏"), MaxLength(32, ErrorMessage = "盘盈/盘亏 不能超过 32 个字符")]
        public string ProfitLoss { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [Display(Name = "BatchNo"), Description("批次号"), MaxLength(32, ErrorMessage = "批次号 不能超过 32 个字符")]
        public string BatchNo { get; set; }

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
}

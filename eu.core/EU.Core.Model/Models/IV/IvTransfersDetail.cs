/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvTransfersDetail.cs
*
*功 能： N / A
* 类 名： IvTransfersDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:05:38  SimonHsiao   初版
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
    /// 库存调拨单明细 (Model)
    /// </summary>
    [SugarTable("IvTransfersDetail", "IvTransfersDetail"), Entity(TableCnName = "库存调拨单明细", TableName = "IvTransfersDetail")]
    public class IvTransfersDetail : BasePoco
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
        /// 货品ID
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Display(Name = "QTY"), Description("数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [Display(Name = "Price"), Description("单价"), Column(TypeName = "decimal(20,2)")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [Display(Name = "BatchNo"), Description("批次号"), MaxLength(32, ErrorMessage = "批次号 不能超过 32 个字符")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 移出仓库ID
        /// </summary>
        public Guid? OutStockId { get; set; }

        /// <summary>
        /// 移出货位ID
        /// </summary>
        public Guid? OutGoodsLocationId { get; set; }

        /// <summary>
        /// 移入仓库ID
        /// </summary>
        public Guid? InStockId { get; set; }

        /// <summary>
        /// 移入货位ID
        /// </summary>
        public Guid? InGoodsLocationId { get; set; }

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

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvInitDetail.cs
*
*功 能： N / A
* 类 名： IvInitDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 11:44:39  SimonHsiao   初版
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

namespace EU.Core.Model.Models
{

    /// <summary>
    /// 库存初始化明细 (Dto.Base)
    /// </summary>
    public class IvInitDetailBase
    {

        /// <summary>
        /// 序号
        /// </summary>
        public int? SerialNumber { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// 物料ID
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Display(Name = "QTY"), Description("数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [Display(Name = "BatchNo"), Description("批次号"), MaxLength(32, ErrorMessage = "批次号 不能超过 32 个字符")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 仓库ID
        /// </summary>
        public Guid? StockId { get; set; }

        /// <summary>
        /// 货位ID
        /// </summary>
        public Guid? GoodsLocationId { get; set; }

        /// <summary>
        /// 初始化时间
        /// </summary>
        public DateTime? InitTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

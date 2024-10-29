/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdRequireAnalysis.cs
*
*功 能： N / A
* 类 名： PdRequireAnalysis
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:40:01  SimonHsiao   初版
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
    /// PdRequireAnalysis (Dto.Base)
    /// </summary>
    public class PdRequireAnalysisBase
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
        /// 来源，销售单、生产计划单
        /// </summary>
        [Display(Name = "Source"), Description("来源，销售单、生产计划单"), MaxLength(32, ErrorMessage = "来源，销售单、生产计划单 不能超过 32 个字符")]
        public string Source { get; set; }

        /// <summary>
        /// 来源单ID
        /// </summary>
        public Guid? SourceOrderId { get; set; }

        /// <summary>
        /// 来源单明细ID
        /// </summary>
        public Guid? SourceOrderDetailId { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        [Display(Name = "OrderNo"), Description("订单号"), MaxLength(32, ErrorMessage = "订单号 不能超过 32 个字符")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 项次
        /// </summary>
        public int? SourceOrderSerialNumber { get; set; }

        /// <summary>
        /// 物料ID
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 订单数量
        /// </summary>
        [Display(Name = "OrderQTY"), Description("订单数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? OrderQTY { get; set; }

        /// <summary>
        /// 增加比例
        /// </summary>
        [Display(Name = "AddRate"), Description("增加比例"), Column(TypeName = "decimal(20,2)")]
        public decimal? AddRate { get; set; }

        /// <summary>
        /// 分析数量
        /// </summary>
        [Display(Name = "QTY"), Description("分析数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

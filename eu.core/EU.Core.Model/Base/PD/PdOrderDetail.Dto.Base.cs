/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdOrderDetail.cs
*
*功 能： N / A
* 类 名： PdOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:39:50  SimonHsiao   初版
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
    /// PdOrderDetail (Dto.Base)
    /// </summary>
    public class PdOrderDetailBase
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
        /// 来源单ID
        /// </summary>
        public Guid? SourceOrderId { get; set; }

        /// <summary>
        /// 来源单明细ID
        /// </summary>
        public Guid? SourceOrderDetailId { get; set; }

        /// <summary>
        /// 来源项次
        /// </summary>
        [Display(Name = "SourceSerialNumber"), Description("来源项次"), Column(TypeName = "decimal(20,8)")]
        public decimal? SourceSerialNumber { get; set; }

        /// <summary>
        /// 客户物料编码
        /// </summary>
        [Display(Name = "CustomerMaterialCode"), Description("客户物料编码"), MaxLength(64, ErrorMessage = "客户物料编码 不能超过 64 个字符")]
        public string CustomerMaterialCode { get; set; }

        /// <summary>
        /// 订单数量
        /// </summary>
        [Display(Name = "QTY"), Description("订单数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 订单交期
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

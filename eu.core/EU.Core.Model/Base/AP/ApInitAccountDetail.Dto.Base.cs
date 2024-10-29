/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApInitAccountDetail.cs
*
*功 能： N / A
* 类 名： ApInitAccountDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/26 15:28:09  SimonHsiao   初版
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
    /// 应付期初建账明细 (Dto.Base)
    /// </summary>
    public class ApInitAccountDetailBase
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
        /// 单据编号
        /// </summary>
        [Display(Name = "OrderNo"), Description("单据编号"), MaxLength(32, ErrorMessage = "单据编号 不能超过 32 个字符")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 单据名称
        /// </summary>
        [Display(Name = "OrderName"), Description("单据名称"), MaxLength(32, ErrorMessage = "单据名称 不能超过 32 个字符")]
        public string OrderName { get; set; }

        /// <summary>
        /// 单据日期
        /// </summary>
        public DateTime? OrderDate { get; set; }

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
        /// 单价
        /// </summary>
        [Display(Name = "Price"), Description("单价"), Column(TypeName = "decimal(20,2)")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 税率
        /// </summary>
        [Display(Name = "TaxRate"), Description("税率"), Column(TypeName = "decimal(20,6)")]
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// 未税金额
        /// </summary>
        [Display(Name = "NoTaxAmount"), Description("未税金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? NoTaxAmount { get; set; }

        /// <summary>
        /// 含税金额
        /// </summary>
        [Display(Name = "TaxIncludedAmount"), Description("含税金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? TaxIncludedAmount { get; set; }

        /// <summary>
        /// 结账数量
        /// </summary>
        [Display(Name = "CheckOutQTY"), Description("结账数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? CheckOutQTY { get; set; }

        /// <summary>
        /// 结账金额
        /// </summary>
        [Display(Name = "CheckOutAmount"), Description("结账金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? CheckOutAmount { get; set; }

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

        /// <summary>
        /// TaxAmount
        /// </summary>
        [Display(Name = "TaxAmount"), Description("TaxAmount"), Column(TypeName = "decimal(20,2)")]
        public decimal? TaxAmount { get; set; }
    }
}

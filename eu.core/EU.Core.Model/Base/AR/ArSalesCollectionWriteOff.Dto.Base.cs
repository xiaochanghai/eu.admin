/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ArSalesCollectionWriteOff.cs
*
*功 能： N / A
* 类 名： ArSalesCollectionWriteOff
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 11:18:05  SimonHsiao   初版
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
    /// 销售收款核销明细 (Dto.Base)
    /// </summary>
    public class ArSalesCollectionWriteOffBase
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
        /// 单据来源
        /// </summary>
        [Display(Name = "OrderSource"), Description("单据来源"), MaxLength(32, ErrorMessage = "单据来源 不能超过 32 个字符")]
        public string OrderSource { get; set; }

        /// <summary>
        /// 来源单ID
        /// </summary>
        public Guid? SourceOrderId { get; set; }

        /// <summary>
        /// 来源单号
        /// </summary>
        [Display(Name = "SourceOrderNo"), Description("来源单号"), MaxLength(32, ErrorMessage = "来源单号 不能超过 32 个字符")]
        public string SourceOrderNo { get; set; }

        /// <summary>
        /// 来源单明细ID
        /// </summary>
        public Guid? SourceOrderDetailId { get; set; }

        /// <summary>
        /// 发票号
        /// </summary>
        [Display(Name = "InvoiceNo"), Description("发票号"), MaxLength(32, ErrorMessage = "发票号 不能超过 32 个字符")]
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 开票日期
        /// </summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>
        /// 币别
        /// </summary>
        public Guid? CurrencyId { get; set; }

        /// <summary>
        /// 发票税率
        /// </summary>
        [Display(Name = "InvoiceRate"), Description("发票税率"), Column(TypeName = "decimal(20,2)")]
        public decimal? InvoiceRate { get; set; }

        /// <summary>
        /// 发票金额
        /// </summary>
        [Display(Name = "InvoiceAmount"), Description("发票金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? InvoiceAmount { get; set; }

        /// <summary>
        /// 预收金额
        /// </summary>
        [Display(Name = "ReceivableAmount"), Description("预收金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? ReceivableAmount { get; set; }

        /// <summary>
        /// 未收金额
        /// </summary>
        [Display(Name = "NoCollectionAmount"), Description("未收金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? NoCollectionAmount { get; set; }

        /// <summary>
        /// 实收金额
        /// </summary>
        [Display(Name = "ActualCollectionAmount"), Description("实收金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? ActualCollectionAmount { get; set; }

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

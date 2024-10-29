/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ArInvoiceAssociation.cs
*
*功 能： N / A
* 类 名： ArInvoiceAssociation
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 10:52:48  SimonHsiao   初版
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
    /// 应收开票对应发票 (Model)
    /// </summary>
    [SugarTable("ArInvoiceAssociation", "ArInvoiceAssociation"), Entity(TableCnName = "应收开票对应发票", TableName = "ArInvoiceAssociation")]
    public class ArInvoiceAssociation : BasePoco
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
        /// 发票号码
        /// </summary>
        [Display(Name = "InvoiceNo"), Description("发票号码"), MaxLength(32, ErrorMessage = "发票号码 不能超过 32 个字符")]
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 发票日期
        /// </summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>
        /// 发票税率
        /// </summary>
        [Display(Name = "TaxRate"), Description("发票税率"), Column(TypeName = "decimal(20,2)")]
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// 未税金额
        /// </summary>
        [Display(Name = "NoTaxAmount"), Description("未税金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? NoTaxAmount { get; set; }

        /// <summary>
        /// 税额
        /// </summary>
        [Display(Name = "TaxAmount"), Description("税额"), Column(TypeName = "decimal(20,2)")]
        public decimal? TaxAmount { get; set; }

        /// <summary>
        /// 含税金额
        /// </summary>
        [Display(Name = "TaxIncludedAmount"), Description("含税金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? TaxIncludedAmount { get; set; }

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

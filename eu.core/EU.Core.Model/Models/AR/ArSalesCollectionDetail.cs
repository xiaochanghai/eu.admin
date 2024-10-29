/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ArSalesCollectionDetail.cs
*
*功 能： N / A
* 类 名： ArSalesCollectionDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 11:13:49  SimonHsiao   初版
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
    /// 销售收款单明细 (Model)
    /// </summary>
    [SugarTable("ArSalesCollectionDetail", "ArSalesCollectionDetail"), Entity(TableCnName = "销售收款单明细", TableName = "ArSalesCollectionDetail")]
    public class ArSalesCollectionDetail : BasePoco
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
        /// 收款方式（转账、支票、承兑、现金、其他）
        /// </summary>
        [Display(Name = "CollectionType"), Description("收款方式（转账、支票、承兑、现金、其他）"), MaxLength(32, ErrorMessage = "收款方式（转账、支票、承兑、现金、其他） 不能超过 32 个字符")]
        public string CollectionType { get; set; }

        /// <summary>
        /// 收款金额
        /// </summary>
        [Display(Name = "CollectionAmount"), Description("收款金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? CollectionAmount { get; set; }

        /// <summary>
        /// 收款日期
        /// </summary>
        public DateTime? CollectionDate { get; set; }

        /// <summary>
        /// 银行名称
        /// </summary>
        [Display(Name = "BankName"), Description("银行名称"), MaxLength(32, ErrorMessage = "银行名称 不能超过 32 个字符")]
        public string BankName { get; set; }

        /// <summary>
        /// 发票号
        /// </summary>
        [Display(Name = "InvoiceNo"), Description("发票号"), MaxLength(32, ErrorMessage = "发票号 不能超过 32 个字符")]
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 到期日
        /// </summary>
        public DateTime? DueDate { get; set; }

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

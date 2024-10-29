/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApInitAccountOrder.cs
*
*功 能： N / A
* 类 名： ApInitAccountOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/26 15:30:41  SimonHsiao   初版
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
    /// 应付期初建账 (Model)
    /// </summary>
    [SugarTable("ApInitAccountOrder", "ApInitAccountOrder"), Entity(TableCnName = "应付期初建账", TableName = "ApInitAccountOrder")]
    public class ApInitAccountOrder : BasePoco
    {

        /// <summary>
        /// 单号
        /// </summary>
        [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 订单日期
        /// </summary>
        public DateTime? OrderDate { get; set; }

        /// <summary>
        /// 供应商ID
        /// </summary>
        public Guid? SupplierId { get; set; }

        /// <summary>
        /// 发票抬头
        /// </summary>
        [Display(Name = "InvoiceTitle"), Description("发票抬头"), MaxLength(64, ErrorMessage = "发票抬头 不能超过 64 个字符")]
        public string InvoiceTitle { get; set; }

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
        /// 建账年月
        /// </summary>
        public DateTime? YearMonth { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        [Display(Name = "InitAccountOrderStatus"), Description("订单状态"), MaxLength(32, ErrorMessage = "订单状态 不能超过 32 个字符")]
        public string InitAccountOrderStatus { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

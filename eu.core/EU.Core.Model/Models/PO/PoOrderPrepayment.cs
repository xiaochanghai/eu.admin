/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoOrderPrepayment.cs
*
*功 能： N / A
* 类 名： PoOrderPrepayment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 15:56:05  SimonHsiao   初版
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
    /// 采购单预付账款 (Model)
    /// </summary>
    [SugarTable("PoOrderPrepayment", "PoOrderPrepayment"), Entity(TableCnName = "采购单预付账款", TableName = "PoOrderPrepayment")]
    public class PoOrderPrepayment : BasePoco
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
        /// 预付比例
        /// </summary>
        [Display(Name = "Percent"), Description("预付比例"), Column(TypeName = "decimal(20,2)")]
        public decimal? Percent { get; set; }

        /// <summary>
        /// 预付金额
        /// </summary>
        [Display(Name = "Amount"), Description("预付金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// 预付时间
        /// </summary>
        public DateTime? PayTime { get; set; }

        /// <summary>
        /// 已付金额
        /// </summary>
        [Display(Name = "HasAmount"), Description("已付金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? HasAmount { get; set; }

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

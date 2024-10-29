/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApPaymentOrder.cs
*
*功 能： N / A
* 类 名： ApPaymentOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/26 17:49:20  SimonHsiao   初版
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
    /// 采购付款单 (Model)
    /// </summary>
    [SugarTable("ApPaymentOrder", "ApPaymentOrder"), Entity(TableCnName = "采购付款单", TableName = "ApPaymentOrder")]
    public class ApPaymentOrder : BasePoco
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
        /// 付款类型
        /// </summary>
        [Display(Name = "ApPaymentType"), Description("付款类型"), MaxLength(32, ErrorMessage = "付款类型 不能超过 32 个字符")]
        public string ApPaymentType { get; set; }

        /// <summary>
        /// 供应商ID
        /// </summary>
        public Guid? SupplierId { get; set; }

        /// <summary>
        /// 冲销金额
        /// </summary>
        [Display(Name = "Amount"), Description("冲销金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// 实收金额
        /// </summary>
        [Display(Name = "ActualAmount"), Description("实收金额"), Column(TypeName = "decimal(20,2)")]
        public decimal? ActualAmount { get; set; }

        /// <summary>
        /// 收款差额
        /// </summary>
        public DateTime? DiffAmount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

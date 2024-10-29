/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* MfInOrderDetail.cs
*
*功 能： N / A
* 类 名： MfInOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:30:04  SimonHsiao   初版
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
    /// MfInOrderDetail (Model)
    /// </summary>
    [SugarTable("MfInOrderDetail", "MfInOrderDetail"), Entity(TableCnName = "MfInOrderDetail", TableName = "MfInOrderDetail")]
    public class MfInOrderDetail : BasePoco
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
        /// 模具ID
        /// </summary>
        public Guid? MouldId { get; set; }

        /// <summary>
        /// 币别ID
        /// </summary>
        public Guid? CurrencyId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Display(Name = "QTY"), Description("数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        [Display(Name = "Price"), Description("价格"), Column(TypeName = "decimal(20,2)")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 仓库ID
        /// </summary>
        public Guid? StockId { get; set; }

        /// <summary>
        /// 货位ID
        /// </summary>
        public Guid? GoodsLocationId { get; set; }

        /// <summary>
        /// 批次号ID
        /// </summary>
        public Guid? BatchId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

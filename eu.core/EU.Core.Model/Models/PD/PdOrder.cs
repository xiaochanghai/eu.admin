/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdOrder.cs
*
*功 能： N / A
* 类 名： PdOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:39:49  SimonHsiao   初版
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
    /// PdOrder (Model)
    /// </summary>
    [SugarTable("PdOrder", "PdOrder"), Entity(TableCnName = "PdOrder", TableName = "PdOrder")]
    public class PdOrder : BasePoco
    {

        /// <summary>
        /// 单号
        /// </summary>
        [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 作业日期
        /// </summary>
        public DateTime? OrderDate { get; set; }

        /// <summary>
        /// 货品编号
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 生产数量
        /// </summary>
        [Display(Name = "QTY"), Description("生产数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        [Display(Name = "Source"), Description("来源"), MaxLength(32, ErrorMessage = "来源 不能超过 32 个字符")]
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
        /// 来源单号
        /// </summary>
        [Display(Name = "SourceOrderNo"), Description("来源单号"), MaxLength(32, ErrorMessage = "来源单号 不能超过 32 个字符")]
        public string SourceOrderNo { get; set; }

        /// <summary>
        /// 紧急程度
        /// </summary>
        [Display(Name = "UrgentType"), Description("紧急程度"), MaxLength(32, ErrorMessage = "紧急程度 不能超过 32 个字符")]
        public string UrgentType { get; set; }

        /// <summary>
        /// 已完工数量
        /// </summary>
        [Display(Name = "CompleteQTY"), Description("已完工数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? CompleteQTY { get; set; }

        /// <summary>
        /// 预开工日
        /// </summary>
        public DateTime? PlanStartDate { get; set; }

        /// <summary>
        /// 预完工日
        /// </summary>
        public DateTime? PlanEndDate { get; set; }

        /// <summary>
        /// 车间ID
        /// </summary>
        public Guid? WorkShopId { get; set; }

        /// <summary>
        /// 需求日期
        /// </summary>
        public DateTime? RequireDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

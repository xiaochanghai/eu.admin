/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdPlanDetail.cs
*
*功 能： N / A
* 类 名： PdPlanDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:39:57  SimonHsiao   初版
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
    /// PdPlanDetail (Dto.Base)
    /// </summary>
    public class PdPlanDetailBase
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
        /// 物料ID
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 补发数量
        /// </summary>
        [Display(Name = "QTY"), Description("补发数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? QTY { get; set; }

        /// <summary>
        /// 需求日期
        /// </summary>
        public DateTime? RequireDate { get; set; }

        /// <summary>
        /// BOMId
        /// </summary>
        public Guid? BOMId { get; set; }

        /// <summary>
        /// 完成数量
        /// </summary>
        [Display(Name = "CompleteQTY"), Description("完成数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? CompleteQTY { get; set; }

        /// <summary>
        /// 转生产量
        /// </summary>
        [Display(Name = "TransferQTY"), Description("转生产量"), Column(TypeName = "decimal(20,8)")]
        public decimal? TransferQTY { get; set; }

        /// <summary>
        /// 客户物料编号
        /// </summary>
        [Display(Name = "CustomerMaterialNo"), Description("客户物料编号"), MaxLength(32, ErrorMessage = "客户物料编号 不能超过 32 个字符")]
        public string CustomerMaterialNo { get; set; }

        /// <summary>
        /// 客户物料名称
        /// </summary>
        [Display(Name = "CustomerMaterialName"), Description("客户物料名称"), MaxLength(32, ErrorMessage = "客户物料名称 不能超过 32 个字符")]
        public string CustomerMaterialName { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

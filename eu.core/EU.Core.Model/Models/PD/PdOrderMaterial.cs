/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdOrderMaterial.cs
*
*功 能： N / A
* 类 名： PdOrderMaterial
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:39:51  SimonHsiao   初版
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
    /// PdOrderMaterial (Model)
    /// </summary>
    [SugarTable("PdOrderMaterial", "PdOrderMaterial"), Entity(TableCnName = "PdOrderMaterial", TableName = "PdOrderMaterial")]
    public class PdOrderMaterial : BasePoco
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
        /// 单位用量
        /// </summary>
        [Display(Name = "Dosage"), Description("单位用量"), Column(TypeName = "decimal(20,8)")]
        public decimal? Dosage { get; set; }

        /// <summary>
        /// 损耗率
        /// </summary>
        [Display(Name = "WastageRate"), Description("损耗率"), Column(TypeName = "decimal(20,2)")]
        public decimal? WastageRate { get; set; }

        /// <summary>
        /// 应发数量
        /// </summary>
        [Display(Name = "ShouldQTY"), Description("应发数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? ShouldQTY { get; set; }

        /// <summary>
        /// 实发数量
        /// </summary>
        [Display(Name = "ActualQTY"), Description("实发数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? ActualQTY { get; set; }

        /// <summary>
        /// 状态，状态 未发料/部分发料/已发料
        /// </summary>
        [Display(Name = "PdOrderMaterialStatus"), Description("状态，状态 未发料/部分发料/已发料"), MaxLength(32, ErrorMessage = "状态，状态 未发料/部分发料/已发料 不能超过 32 个字符")]
        public string PdOrderMaterialStatus { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

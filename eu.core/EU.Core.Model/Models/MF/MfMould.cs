/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* MfMould.cs
*
*功 能： N / A
* 类 名： MfMould
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:30:06  SimonHsiao   初版
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
    /// MfMould (Model)
    /// </summary>
    [SugarTable("MfMould", "MfMould"), Entity(TableCnName = "MfMould", TableName = "MfMould")]
    public class MfMould : BasePoco
    {

        /// <summary>
        /// 工模治具编号
        /// </summary>
        [Display(Name = "MouldNo"), Description("工模治具编号"), MaxLength(64, ErrorMessage = "工模治具编号 不能超过 64 个字符")]
        public string MouldNo { get; set; }

        /// <summary>
        /// 工模治具名称
        /// </summary>
        [Display(Name = "MouldName"), Description("工模治具名称"), MaxLength(64, ErrorMessage = "工模治具名称 不能超过 64 个字符")]
        public string MouldName { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        [Display(Name = "Specifications"), Description("规格"), MaxLength(32, ErrorMessage = "规格 不能超过 32 个字符")]
        public string Specifications { get; set; }

        /// <summary>
        /// 材质ID
        /// </summary>
        public Guid? TextureId { get; set; }

        /// <summary>
        /// 单位ID
        /// </summary>
        public Guid? UnitId { get; set; }

        /// <summary>
        /// 工模治具类别ID
        /// </summary>
        public Guid? MouldTypeId { get; set; }

        /// <summary>
        /// 模穴数
        /// </summary>
        public int? QTY { get; set; }

        /// <summary>
        /// 成型时间（S）
        /// </summary>
        [Display(Name = "MoldingTime"), Description("成型时间（S）"), Column(TypeName = "decimal(20,2)")]
        public decimal? MoldingTime { get; set; }

        /// <summary>
        /// 现有数量
        /// </summary>
        [Display(Name = "CurrentQTY"), Description("现有数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? CurrentQTY { get; set; }

        /// <summary>
        /// 可用数量
        /// </summary>
        [Display(Name = "AvailableQTY"), Description("可用数量"), Column(TypeName = "decimal(20,8)")]
        public decimal? AvailableQTY { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Display(Name = "MouldDesc"), Description("描述"), MaxLength(2000, ErrorMessage = "描述 不能超过 2000 个字符")]
        public string MouldDesc { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

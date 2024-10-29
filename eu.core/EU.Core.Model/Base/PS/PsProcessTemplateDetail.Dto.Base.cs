/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessTemplateDetail.cs
*
*功 能： N / A
* 类 名： PsProcessTemplateDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:32  SimonHsiao   初版
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
    /// PsProcessTemplateDetail (Dto.Base)
    /// </summary>
    public class PsProcessTemplateDetailBase
    {

        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int? SerialNumber { get; set; }

        /// <summary>
        /// 工序ID
        /// </summary>
        public Guid? ProcessId { get; set; }

        /// <summary>
        /// 机台ID
        /// </summary>
        public Guid? MachineId { get; set; }

        /// <summary>
        /// 重量单位，g/kg
        /// </summary>
        [Display(Name = "WeightUnit"), Description("重量单位，g/kg"), MaxLength(32, ErrorMessage = "重量单位，g/kg 不能超过 32 个字符")]
        public string WeightUnit { get; set; }

        /// <summary>
        /// 单重
        /// </summary>
        [Display(Name = "PieceWeight"), Description("单重"), Column(TypeName = "decimal(20,3)")]
        public decimal? PieceWeight { get; set; }

        /// <summary>
        /// 加工天数
        /// </summary>
        public int? ProcessingDays { get; set; }

        /// <summary>
        /// 调机时间（分钟）
        /// </summary>
        [Display(Name = "SetupTime"), Description("调机时间（分钟）"), Column(TypeName = "decimal(20,2)")]
        public decimal? SetupTime { get; set; }

        /// <summary>
        /// 标准工时，保留两位小数
        /// </summary>
        [Display(Name = "StandardHours"), Description("标准工时，保留两位小数"), Column(TypeName = "decimal(20,2)")]
        public decimal? StandardHours { get; set; }

        /// <summary>
        /// 工时单位，时分秒
        /// </summary>
        [Display(Name = "TimeUnit"), Description("工时单位，时分秒"), MaxLength(32, ErrorMessage = "工时单位，时分秒 不能超过 32 个字符")]
        public string TimeUnit { get; set; }

        /// <summary>
        /// 标准工价
        /// </summary>
        [Display(Name = "StandardWages"), Description("标准工价"), Column(TypeName = "decimal(20,2)")]
        public decimal? StandardWages { get; set; }

        /// <summary>
        /// 检验后转移
        /// </summary>
        public bool? IsTransfer { get; set; }

        /// <summary>
        /// 工艺不良率（%），百分比数据
        /// </summary>
        public int? RejectRate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

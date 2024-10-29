/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcess.cs
*
*功 能： N / A
* 类 名： PsProcess
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:24  SimonHsiao   初版
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
    /// PsProcess (Dto.Base)
    /// </summary>
    public class PsProcessBase
    {

        /// <summary>
        /// 工序编号
        /// </summary>
        [Display(Name = "ProcessNo"), Description("工序编号"), MaxLength(32, ErrorMessage = "工序编号 不能超过 32 个字符")]
        public string ProcessNo { get; set; }

        /// <summary>
        /// 工序名称
        /// </summary>
        [Display(Name = "ProcessName"), Description("工序名称"), MaxLength(32, ErrorMessage = "工序名称 不能超过 32 个字符")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 车间ID
        /// </summary>
        public Guid? WorkShopId { get; set; }

        /// <summary>
        /// 加工类型，自制/外协/质检
        /// </summary>
        [Display(Name = "MachiningType"), Description("加工类型，自制/外协/质检"), MaxLength(32, ErrorMessage = "加工类型，自制/外协/质检 不能超过 32 个字符")]
        public string MachiningType { get; set; }

        /// <summary>
        /// 工序类型，机台加工/非机台加工
        /// </summary>
        [Display(Name = "ProcessType"), Description("工序类型，机台加工/非机台加工"), MaxLength(32, ErrorMessage = "工序类型，机台加工/非机台加工 不能超过 32 个字符")]
        public string ProcessType { get; set; }

        /// <summary>
        /// 外协定价，工时/重量
        /// </summary>
        [Display(Name = "PricingType"), Description("外协定价，工时/重量"), MaxLength(32, ErrorMessage = "外协定价，工时/重量 不能超过 32 个字符")]
        public string PricingType { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

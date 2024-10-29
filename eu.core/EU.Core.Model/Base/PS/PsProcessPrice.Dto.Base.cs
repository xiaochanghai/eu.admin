/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessPrice.cs
*
*功 能： N / A
* 类 名： PsProcessPrice
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:28  SimonHsiao   初版
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
    /// PsProcessPrice (Dto.Base)
    /// </summary>
    public class PsProcessPriceBase
    {

        /// <summary>
        /// 工序ID
        /// </summary>
        public Guid? ProcessId { get; set; }

        /// <summary>
        /// 机台ID
        /// </summary>
        public Guid? MachineId { get; set; }

        /// <summary>
        /// 标准单价
        /// </summary>
        [Display(Name = "Price"), Description("标准单价"), Column(TypeName = "decimal(20,2)")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 时间单位，小时/分钟/秒，从机台资料带出来，如果机台资料为空，可手动选择时分秒
        /// </summary>
        [Display(Name = "TimeUnit"), Description("时间单位，小时/分钟/秒，从机台资料带出来，如果机台资料为空，可手动选择时分秒"), MaxLength(32, ErrorMessage = "时间单位，小时/分钟/秒，从机台资料带出来，如果机台资料为空，可手动选择时分秒 不能超过 32 个字符")]
        public string TimeUnit { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

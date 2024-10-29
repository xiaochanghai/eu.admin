/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessMachine.cs
*
*功 能： N / A
* 类 名： PsProcessMachine
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:27  SimonHsiao   初版
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
    /// PsProcessMachine (Dto.Base)
    /// </summary>
    public class PsProcessMachineBase
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
        /// 机台数量
        /// </summary>
        public int? QTY { get; set; }

        /// <summary>
        /// 优先级，本工序加工优先选择机台，按照123456排序
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// 标准加工时间,从机台资料带出来，如果机台资料为空，可手动输入
        /// </summary>
        [Display(Name = "StandardMachineTime"), Description("标准加工时间,从机台资料带出来，如果机台资料为空，可手动输入"), Column(TypeName = "decimal(20,2)")]
        public decimal? StandardMachineTime { get; set; }

        /// <summary>
        /// 最大加工时间,从机台资料带出来，如果机台资料为空，可手动输入
        /// </summary>
        [Display(Name = "MaxMachineTime"), Description("最大加工时间,从机台资料带出来，如果机台资料为空，可手动输入"), Column(TypeName = "decimal(20,2)")]
        public decimal? MaxMachineTime { get; set; }

        /// <summary>
        /// 时间单位，小时/分钟/秒，从机台资料带出来，如果机台资料为空，可手动选择时分秒
        /// </summary>
        [Display(Name = "TimeUnit"), Description("时间单位，小时/分钟/秒，从机台资料带出来，如果机台资料为空，可手动选择时分秒"), MaxLength(32, ErrorMessage = "时间单位，小时/分钟/秒，从机台资料带出来，如果机台资料为空，可手动选择时分秒 不能超过 32 个字符")]
        public string TimeUnit { get; set; }

        /// <summary>
        /// 加工精度
        /// </summary>
        [Display(Name = "MachineAccuracy"), Description("加工精度"), MaxLength(32, ErrorMessage = "加工精度 不能超过 32 个字符")]
        public string MachineAccuracy { get; set; }

        /// <summary>
        /// 加工说明
        /// </summary>
        [Display(Name = "Explantion"), Description("加工说明"), MaxLength(2000, ErrorMessage = "加工说明 不能超过 2000 个字符")]
        public string Explantion { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmMachine.cs
*
*功 能： N / A
* 类 名： EmMachine
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 11:26:22  SimonHsiao   初版
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
    /// 设备基础资料 (Dto.Base)
    /// </summary>
    public class EmMachineBase
    {

        /// <summary>
        /// 设备编号
        /// </summary>
        [Display(Name = "MachineNo"), Description("设备编号"), MaxLength(32, ErrorMessage = "设备编号 不能超过 32 个字符")]
        public string MachineNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Display(Name = "MachineName"), Description("设备名称"), MaxLength(32, ErrorMessage = "设备名称 不能超过 32 个字符")]
        public string MachineName { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public Guid? MachineType { get; set; }

        /// <summary>
        /// 设备状态,加工中/空闲中/报修中
        /// </summary>
        [Display(Name = "MachineStatus"), Description("设备状态,加工中/空闲中/报修中"), MaxLength(32, ErrorMessage = "设备状态,加工中/空闲中/报修中 不能超过 32 个字符")]
        public string MachineStatus { get; set; }

        /// <summary>
        /// 标准加工时间
        /// </summary>
        [Display(Name = "StandardMachineTime"), Description("标准加工时间"), Column(TypeName = "decimal(20,2)")]
        public decimal? StandardMachineTime { get; set; }

        /// <summary>
        /// 最大加工时间
        /// </summary>
        [Display(Name = "MaxMachineTime"), Description("最大加工时间"), Column(TypeName = "decimal(20,2)")]
        public decimal? MaxMachineTime { get; set; }

        /// <summary>
        /// 时间单位，小时/分钟/秒
        /// </summary>
        [Display(Name = "TimeUnit"), Description("时间单位，小时/分钟/秒"), MaxLength(32, ErrorMessage = "时间单位，小时/分钟/秒 不能超过 32 个字符")]
        public string TimeUnit { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        [Display(Name = "Location"), Description("位置"), MaxLength(128, ErrorMessage = "位置 不能超过 128 个字符")]
        public string Location { get; set; }

        /// <summary>
        /// 责任人
        /// </summary>
        public Guid? ResponsibleId { get; set; }

        /// <summary>
        /// 图片地址
        /// </summary>
        public Guid? FileCode { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

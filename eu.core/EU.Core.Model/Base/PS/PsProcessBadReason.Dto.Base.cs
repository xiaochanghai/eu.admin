/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessBadReason.cs
*
*功 能： N / A
* 类 名： PsProcessBadReason
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:25  SimonHsiao   初版
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
    /// PsProcessBadReason (Dto.Base)
    /// </summary>
    public class PsProcessBadReasonBase
    {

        /// <summary>
        /// 工序单价ID
        /// </summary>
        public Guid? ProcessId { get; set; }

        /// <summary>
        /// 不良原因代码
        /// </summary>
        [Display(Name = "BadCode"), Description("不良原因代码"), MaxLength(32, ErrorMessage = "不良原因代码 不能超过 32 个字符")]
        public string BadCode { get; set; }

        /// <summary>
        /// 不良类型
        /// </summary>
        [Display(Name = "ProcessBadType"), Description("不良类型"), MaxLength(32, ErrorMessage = "不良类型 不能超过 32 个字符")]
        public string ProcessBadType { get; set; }

        /// <summary>
        /// 不良描述
        /// </summary>
        [Display(Name = "BadDesc"), Description("不良描述"), MaxLength(2000, ErrorMessage = "不良描述 不能超过 2000 个字符")]
        public string BadDesc { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

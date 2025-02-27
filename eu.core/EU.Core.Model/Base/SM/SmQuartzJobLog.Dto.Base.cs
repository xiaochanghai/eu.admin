/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmQuartzJobLog.cs
*
* 功 能： N / A
* 类 名： SmQuartzJobLog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:05  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 执行任务日志 (Dto.Base)
/// </summary>
public class SmQuartzJobLogBase : BasePoco
{

    /// <summary>
    /// 任务ID
    /// </summary>
    [Display(Name = "SmQuartzJobId"), Description("任务ID")]
    public Guid? SmQuartzJobId { get; set; }

    /// <summary>
    /// 任务代码
    /// </summary>
    [Display(Name = "JobCode"), Description("任务代码"), MaxLength(32, ErrorMessage = "任务代码 不能超过 32 个字符")]
    public string JobCode { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [Display(Name = "BeginTime"), Description("开始时间")]
    public DateTime? BeginTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [Display(Name = "EndTime"), Description("结束时间")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [Display(Name = "ErrMessage"), Description("错误信息"), MaxLength(2000, ErrorMessage = "错误信息 不能超过 2000 个字符")]
    public string ErrMessage { get; set; }

    /// <summary>
    /// 堆栈跟踪
    /// </summary>
    [Display(Name = "ErrStackTrace"), Description("堆栈跟踪"), MaxLength(2147483647, ErrorMessage = "堆栈跟踪 不能超过 2147483647 个字符")]
    public string ErrStackTrace { get; set; }

    /// <summary>
    /// 执行耗时
    /// </summary>
    [Display(Name = "TotalTime"), Description("执行耗时"), Column(TypeName = "decimal(20,6)")]
    public decimal? TotalTime { get; set; }

    /// <summary>
    /// 执行结果
    /// </summary>
    [Display(Name = "ExecuteResult"), Description("执行结果")]
    public bool? ExecuteResult { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

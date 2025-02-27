/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmQuartzJob.cs
*
* 功 能： N / A
* 类 名： SmQuartzJob
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:16  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 任务调度 (Model)
/// </summary>
[SugarTable("SmQuartzJob", "任务调度"), Entity(TableCnName = "任务调度", TableName = "SmQuartzJob")]
public class SmQuartzJob : BasePoco
{

    /// <summary>
    /// 任务代码
    /// </summary>
    [Display(Name = "JobCode"), Description("任务代码"), MaxLength(32, ErrorMessage = "任务代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string JobCode { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    [Display(Name = "JobName"), Description("任务名称"), MaxLength(32, ErrorMessage = "任务名称 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string JobName { get; set; }

    /// <summary>
    /// 类名
    /// </summary>
    [Display(Name = "ClassName"), Description("类名"), MaxLength(128, ErrorMessage = "类名 不能超过 128 个字符"), SugarColumn(IsNullable = true)]
    public string ClassName { get; set; }

    /// <summary>
    /// 执行规则
    /// </summary>
    [Display(Name = "ScheduleRule"), Description("执行规则"), MaxLength(32, ErrorMessage = "执行规则 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ScheduleRule { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [Display(Name = "Status"), Description("状态"), MaxLength(32, ErrorMessage = "状态 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Status { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    [Display(Name = "LastExecuteTime"), Description("上次执行时间"), SugarColumn(IsNullable = true)]
    public DateTime? LastExecuteTime { get; set; }

    /// <summary>
    /// 上次执行耗时
    /// </summary>
    [Display(Name = "LastCost"), Description("上次执行耗时"), Column(TypeName = "decimal(20,6)"), SugarColumn(IsNullable = true)]
    public decimal? LastCost { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    [Display(Name = "NextExecuteTime"), Description("下次执行时间"), SugarColumn(IsNullable = true)]
    public DateTime? NextExecuteTime { get; set; }

    /// <summary>
    /// 是否更新
    /// </summary>
    [Display(Name = "IsUpdate"), Description("是否更新"), MaxLength(32, ErrorMessage = "是否更新 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string IsUpdate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// LastResult
    /// </summary>
    [Display(Name = "LastResult"), Description("LastResult"), MaxLength(2000, ErrorMessage = "LastResult 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string LastResult { get; set; }
}

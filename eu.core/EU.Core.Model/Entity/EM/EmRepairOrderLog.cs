/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmRepairOrderLog.cs
*
* 功 能： N / A
* 类 名： EmRepairOrderLog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/11/23 19:30:18  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 报修工单日志 (Model)
/// </summary>
[SugarTable("EmRepairOrderLog", "报修工单日志"), Entity(TableCnName = "报修工单日志", TableName = "EmRepairOrderLog")]
public class EmRepairOrderLog : BasePoco
{

    /// <summary>
    /// 工单ID
    /// </summary>
    [Display(Name = "OrderId"), Description("工单ID"), SugarColumn(IsNullable = true)]
    public Guid? OrderId { get; set; }

    /// <summary>
    /// 处理状态
    /// </summary>
    [Display(Name = "DealStatus"), Description("处理状态"), SugarColumn(IsNullable = true, Length = 500)]
    public string DealStatus { get; set; }

    /// <summary>
    /// 处理说明
    /// </summary>
    [Display(Name = "DealDesc"), Description("处理说明"), SugarColumn(IsNullable = true, Length = 500)]
    public string DealDesc { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    [Display(Name = "DealTime"), Description("处理时间")]
    public DateTime? DealTime { get; set; }

    /// <summary>
    /// 指派人ID
    /// </summary>
    [Display(Name = "DealerId"), Description("指派人ID"), SugarColumn(IsNullable = true)]
    public Guid? DealerId { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    [Display(Name = "IsDisplay"), Description("是否显示")]
    public bool? IsDisplay { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 300)]
    public string Remark { get; set; }
}

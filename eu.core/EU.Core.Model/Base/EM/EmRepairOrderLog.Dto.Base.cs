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

namespace EU.Core.Model.Base;

/// <summary>
/// 报修工单日志 (Dto.Base)
/// </summary>
public class EmRepairOrderLogBase : BasePoco
{

    /// <summary>
    /// 工单ID
    /// </summary>
    [Display(Name = "OrderId"), Description("工单ID")]
    public Guid? OrderId { get; set; }

    /// <summary>
    /// 处理状态
    /// </summary>
    [Display(Name = "DealStatus"), Description("处理状态"), MaxLength(500, ErrorMessage = "处理状态 不能超过 500 个字符")]
    public string DealStatus { get; set; }

    /// <summary>
    /// 处理说明
    /// </summary>
    [Display(Name = "DealDesc"), Description("处理说明"), MaxLength(500, ErrorMessage = "处理说明 不能超过 500 个字符")]
    public string DealDesc { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    [Display(Name = "DealTime"), Description("处理时间")]
    public DateTime? DealTime { get; set; }

    /// <summary>
    /// 指派人ID
    /// </summary>
    [Display(Name = "DealerId"), Description("指派人ID")]
    public Guid? DealerId { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    [Display(Name = "IsDisplay"), Description("是否显示")]
    public bool? IsDisplay { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(300, ErrorMessage = "备注 不能超过 300 个字符")]
    public string Remark { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    [Display(Name = "Icon"), Description("图标"), SugarColumn(IsNullable = true, Length = 300)]
    public string Icon { get; set; }

    /// <summary>
    /// 图标颜色
    /// </summary>
    [Display(Name = "IconColor"), Description("图标颜色"), SugarColumn(IsNullable = true, Length = 300)]
    public string IconColor { get; set; }

    /// <summary>
    /// 图标背景颜色
    /// </summary>
    [Display(Name = "BgColor"), Description("图标背景颜色"), SugarColumn(IsNullable = true, Length = 300)]
    public string BgColor { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    [Display(Name = "Title"), Description("标题"), SugarColumn(IsNullable = true, Length = 300)]
    public string Title { get; set; }
}

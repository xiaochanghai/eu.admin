/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmRepairOrder.cs
*
* 功 能： N / A
* 类 名： EmRepairOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/14 22:37:38  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 报修工单 (Dto.Base)
/// </summary>
public class EmRepairOrderBase : BasePoco
{

    /// <summary>
    /// 单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 设备ID
    /// </summary>
    [Display(Name = "EquipmentId"), Description("设备ID")]
    public Guid? EquipmentId { get; set; }

    /// <summary>
    /// 故障类型
    /// </summary>
    [Display(Name = "FaultType"), Description("故障类型"), MaxLength(32, ErrorMessage = "故障类型 不能超过 32 个字符")]
    public string FaultType { get; set; }

    /// <summary>
    /// 紧急程度
    /// </summary>
    [Display(Name = "UrgencyLevel"), Description("紧急程度"), MaxLength(32, ErrorMessage = "紧急程度 不能超过 32 个字符")]
    public string UrgencyLevel { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    [Display(Name = "Priority"), Description("优先级"), MaxLength(32, ErrorMessage = "优先级 不能超过 32 个字符")]
    public string Priority { get; set; }

    /// <summary>
    /// 影响程度
    /// </summary>
    [Display(Name = "Impact"), Description("影响程度"), MaxLength(32, ErrorMessage = "影响程度 不能超过 32 个字符")]
    public string Impact { get; set; }

    /// <summary>
    /// 故障描述
    /// </summary>
    [Display(Name = "FaultDesc"), Description("故障描述"), MaxLength(256, ErrorMessage = "故障描述 不能超过 256 个字符")]
    public string FaultDesc { get; set; }

    /// <summary>
    /// 指派人ID
    /// </summary>
    [Display(Name = "AssignUserId"), Description("指派人ID")]
    public Guid? AssignUserId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [Display(Name = "StartTime"), Description("开始时间")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 预计完成时间
    /// </summary>
    [Display(Name = "ExpectedCompleteTime"), Description("预计完成时间")]
    public DateTime? ExpectedCompleteTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    [Display(Name = "CompleteTime"), Description("完成时间")]
    public DateTime? CompleteTime { get; set; }

    /// <summary>
    /// 维修时长
    /// </summary>
    [Display(Name = "RepairDuration"), Description("维修时长")]
    public int? RepairDuration { get; set; }

    /// <summary>
    /// 停机时长
    /// </summary>
    [Display(Name = "StopDuration"), Description("停机时长")]
    public int? StopDuration { get; set; }

    /// <summary>
    /// 维修状态数据字典
    /// </summary>
    [Display(Name = "RepairStatus"), Description("维修状态数据字典"), MaxLength(36, ErrorMessage = "维修状态数据字典 不能超过 36 个字符")]
    public string RepairStatus { get; set; }

    /// <summary>
    /// 验收时间
    /// </summary>
    [Display(Name = "AcceptTime"), Description("验收时间")]
    public DateTime? AcceptTime { get; set; }

    /// <summary>
    /// 验收人员
    /// </summary>
    [Display(Name = "AcceptUserId"), Description("验收人员")]
    public Guid? AcceptUserId { get; set; }

    /// <summary>
    /// 验收备注
    /// </summary>
    [Display(Name = "AcceptRemark"), Description("验收备注"), MaxLength(2000, ErrorMessage = "验收备注 不能超过 2000 个字符")]
    public string AcceptRemark { get; set; }

    /// <summary>
    /// 驳回原因
    /// </summary>
    [Display(Name = "RejectedReason"), Description("驳回原因"), MaxLength(300, ErrorMessage = "驳回原因 不能超过 300 个字符")]
    public string RejectedReason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(300, ErrorMessage = "备注 不能超过 300 个字符")]
    public string Remark { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    [Display(Name = "Status"), Description("Status"), MaxLength(32, ErrorMessage = "Status 不能超过 32 个字符")]
    public string Status { get; set; }
}

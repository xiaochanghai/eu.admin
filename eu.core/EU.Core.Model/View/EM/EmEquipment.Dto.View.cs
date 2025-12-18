/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmEquipment.cs
*
* 功 能： N / A
* 类 名： EmEquipment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/3 23:26:02  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

using EU.Core.Model.ViewModels;

namespace EU.Core.Model.Models;

/// <summary>
/// 设备基础资料(Dto.View)
/// </summary>
public class EmEquipmentDto : EmEquipment
{
    /// <summary>
    /// 设备图片ID
    /// </summary>
    public List<Guid> ImageIds { get; set; }

    /// <summary>
    /// 设备附件
    /// </summary>
    public List<FileAttachment> Attachments { get; set; }

    public List<EquipmetRepairStats> RepairStats { get; set; } = new();

    public List<FileAttachment> MaintenanceOrder { get; set; }
    public List<FileAttachment> RepairOrder { get; set; }

    /// <summary>
    /// 责任人
    /// </summary>
    public string UseManagerName { get; set; }

    /// <summary>
    /// 启用日期
    /// </summary>
    public string StartDate1 { get; set; }

    /// <summary>
    /// 部门
    /// </summary>
    public string DeptName { get; set; }

    /// <summary>
    /// 运行时长
    /// </summary>
    public string Runtime { get; set; }

    /// <summary>
    /// 健康度
    /// </summary>
    public int Health { get; set; } = 100;

    /// <summary>
    /// 状态
    /// </summary>
    public string StatusText { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public string MachineType { get; set; }

}

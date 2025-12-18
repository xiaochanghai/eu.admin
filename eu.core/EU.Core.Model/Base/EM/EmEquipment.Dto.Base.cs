/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmEquipment.cs
*
* 功 能： N / A
* 类 名： EmEquipment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/11 20:16:49  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 设备基础资料 (Dto.Base)
/// </summary>
public class EmEquipmentBase : BasePoco
{

    /// <summary>
    /// 设备类型
    /// </summary>
    [Display(Name = "MachineTypeId"), Description("设备类型")]
    public Guid? MachineTypeId { get; set; }

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
    /// 设备状态,加工中/空闲中/报修中
    /// </summary>
    [Display(Name = "Status"), Description("设备状态,加工中/空闲中/报修中"), MaxLength(32, ErrorMessage = "设备状态,加工中/空闲中/报修中 不能超过 32 个字符")]
    public string Status { get; set; }

    /// <summary>
    /// 使用部门
    /// </summary>
    [Display(Name = "UseDeptId"), Description("使用部门")]
    public Guid? UseDeptId { get; set; }

    /// <summary>
    /// 使用负责人
    /// </summary>
    [Display(Name = "UseManagerId"), Description("使用负责人")]
    public Guid? UseManagerId { get; set; }

    /// <summary>
    /// 维修负责人
    /// </summary>
    [Display(Name = "RepairManagerId"), Description("维修负责人")]
    public Guid? RepairManagerId { get; set; }

    /// <summary>
    /// 品牌型号
    /// </summary>
    [Display(Name = "BrandModel"), Description("品牌型号"), MaxLength(32, ErrorMessage = "品牌型号 不能超过 32 个字符")]
    public string BrandModel { get; set; }

    /// <summary>
    /// 生产厂家
    /// </summary>
    [Display(Name = "Manufacturer"), Description("生产厂家"), MaxLength(64, ErrorMessage = "生产厂家 不能超过 64 个字符")]
    public string Manufacturer { get; set; }

    /// <summary>
    /// 供应商家
    /// </summary>
    [Display(Name = "Supplier"), Description("供应商家"), MaxLength(64, ErrorMessage = "供应商家 不能超过 64 个字符")]
    public string Supplier { get; set; }

    /// <summary>
    /// 设备位置
    /// </summary>
    [Display(Name = "Location"), Description("设备位置"), MaxLength(64, ErrorMessage = "设备位置 不能超过 64 个字符")]
    public string Location { get; set; }

    /// <summary>
    /// 年检，是/否
    /// </summary>
    [Display(Name = "AnnualInspection"), Description("年检，是/否")]
    public bool? AnnualInspection { get; set; }

    /// <summary>
    /// 年检日期
    /// </summary>
    [Display(Name = "AnnualInspectionDate"), Description("年检日期")]
    public DateTime? AnnualInspectionDate { get; set; }

    /// <summary>
    /// 投入日期
    /// </summary>
    [Display(Name = "CommissioningDate"), Description("投入日期")]
    public DateTime? CommissioningDate { get; set; }

    /// <summary>
    /// 停用日期
    /// </summary>
    [Display(Name = "StopDate"), Description("停用日期")]
    public DateTime? StopDate { get; set; }

    /// <summary>
    /// 图片ID
    /// </summary>
    [Display(Name = "ImageId"), Description("图片ID")]
    public Guid? ImageId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

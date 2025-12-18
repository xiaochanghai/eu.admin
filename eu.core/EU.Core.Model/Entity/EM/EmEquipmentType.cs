/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmEquipmentType.cs
*
* 功 能： N / A
* 类 名： EmEquipmentType
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/11/20 22:58:25  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 设备分类 (Model)
/// </summary>
[SugarTable("EmEquipmentType", "设备分类"), Entity(TableCnName = "设备分类", TableName = "EmEquipmentType")]
public class EmEquipmentType : BasePoco
{

    /// <summary>
    /// 分类编号
    /// </summary>
    [Display(Name = "TypeNo"), Description("分类编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string TypeNo { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [Display(Name = "TypeName"), Description("分类名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string TypeName { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [Display(Name = "DepartmentId"), Description("部门ID"), SugarColumn(IsNullable = true)]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialType.cs
*
* 功 能： N / A
* 类 名： BdMaterialType
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:29:45  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 物料类型 (Model)
/// </summary>
[SugarTable("BdMaterialType", "物料类型"), Entity(TableCnName = "物料类型", TableName = "BdMaterialType")]
public class BdMaterialType : BasePoco
{

    /// <summary>
    /// 类型编号
    /// </summary>
    [Display(Name = "MaterialTypeNo"), Description("类型编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string MaterialTypeNo { get; set; }

    /// <summary>
    /// 类型名称
    /// </summary>
    [Display(Name = "MaterialTypeNames"), Description("类型名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string MaterialTypeNames { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// 上级ID
    /// </summary>
    [Display(Name = "ParentTypeId"), Description("上级ID"), SugarColumn(IsNullable = true)]
    public Guid? ParentTypeId { get; set; }
}

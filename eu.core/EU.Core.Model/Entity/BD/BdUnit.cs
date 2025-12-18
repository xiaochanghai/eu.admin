/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdUnit.cs
*
* 功 能： N / A
* 类 名： BdUnit
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/18 17:16:08  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 计量单位 (Model)
/// </summary>
[SugarTable("BdUnit", "计量单位"), Entity(TableCnName = "计量单位", TableName = "BdUnit")]
public class BdUnit : BasePoco
{

    /// <summary>
    /// 类型编号
    /// </summary>
    [Display(Name = "UnitNo"), Description("类型编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string UnitNo { get; set; }

    /// <summary>
    /// 类型名称
    /// </summary>
    [Display(Name = "UnitNames"), Description("类型名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string UnitNames { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "DecimalPlaces"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

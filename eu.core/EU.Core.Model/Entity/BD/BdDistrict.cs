/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdDistrict.cs
*
* 功 能： N / A
* 类 名： BdDistrict
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/18 22:19:32  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 地区建立 (Model)
/// </summary>
[SugarTable("BdDistrict", "地区建立"), Entity(TableCnName = "地区建立", TableName = "BdDistrict")]
public class BdDistrict : BasePoco
{

    /// <summary>
    /// 地区编号
    /// </summary>
    [Display(Name = "DistrictNo"), Description("地区编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string DistrictNo { get; set; }

    /// <summary>
    /// 地区名称
    /// </summary>
    [Display(Name = "DistrictName"), Description("地区名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string DistrictName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

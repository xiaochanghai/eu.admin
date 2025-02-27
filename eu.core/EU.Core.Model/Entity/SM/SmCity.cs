/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCity.cs
*
* 功 能： N / A
* 类 名： SmCity
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:34  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 城市 (Model)
/// </summary>
[SugarTable("SmCity", "城市"), Entity(TableCnName = "城市", TableName = "SmCity")]
public class SmCity : BasePoco
{

    /// <summary>
    /// 省份ID
    /// </summary>
    [Display(Name = "ProvinceId"), Description("省份ID"), SugarColumn(IsNullable = true)]
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// 城市代码
    /// </summary>
    [Display(Name = "CityCode"), Description("城市代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string CityCode { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    [Display(Name = "CityNameZh"), Description("城市"), SugarColumn(IsNullable = true, Length = 32)]
    public string CityNameZh { get; set; }

    /// <summary>
    /// 城市(英文)
    /// </summary>
    [Display(Name = "CityNameEn"), Description("城市(英文)"), SugarColumn(IsNullable = true, Length = 32)]
    public string CityNameEn { get; set; }

    /// <summary>
    /// 城市编号
    /// </summary>
    [Display(Name = "CityNo"), Description("城市编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string CityNo { get; set; }

    /// <summary>
    /// 城市邮编
    /// </summary>
    [Display(Name = "CityZipCode"), Description("城市邮编"), SugarColumn(IsNullable = true, Length = 32)]
    public string CityZipCode { get; set; }

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
}

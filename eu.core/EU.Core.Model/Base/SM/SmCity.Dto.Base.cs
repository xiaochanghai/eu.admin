/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCity.cs
*
* 功 能： N / A
* 类 名： SmCity
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:25:49  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 城市 (Dto.Base)
/// </summary>
public class SmCityBase : BasePoco
{

    /// <summary>
    /// 省份ID
    /// </summary>
    [Display(Name = "ProvinceId"), Description("省份ID"), SugarColumn(IsNullable = true)]
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// 城市代码
    /// </summary>
    [Display(Name = "CityCode"), Description("城市代码"), MaxLength(32, ErrorMessage = "城市代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CityCode { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    [Display(Name = "CityNameZh"), Description("城市"), MaxLength(32, ErrorMessage = "城市 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CityNameZh { get; set; }

    /// <summary>
    /// 城市(英文)
    /// </summary>
    [Display(Name = "CityNameEn"), Description("城市(英文)"), MaxLength(32, ErrorMessage = "城市(英文) 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CityNameEn { get; set; }

    /// <summary>
    /// 城市编号
    /// </summary>
    [Display(Name = "CityNo"), Description("城市编号"), MaxLength(32, ErrorMessage = "城市编号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CityNo { get; set; }

    /// <summary>
    /// 城市邮编
    /// </summary>
    [Display(Name = "CityZipCode"), Description("城市邮编"), MaxLength(32, ErrorMessage = "城市邮编 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CityZipCode { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

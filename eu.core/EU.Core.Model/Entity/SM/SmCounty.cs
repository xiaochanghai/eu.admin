/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCounty.cs
*
* 功 能： N / A
* 类 名： SmCounty
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:42  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 区县 (Model)
/// </summary>
[SugarTable("SmCounty", "区县"), Entity(TableCnName = "区县", TableName = "SmCounty")]
public class SmCounty : BasePoco
{

    /// <summary>
    /// 省份ID
    /// </summary>
    [Display(Name = "ProvinceId"), Description("省份ID"), SugarColumn(IsNullable = true)]
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// 城市ID
    /// </summary>
    [Display(Name = "CityId"), Description("城市ID"), SugarColumn(IsNullable = true)]
    public Guid? CityId { get; set; }

    /// <summary>
    /// 区县代码
    /// </summary>
    [Display(Name = "CountyCode"), Description("区县代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string CountyCode { get; set; }

    /// <summary>
    /// 区县
    /// </summary>
    [Display(Name = "CountyNameZh"), Description("区县"), SugarColumn(IsNullable = true, Length = 32)]
    public string CountyNameZh { get; set; }

    /// <summary>
    /// 区县(英文)
    /// </summary>
    [Display(Name = "CountyNameEn"), Description("区县(英文)"), SugarColumn(IsNullable = true, Length = 32)]
    public string CountyNameEn { get; set; }

    /// <summary>
    /// 区县编号
    /// </summary>
    [Display(Name = "CountyNo"), Description("区县编号"), SugarColumn(IsNullable = true, Length = 32)]
    public string CountyNo { get; set; }

    /// <summary>
    /// 区县邮编
    /// </summary>
    [Display(Name = "CountyZipCode"), Description("区县邮编"), SugarColumn(IsNullable = true, Length = 32)]
    public string CountyZipCode { get; set; }

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

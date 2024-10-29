/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCounty.cs
*
*功 能： N / A
* 类 名： SmCounty
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/24 17:31:34  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 区县 (Model)
/// </summary>
[SugarTable("SmCounty", "SmCounty"), Entity(TableCnName = "区县", TableName = "SmCounty")]
public class SmCounty : BasePoco
{

    /// <summary>
    /// 省份ID
    /// </summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// 城市ID
    /// </summary>
    public Guid? CityId { get; set; }

    /// <summary>
    /// 区县代码
    /// </summary>
    [Display(Name = "CountyCode"), Description("区县代码"), MaxLength(32, ErrorMessage = "区县代码 不能超过 32 个字符")]
    public string CountyCode { get; set; }

    /// <summary>
    /// 区县
    /// </summary>
    [Display(Name = "CountyNameZh"), Description("区县"), MaxLength(32, ErrorMessage = "区县 不能超过 32 个字符")]
    public string CountyNameZh { get; set; }

    /// <summary>
    /// 区县(英文)
    /// </summary>
    [Display(Name = "CountyNameEn"), Description("区县(英文)"), MaxLength(32, ErrorMessage = "区县(英文) 不能超过 32 个字符")]
    public string CountyNameEn { get; set; }

    /// <summary>
    /// 区县编号
    /// </summary>
    [Display(Name = "CountyNo"), Description("区县编号"), MaxLength(32, ErrorMessage = "区县编号 不能超过 32 个字符")]
    public string CountyNo { get; set; }

    /// <summary>
    /// 区县邮编
    /// </summary>
    [Display(Name = "CountyZipCode"), Description("区县邮编"), MaxLength(32, ErrorMessage = "区县邮编 不能超过 32 个字符")]
    public string CountyZipCode { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

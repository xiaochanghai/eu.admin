﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
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

namespace EU.Core.Model.Base;

/// <summary>
/// 区县 (Dto.Base)
/// </summary>
public class SmCountyBase : BasePoco
{

    /// <summary>
    /// 省份ID
    /// </summary>
    [Display(Name = "ProvinceId"), Description("省份ID")]
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// 城市ID
    /// </summary>
    [Display(Name = "CityId"), Description("城市ID")]
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
    [Display(Name = "TaxisNo"), Description("排序号")]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

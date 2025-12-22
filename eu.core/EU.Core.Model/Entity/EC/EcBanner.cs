/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EcBanner.cs
*
* 功 能： N / A
* 类 名： EcBanner
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/22 22:22:29  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// EcBanner (Model)
/// </summary>
[SugarTable("EcBanner", "EcBanner"), Entity(TableCnName = "EcBanner", TableName = "EcBanner")]
public class EcBanner : BasePoco
{

    /// <summary>
    /// 代码
    /// </summary>
    [Display(Name = "BannerCode"), Description("代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string BannerCode { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [Display(Name = "BannerName"), Description("名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string BannerName { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    [Display(Name = "ImageUrl"), Description("图片URL"), SugarColumn(IsNullable = true, Length = 64)]
    public string ImageUrl { get; set; }

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

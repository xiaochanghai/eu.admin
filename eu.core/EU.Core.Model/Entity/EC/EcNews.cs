/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EcNews.cs
*
* 功 能： N / A
* 类 名： EcNews
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/22 22:22:59  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 新闻 (Model)
/// </summary>
[SugarTable("EcNews", "新闻"), Entity(TableCnName = "新闻", TableName = "EcNews")]
public class EcNews : BasePoco
{

    /// <summary>
    /// 标题
    /// </summary>
    [Display(Name = "Title"), Description("标题"), SugarColumn(IsNullable = true, Length = 256)]
    public string Title { get; set; }

    /// <summary>
    /// 简介
    /// </summary>
    [Display(Name = "Summary"), Description("简介"), SugarColumn(IsNullable = true, Length = 256)]
    public string Summary { get; set; }

    /// <summary>
    /// 发布日期
    /// </summary>
    [Display(Name = "PublishDate"), Description("发布日期"), SugarColumn(IsNullable = true)]
    public DateTime? PublishDate { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    [Display(Name = "Author"), Description("作者"), SugarColumn(IsNullable = true, Length = 32)]
    public string Author { get; set; }

    /// <summary>
    /// 新闻内容
    /// </summary>
    [Display(Name = "NewsContent"), Description("新闻内容"), SugarColumn(IsNullable = true, Length = 2147483647)]
    public string NewsContent { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    [Display(Name = "NewsType"), Description("类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string NewsType { get; set; }

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

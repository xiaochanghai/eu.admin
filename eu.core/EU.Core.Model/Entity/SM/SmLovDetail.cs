/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLovDetail.cs
*
* 功 能： N / A
* 类 名： SmLovDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:56  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 字典明细 (Model)
/// </summary>
[SugarTable("SmLovDetail", "字典明细"), Entity(TableCnName = "字典明细", TableName = "SmLovDetail")]
public class SmLovDetail : BasePoco
{

    /// <summary>
    /// 字典ID
    /// </summary>
    [Display(Name = "SmLovId"), Description("字典ID"), SugarColumn(IsNullable = true)]
    public Guid? SmLovId { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 参数值
    /// </summary>
    [Display(Name = "Value"), Description("参数值"), SugarColumn(IsNullable = true, Length = 32)]
    public string Value { get; set; }

    /// <summary>
    /// 参数
    /// </summary>
    [Display(Name = "Text"), Description("参数"), SugarColumn(IsNullable = true, Length = 32)]
    public string Text { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    [Display(Name = "InureTime"), Description("生效时间"), SugarColumn(IsNullable = true)]
    public DateTime? InureTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    [Display(Name = "AbateTime"), Description("失效时间"), SugarColumn(IsNullable = true)]
    public DateTime? AbateTime { get; set; }

    /// <summary>
    /// 标签颜色
    /// </summary>
    [Display(Name = "TagColor"), Description("标签颜色"), SugarColumn(IsNullable = true, Length = 32)]
    public string TagColor { get; set; }

    /// <summary>
    /// 标签是否有边框
    /// </summary>
    [Display(Name = "TagBordered"), Description("标签是否有边框"), SugarColumn(IsNullable = true)]
    public bool? TagBordered { get; set; }

    /// <summary>
    /// 标签图标
    /// </summary>
    [Display(Name = "TagIcon"), Description("标签图标"), SugarColumn(IsNullable = true, Length = 32)]
    public string TagIcon { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

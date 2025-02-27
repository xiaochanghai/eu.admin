/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmAutoCode.cs
*
* 功 能： N / A
* 类 名： SmAutoCode
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:33  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 自动编号 (Model)
/// </summary>
[SugarTable("SmAutoCode", "自动编号"), Entity(TableCnName = "自动编号", TableName = "SmAutoCode")]
public class SmAutoCode : BasePoco
{

    /// <summary>
    /// 编号代码
    /// </summary>
    [Display(Name = "NumberCode"), Description("编号代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string NumberCode { get; set; }

    /// <summary>
    /// 前缀
    /// </summary>
    [Display(Name = "Prefix"), Description("前缀"), SugarColumn(IsNullable = true, Length = 32)]
    public string Prefix { get; set; }

    /// <summary>
    /// 时间类型
    /// </summary>
    [Display(Name = "DateFormatType"), Description("时间类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string DateFormatType { get; set; }

    /// <summary>
    /// 编号长度
    /// </summary>
    [Display(Name = "NumberLength"), Description("编号长度"), SugarColumn(IsNullable = true)]
    public int? NumberLength { get; set; }

    /// <summary>
    /// 关联表
    /// </summary>
    [Display(Name = "TableName"), Description("关联表"), SugarColumn(IsNullable = true, Length = 32)]
    public string TableName { get; set; }

    /// <summary>
    /// 关联栏位名
    /// </summary>
    [Display(Name = "ColumnName"), Description("关联栏位名"), SugarColumn(IsNullable = true, Length = 32)]
    public string ColumnName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

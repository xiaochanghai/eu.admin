/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmAutoCode.cs
*
* 功 能： N / A
* 类 名： SmAutoCode
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:43:07  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
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
    [Display(Name = "NumberCode"), Description("编号代码"), MaxLength(32, ErrorMessage = "编号代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string NumberCode { get; set; }

    /// <summary>
    /// 前缀
    /// </summary>
    [Display(Name = "Prefix"), Description("前缀"), MaxLength(32, ErrorMessage = "前缀 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Prefix { get; set; }

    /// <summary>
    /// 时间类型
    /// </summary>
    [Display(Name = "DateFormatType"), Description("时间类型"), MaxLength(32, ErrorMessage = "时间类型 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DateFormatType { get; set; }

    /// <summary>
    /// 编号长度
    /// </summary>
    [Display(Name = "NumberLength"), Description("编号长度"), SugarColumn(IsNullable = true)]
    public int? NumberLength { get; set; }

    /// <summary>
    /// 关联表
    /// </summary>
    [Display(Name = "TableName"), Description("关联表"), MaxLength(32, ErrorMessage = "关联表 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string TableName { get; set; }

    /// <summary>
    /// 关联栏位名
    /// </summary>
    [Display(Name = "ColumnName"), Description("关联栏位名"), MaxLength(32, ErrorMessage = "关联栏位名 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ColumnName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}

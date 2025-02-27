/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImpTemplate.cs
*
* 功 能： N / A
* 类 名： SmImpTemplate
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:52  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 系统导入模板 (Model)
/// </summary>
[SugarTable("SmImpTemplate", "系统导入模板"), Entity(TableCnName = "系统导入模板", TableName = "SmImpTemplate")]
public class SmImpTemplate : BasePoco
{

    /// <summary>
    /// 模板代码
    /// </summary>
    [Display(Name = "TemplateCode"), Description("模板代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string TemplateCode { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [Display(Name = "TemplateName"), Description("模板名称"), SugarColumn(IsNullable = true, Length = 32)]
    public string TemplateName { get; set; }

    /// <summary>
    /// 表代码
    /// </summary>
    [Display(Name = "TableCode"), Description("表代码"), SugarColumn(IsNullable = true, Length = 32)]
    public string TableCode { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "ModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? ModuleId { get; set; }

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), SugarColumn(IsNullable = true, Length = 32)]
    public string SheetName { get; set; }

    /// <summary>
    /// 数据起始行
    /// </summary>
    [Display(Name = "StartRow"), Description("数据起始行"), SugarColumn(IsNullable = true)]
    public int? StartRow { get; set; }

    /// <summary>
    /// 标签名
    /// </summary>
    [Display(Name = "Label"), Description("标签名"), SugarColumn(IsNullable = true, Length = 32)]
    public string Label { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 转验证完全正确，允许存在错误
    /// </summary>
    [Display(Name = "TransferMode"), Description("转验证完全正确，允许存在错误"), SugarColumn(IsNullable = true, Length = 32)]
    public string TransferMode { get; set; }

    /// <summary>
    /// 显示进度条
    /// </summary>
    [Display(Name = "IsDisplayProgress"), Description("显示进度条"), SugarColumn(IsNullable = true)]
    public bool? IsDisplayProgress { get; set; }

    /// <summary>
    /// 加载数据
    /// </summary>
    [Display(Name = "IsLoadData"), Description("加载数据"), SugarColumn(IsNullable = true)]
    public bool? IsLoadData { get; set; }

    /// <summary>
    /// 显示
    /// </summary>
    [Display(Name = "IsDisplay"), Description("显示"), SugarColumn(IsNullable = true)]
    public bool? IsDisplay { get; set; }

    /// <summary>
    /// 是否允许覆盖导入
    /// </summary>
    [Display(Name = "IsAllowOverride"), Description("是否允许覆盖导入"), SugarColumn(IsNullable = true)]
    public bool? IsAllowOverride { get; set; }

    /// <summary>
    /// 排除最后行数
    /// </summary>
    [Display(Name = "ExcludeLastRow"), Description("排除最后行数"), SugarColumn(IsNullable = true)]
    public int? ExcludeLastRow { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

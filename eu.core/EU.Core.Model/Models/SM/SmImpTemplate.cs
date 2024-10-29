/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImpTemplate.cs
*
*功 能： N / A
* 类 名： SmImpTemplate
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/22 9:39:17  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 系统导入模板 (Model)
/// </summary>
[SugarTable("SmImpTemplate", "SmImpTemplate"), Entity(TableCnName = "系统导入模板", TableName = "SmImpTemplate")]
public class SmImpTemplate : BasePoco
{

    /// <summary>
    /// 模板代码
    /// </summary>
    [Display(Name = "TemplateCode"), Description("模板代码"), MaxLength(32, ErrorMessage = "模板代码 不能超过 32 个字符")]
    public string TemplateCode { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [Display(Name = "TemplateName"), Description("模板名称"), MaxLength(32, ErrorMessage = "模板名称 不能超过 32 个字符")]
    public string TemplateName { get; set; }

    /// <summary>
    /// 表代码
    /// </summary>
    [Display(Name = "TableCode"), Description("表代码"), MaxLength(32, ErrorMessage = "表代码 不能超过 32 个字符")]
    public string TableCode { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public Guid? ModuleId { get; set; }

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), MaxLength(32, ErrorMessage = "Sheet名 不能超过 32 个字符")]
    public string SheetName { get; set; }

    /// <summary>
    /// 数据起始行
    /// </summary>
    public int? StartRow { get; set; }

    /// <summary>
    /// 标签名
    /// </summary>
    [Display(Name = "Label"), Description("标签名"), MaxLength(32, ErrorMessage = "标签名 不能超过 32 个字符")]
    public string Label { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 转验证完全正确，允许存在错误
    /// </summary>
    [Display(Name = "TransferMode"), Description("转验证完全正确，允许存在错误"), MaxLength(32, ErrorMessage = "转验证完全正确，允许存在错误 不能超过 32 个字符")]
    public string TransferMode { get; set; }

    /// <summary>
    /// 显示进度条
    /// </summary>
    public bool? IsDisplayProgress { get; set; }

    /// <summary>
    /// 加载数据
    /// </summary>
    public bool? IsLoadData { get; set; }

    /// <summary>
    /// 显示
    /// </summary>
    public bool? IsDisplay { get; set; }

    /// <summary>
    /// 是否允许覆盖导入
    /// </summary>
    public bool? IsAllowOverride { get; set; }

    /// <summary>
    /// 排除最后行数
    /// </summary>
    public int? ExcludeLastRow { get; set; }
}

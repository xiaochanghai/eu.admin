/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleColumn.cs
*
* 功 能： N / A
* 类 名： SmModuleColumn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:10  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 模块列 (Model)
/// </summary>
[SugarTable("SmModuleColumn", "模块列"), Entity(TableCnName = "模块列", TableName = "SmModuleColumn")]
public class SmModuleColumn : BasePoco
{

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "SmModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 列名称
    /// </summary>
    [Display(Name = "Title"), Description("列名称"), MaxLength(32, ErrorMessage = "列名称 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Title { get; set; }

    /// <summary>
    /// 栏位名
    /// </summary>
    [Display(Name = "DataIndex"), Description("栏位名"), MaxLength(32, ErrorMessage = "栏位名 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DataIndex { get; set; }

    /// <summary>
    /// 列表数据类型
    /// </summary>
    [Display(Name = "ValueType"), Description("列表数据类型"), MaxLength(32, ErrorMessage = "列表数据类型 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ValueType { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    [Display(Name = "Width"), Description("宽度"), Column(TypeName = "decimal(20,2)"), SugarColumn(IsNullable = true)]
    public decimal? Width { get; set; }

    /// <summary>
    /// 列表中隐藏
    /// </summary>
    [Display(Name = "HideInTable"), Description("列表中隐藏"), SugarColumn(IsNullable = true)]
    public bool? HideInTable { get; set; }

    /// <summary>
    /// 是否排序
    /// </summary>
    [Display(Name = "Sorter"), Description("是否排序"), SugarColumn(IsNullable = true)]
    public bool? Sorter { get; set; }

    /// <summary>
    /// filters
    /// </summary>
    [Display(Name = "filters"), Description("filters"), SugarColumn(IsNullable = true)]
    public bool? filters { get; set; }

    /// <summary>
    /// filterMultiple
    /// </summary>
    [Display(Name = "filterMultiple"), Description("filterMultiple"), SugarColumn(IsNullable = true)]
    public bool? filterMultiple { get; set; }

    /// <summary>
    /// 是否导出Excel
    /// </summary>
    [Display(Name = "IsExport"), Description("是否导出Excel"), SugarColumn(IsNullable = true)]
    public bool? IsExport { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 是否参数
    /// </summary>
    [Display(Name = "IsLovCode"), Description("是否参数"), SugarColumn(IsNullable = true)]
    public bool? IsLovCode { get; set; }

    /// <summary>
    /// 是否bool
    /// </summary>
    [Display(Name = "IsBool"), Description("是否bool"), SugarColumn(IsNullable = true)]
    public bool? IsBool { get; set; }

    /// <summary>
    /// QueryValue
    /// </summary>
    [Display(Name = "QueryValue"), Description("QueryValue"), MaxLength(2000, ErrorMessage = "QueryValue 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string QueryValue { get; set; }

    /// <summary>
    /// QueryValueType
    /// </summary>
    [Display(Name = "QueryValueType"), Description("QueryValueType"), MaxLength(2000, ErrorMessage = "QueryValueType 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string QueryValueType { get; set; }

    /// <summary>
    /// 查询中隐藏
    /// </summary>
    [Display(Name = "HideInSearch"), Description("查询中隐藏"), SugarColumn(IsNullable = true)]
    public bool? HideInSearch { get; set; }

    /// <summary>
    /// 数据格式
    /// </summary>
    [Display(Name = "DataFormate"), Description("数据格式"), MaxLength(32, ErrorMessage = "数据格式 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DataFormate { get; set; }

    /// <summary>
    /// 对齐方式
    /// </summary>
    [Display(Name = "Align"), Description("对齐方式"), MaxLength(32, ErrorMessage = "对齐方式 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Align { get; set; }

    /// <summary>
    /// 表別名
    /// </summary>
    [Display(Name = "TableAlias"), Description("表別名"), MaxLength(32, ErrorMessage = "表別名 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string TableAlias { get; set; }

    /// <summary>
    /// 是否合计
    /// </summary>
    [Display(Name = "IsSum"), Description("是否合计"), SugarColumn(IsNullable = true)]
    public bool? IsSum { get; set; }

    /// <summary>
    /// 表单排序号
    /// </summary>
    [Display(Name = "FromTaxisNo"), Description("表单排序号"), SugarColumn(IsNullable = true)]
    public int? FromTaxisNo { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    [Display(Name = "DefaultValue"), Description("默认值"), MaxLength(32, ErrorMessage = "默认值 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DefaultValue { get; set; }

    /// <summary>
    /// 表单隐藏
    /// </summary>
    [Display(Name = "HideInForm"), Description("表单隐藏"), SugarColumn(IsNullable = true)]
    public bool? HideInForm { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [Display(Name = "Required"), Description("是否必填"), SugarColumn(IsNullable = true)]
    public bool? Required { get; set; }

    /// <summary>
    /// 只读
    /// </summary>
    [Display(Name = "Disabled"), Description("只读"), SugarColumn(IsNullable = true)]
    public bool? Disabled { get; set; }

    /// <summary>
    /// 限定输入格式
    /// </summary>
    [Display(Name = "Validator"), Description("限定输入格式"), MaxLength(32, ErrorMessage = "限定输入格式 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Validator { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    [Display(Name = "ValidPattern"), Description("正则表达式"), MaxLength(32, ErrorMessage = "正则表达式 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ValidPattern { get; set; }

    /// <summary>
    /// 是否唯一
    /// </summary>
    [Display(Name = "IsUnique"), Description("是否唯一"), SugarColumn(IsNullable = true)]
    public bool? IsUnique { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    [Display(Name = "MaxLength"), Description("最大长度"), SugarColumn(IsNullable = true)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// 最小长度
    /// </summary>
    [Display(Name = "MinLength"), Description("最小长度"), SugarColumn(IsNullable = true)]
    public int? MinLength { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    [Display(Name = "Maximum"), Description("最大值"), Column(TypeName = "decimal(20,4)"), SugarColumn(IsNullable = true)]
    public decimal? Maximum { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    [Display(Name = "Minimum"), Description("最小值"), Column(TypeName = "decimal(20,4)"), SugarColumn(IsNullable = true)]
    public decimal? Minimum { get; set; }

    /// <summary>
    /// 新增时隐藏
    /// </summary>
    [Display(Name = "CreateHide"), Description("新增时隐藏"), SugarColumn(IsNullable = true)]
    public bool? CreateHide { get; set; }

    /// <summary>
    /// 修改时只读
    /// </summary>
    [Display(Name = "ModifyDisabled"), Description("修改时只读"), SugarColumn(IsNullable = true)]
    public bool? ModifyDisabled { get; set; }

    /// <summary>
    /// 字段占比
    /// </summary>
    [Display(Name = "GridSpan"), Description("字段占比"), SugarColumn(IsNullable = true)]
    public int? GridSpan { get; set; }

    /// <summary>
    /// 表单项标题
    /// </summary>
    [Display(Name = "FormTitle"), Description("表单项标题"), MaxLength(32, ErrorMessage = "表单项标题 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string FormTitle { get; set; }

    /// <summary>
    /// 字段控件类型
    /// </summary>
    [Display(Name = "FieldType"), Description("字段控件类型"), MaxLength(32, ErrorMessage = "字段控件类型 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string FieldType { get; set; }

    /// <summary>
    /// 占位符
    /// </summary>
    [Display(Name = "Placeholder"), Description("占位符"), MaxLength(32, ErrorMessage = "占位符 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Placeholder { get; set; }

    /// <summary>
    /// 数据来源方式
    /// </summary>
    [Display(Name = "DataSourceType"), Description("数据来源方式"), MaxLength(32, ErrorMessage = "数据来源方式 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DataSourceType { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    [Display(Name = "DataSource"), Description("数据来源"), MaxLength(32, ErrorMessage = "数据来源 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DataSource { get; set; }

    /// <summary>
    /// 是否为Master栏位
    /// </summary>
    [Display(Name = "IsMasterId"), Description("是否为Master栏位"), SugarColumn(IsNullable = true)]
    public bool? IsMasterId { get; set; }

    /// <summary>
    /// 标签布局
    /// </summary>
    [Display(Name = "LabelCol"), Description("标签布局"), SugarColumn(IsNullable = true)]
    public int? LabelCol { get; set; }

    /// <summary>
    /// 控件布局
    /// </summary>
    [Display(Name = "WrapperCol"), Description("控件布局"), SugarColumn(IsNullable = true)]
    public int? WrapperCol { get; set; }

    /// <summary>
    /// TextArea最小行数
    /// </summary>
    [Display(Name = "MinRows"), Description("TextArea最小行数"), SugarColumn(IsNullable = true)]
    public int? MinRows { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 表单字段组别
    /// </summary>
    [Display(Name = "FromFieldGroup"), Description("表单字段组别"), SugarColumn(IsNullable = true)]
    public int? FromFieldGroup { get; set; }

    /// <summary>
    /// 是否表格编辑
    /// </summary>
    [Display(Name = "IsTableEditable"), Description("是否表格编辑"), SugarColumn(IsNullable = true)]
    public bool? IsTableEditable { get; set; }

    /// <summary>
    /// 是否自动编号
    /// </summary>
    [Display(Name = "IsAutoCode"), Description("是否自动编号"), SugarColumn(IsNullable = true)]
    public bool? IsAutoCode { get; set; }

    /// <summary>
    /// 栏位模式,通用/列表/表单
    /// </summary>
    [Display(Name = "ColumnMode"), Description("栏位模式,通用/列表/表单"), MaxLength(32, ErrorMessage = "栏位模式,通用/列表/表单 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ColumnMode { get; set; }

    /// <summary>
    /// 是否复制
    /// </summary>
    [Display(Name = "IsCopy"), Description("是否复制"), SugarColumn(IsNullable = true)]
    public bool? IsCopy { get; set; }

    /// <summary>
    /// 是否提示
    /// </summary>
    [Display(Name = "IsTooltip"), Description("是否提示"), SugarColumn(IsNullable = true)]
    public bool? IsTooltip { get; set; }

    /// <summary>
    /// 提示内容
    /// </summary>
    [Display(Name = "TooltipContent"), Description("提示内容"), MaxLength(64, ErrorMessage = "提示内容 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string TooltipContent { get; set; }

    /// <summary>
    /// 字段颜色
    /// </summary>
    [Display(Name = "Color"), Description("字段颜色"), MaxLength(32, ErrorMessage = "字段颜色 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Color { get; set; }

    /// <summary>
    /// 跟随主题颜色
    /// </summary>
    [Display(Name = "IsThemeColor"), Description("跟随主题颜色"), SugarColumn(IsNullable = true)]
    public bool? IsThemeColor { get; set; }

    /// <summary>
    /// 清空内容
    /// </summary>
    [Display(Name = "AllowClear"), Description("清空内容"), SugarColumn(IsNullable = true)]
    public bool? AllowClear { get; set; }
}

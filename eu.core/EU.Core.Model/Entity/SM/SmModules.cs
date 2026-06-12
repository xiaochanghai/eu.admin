/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModules.cs
*
* 功 能： N / A
* 类 名： SmModules
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/6/13 0:21:02  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 系统模板 (Model)
/// </summary>
[SugarTable("SmModules", "系统模板"), Entity(TableCnName = "系统模板", TableName = "SmModules")]
public class SmModules : BasePoco
{

    /// <summary>
    /// 模块代码
    /// </summary>
    [Display(Name = "ModuleCode"), Description("模块代码"), SugarColumn(IsNullable = true, Length = 50)]

    /// <summary>
    /// 模块名称
    /// </summary>
    [Display(Name = "ModuleName"), Description("模块名称"), SugarColumn(IsNullable = true, Length = 50)]

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    [Display(Name = "Icon"), Description("图标"), SugarColumn(IsNullable = true, Length = -1)]

    /// <summary>
    /// 路由
    /// </summary>
    [Display(Name = "RoutePath"), Description("路由"), SugarColumn(IsNullable = true, Length = 50)]

    /// <summary>
    /// 上级模块ID
    /// </summary>
    [Display(Name = "ParentId"), Description("上级模块ID"), SugarColumn(IsNullable = true)]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 是否目录
    /// </summary>
    [Display(Name = "IsParent"), Description("是否目录"), SugarColumn(IsNullable = true)]
    public bool? IsParent { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    [Display(Name = "ApiUrl"), Description("请求地址"), SugarColumn(IsNullable = true, Length = 64)]

    /// <summary>
    /// 是否允许新建
    /// </summary>
    [Display(Name = "IsShowAdd"), Description("是否允许新建"), SugarColumn(IsNullable = true)]
    public bool? IsShowAdd { get; set; }

    /// <summary>
    /// 是否允许批量删除
    /// </summary>
    [Display(Name = "IsShowBatchDelete"), Description("是否允许批量删除"), SugarColumn(IsNullable = true)]
    public bool? IsShowBatchDelete { get; set; }

    /// <summary>
    /// 是否允许删除
    /// </summary>
    [Display(Name = "IsShowDelete"), Description("是否允许删除"), SugarColumn(IsNullable = true)]
    public bool? IsShowDelete { get; set; }

    /// <summary>
    /// 是否允许更新
    /// </summary>
    [Display(Name = "IsShowUpdate"), Description("是否允许更新"), SugarColumn(IsNullable = true)]
    public bool? IsShowUpdate { get; set; }

    /// <summary>
    /// 是否允许查看
    /// </summary>
    [Display(Name = "IsShowView"), Description("是否允许查看"), SugarColumn(IsNullable = true)]
    public bool? IsShowView { get; set; }

    /// <summary>
    /// 是否从表
    /// </summary>
    [Display(Name = "IsDetail"), Description("是否从表"), SugarColumn(IsNullable = true)]
    public bool? IsDetail { get; set; }

    /// <summary>
    /// 所属模块ID
    /// </summary>
    [Display(Name = "BelongModuleId"), Description("所属模块ID"), SugarColumn(IsNullable = true)]
    public Guid? BelongModuleId { get; set; }

    /// <summary>
    /// 是否允许提交
    /// </summary>
    [Display(Name = "IsShowSubmit"), Description("是否允许提交"), SugarColumn(IsNullable = true)]
    public bool? IsShowSubmit { get; set; }

    /// <summary>
    /// DefaultSort
    /// </summary>
    [Display(Name = "DefaultSort"), Description("DefaultSort"), SugarColumn(IsNullable = true, Length = 50)]

    /// <summary>
    /// DefaultSortOrder
    /// </summary>
    [Display(Name = "DefaultSortOrder"), Description("DefaultSortOrder"), SugarColumn(IsNullable = true, Length = 50)]

    /// <summary>
    /// 是否允许审核
    /// </summary>
    [Display(Name = "IsShowAudit"), Description("是否允许审核"), SugarColumn(IsNullable = true)]
    public bool? IsShowAudit { get; set; }

    /// <summary>
    /// IsShowGoBack
    /// </summary>
    [Display(Name = "IsShowGoBack"), Description("IsShowGoBack"), SugarColumn(IsNullable = true)]
    public bool? IsShowGoBack { get; set; }

    /// <summary>
    /// 是否执行查询
    /// </summary>
    [Display(Name = "IsExecQuery"), Description("是否执行查询"), SugarColumn(IsNullable = true)]
    public bool? IsExecQuery { get; set; }

    /// <summary>
    /// 是否合计
    /// </summary>
    [Display(Name = "IsSum"), Description("是否合计"), SugarColumn(IsNullable = true)]
    public bool? IsSum { get; set; }

    /// <summary>
    /// 打开方式
    /// </summary>
    [Display(Name = "OpenType"), Description("打开方式"), SugarColumn(IsNullable = true, Length = 32)]

    /// <summary>
    /// 编辑页路径
    /// </summary>
    [Display(Name = "FormPage"), Description("编辑页路径"), SugarColumn(IsNullable = true, Length = 128)]

    /// <summary>
    /// 模块类型，Form表单/FormGroup表单集
    /// </summary>
    [Display(Name = "ModuleType"), Description("模块类型，Form表单/FormGroup表单集"), SugarColumn(IsNullable = true, Length = 32)]

    /// <summary>
    /// 编辑页宽度
    /// </summary>
    [Display(Name = "FormPageWidth"), Description("编辑页宽度"), SugarColumn(IsNullable = true)]
    public int? FormPageWidth { get; set; }

    /// <summary>
    /// 路由文件地址
    /// </summary>
    [Display(Name = "Element"), Description("路由文件地址"), SugarColumn(IsNullable = true, Length = 64)]

    /// <summary>
    /// 是否全屏
    /// </summary>
    [Display(Name = "IsFull"), Description("是否全屏"), SugarColumn(IsNullable = true)]
    public bool? IsFull { get; set; }

    /// <summary>
    /// 是否导出Excel
    /// </summary>
    [Display(Name = "IsExportExcel"), Description("是否导出Excel"), SugarColumn(IsNullable = true)]
    public bool? IsExportExcel { get; set; }

    /// <summary>
    /// 是否导入Excel
    /// </summary>
    [Display(Name = "IsImportExcel"), Description("是否导入Excel"), SugarColumn(IsNullable = true)]
    public bool? IsImportExcel { get; set; }

    /// <summary>
    /// 表格行是否可选择
    /// </summary>
    [Display(Name = "IsDisplayRowSelection"), Description("表格行是否可选择"), SugarColumn(IsNullable = true)]
    public bool? IsDisplayRowSelection { get; set; }
}

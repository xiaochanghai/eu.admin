/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModules.cs
*
*功 能： N / A
* 类 名： SmModules
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/20 23:12:41  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models
{

    /// <summary>
    /// 系统模板 (Dto.Base)
    /// </summary>
    public class SmModulesBase
    {

        /// <summary>
        /// 模块代码
        /// </summary>
        [Display(Name = "ModuleCode"), Description("模块代码"), MaxLength(50, ErrorMessage = "模块代码 不能超过 50 个字符")]
        public string ModuleCode { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        [Display(Name = "ModuleName"), Description("模块名称"), MaxLength(50, ErrorMessage = "模块名称 不能超过 50 个字符")]
        public string ModuleName { get; set; }

        /// <summary>
        /// 模块类型
        /// </summary>
        [Display(Name = "ModuleType"), Description("模块类型"), MaxLength(50, ErrorMessage = "模块类型 不能超过 50 个字符")]
        public string ModuleType { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int? TaxisNo { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [Display(Name = "Icon"), Description("图标"), MaxLength(-1, ErrorMessage = "图标 不能超过 -1 个字符")]
        public string Icon { get; set; }

        /// <summary>
        /// 路由地址
        /// </summary>
        [Display(Name = "RoutePath"), Description("路由地址"), MaxLength(50, ErrorMessage = "路由地址 不能超过 50 个字符")]
        public string RoutePath { get; set; }


        public string Element { get; set; }

        /// <summary>
        /// ParentId
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// IsParent
        /// </summary>
        public bool? IsParent { get; set; }

        /// <summary>
        /// 请求地址
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// 是否显示新增
        /// </summary>
        public bool? IsShowAdd { get; set; }

        /// <summary>
        /// 是否显示批量删除
        /// </summary>
        public bool? IsShowBatchDelete { get; set; }

        /// <summary>
        /// 是否显示删除
        /// </summary>
        public bool? IsShowDelete { get; set; }

        /// <summary>
        /// 是否显示更新
        /// </summary>
        public bool? IsShowUpdate { get; set; }

        /// <summary>
        /// 是否显示查看
        /// </summary>
        public bool? IsShowView { get; set; }

        /// <summary>
        /// 是否为从表
        /// </summary>
        public bool? IsDetail { get; set; }

        /// <summary>
        /// 所属模块ID
        /// </summary>
        public Guid? BelongModuleId { get; set; }

        /// <summary>
        /// 是否显示提交
        /// </summary>
        public bool? IsShowSubmit { get; set; }

        /// <summary>
        /// 默认排序
        /// </summary>
        [Display(Name = "DefaultSort"), Description("默认排序"), MaxLength(50, ErrorMessage = "默认排序 不能超过 50 个字符")]
        public string DefaultSort { get; set; }

        /// <summary>
        /// 默认正序/倒序
        /// </summary>
        [Display(Name = "DefaultSortOrder"), Description("默认正序/倒序"), MaxLength(50, ErrorMessage = "默认正序/倒序 不能超过 50 个字符")]
        public string DefaultSortOrder { get; set; }

        /// <summary>
        /// 是否显示审核
        /// </summary>
        public bool? IsShowAudit { get; set; }

        /// <summary>
        /// 是否显示返回
        /// </summary>
        public bool? IsShowGoBack { get; set; }

        /// <summary>
        /// 是否执行查询
        /// </summary>
        public bool? IsExecQuery { get; set; }

        /// <summary>
        /// 是否合计
        /// </summary>
        public bool? IsSum { get; set; }

        /// <summary>
        /// 打开方式
        /// </summary>
        [Display(Name = "OpenType"), Description("打开方式"), MaxLength(50, ErrorMessage = "打开方式 不能超过 32 个字符")]
        public string OpenType { get; set; }

        /// <summary>
        /// 编辑页路径
        /// </summary>
        [Display(Name = "FormPage"), Description("编辑页路径"), MaxLength(50, ErrorMessage = "打开方式 不能超过 32 个字符")]
        public string FormPage { get; set; }

        /// <summary>
        /// 编辑页宽度
        /// </summary>
        public int? FormPageWidth { get; set; }
    }
}

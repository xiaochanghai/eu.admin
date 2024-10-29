/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessTemplate.cs
*
*功 能： N / A
* 类 名： PsProcessTemplate
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:30  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/ 
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace EU.Core.Model.Models
{

    /// <summary>
    /// PsProcessTemplate (Model)
    /// </summary>
    [SugarTable("PsProcessTemplate", "PsProcessTemplate"), Entity(TableCnName = "PsProcessTemplate", TableName = "PsProcessTemplate")]
    public class PsProcessTemplate : BasePoco
    {

        /// <summary>
        /// 模版单号
        /// </summary>
        [Display(Name = "TemplateNo"), Description("模版单号"), MaxLength(32, ErrorMessage = "模版单号 不能超过 32 个字符")]
        public string TemplateNo { get; set; }

        /// <summary>
        /// 模版名称
        /// </summary>
        [Display(Name = "TemplateName"), Description("模版名称"), MaxLength(32, ErrorMessage = "模版名称 不能超过 32 个字符")]
        public string TemplateName { get; set; }

        /// <summary>
        /// 工艺流程
        /// </summary>
        [Display(Name = "FlowList"), Description("工艺流程"), MaxLength(2000, ErrorMessage = "工艺流程 不能超过 2000 个字符")]
        public string FlowList { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

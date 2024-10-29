/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessEmployee.cs
*
*功 能： N / A
* 类 名： PsProcessEmployee
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:26  SimonHsiao   初版
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
    /// PsProcessEmployee (Model)
    /// </summary>
    [SugarTable("PsProcessEmployee", "PsProcessEmployee"), Entity(TableCnName = "PsProcessEmployee", TableName = "PsProcessEmployee")]
    public class PsProcessEmployee : BasePoco
    {

        /// <summary>
        /// 工序单价ID
        /// </summary>
        public Guid? ProcessId { get; set; }

        /// <summary>
        /// 员工ID
        /// </summary>
        public Guid? EmployeeId { get; set; }

        /// <summary>
        /// 机台ID
        /// </summary>
        public Guid? MachineId { get; set; }

        /// <summary>
        /// 标准单价
        /// </summary>
        [Display(Name = "Position"), Description("标准单价"), MaxLength(32, ErrorMessage = "标准单价 不能超过 32 个字符")]
        public string Position { get; set; }

        /// <summary>
        /// 联系方式
        /// </summary>
        [Display(Name = "Contact"), Description("联系方式"), MaxLength(64, ErrorMessage = "联系方式 不能超过 64 个字符")]
        public string Contact { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoRequestion.cs
*
*功 能： N / A
* 类 名： PoRequestion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/9/4 16:16:32  SimonHsiao   初版
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
    /// 请购单 (Model)
    /// </summary>
    [SugarTable("PoRequestion", "PoRequestion"), Entity(TableCnName = "请购单", TableName = "PoRequestion")]
    public class PoRequestion : BasePoco
    {

        /// <summary>
        /// 请购单号
        /// </summary>
        [Display(Name = "OrderNo"), Description("请购单号"), MaxLength(32, ErrorMessage = "请购单号 不能超过 32 个字符")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 请购类型
        /// </summary>
        [Display(Name = "RequestionType"), Description("请购类型"), MaxLength(10, ErrorMessage = "请购类型 不能超过 32 个字符")]
        public string RequestionType { get; set; }

        /// <summary>
        /// 请购人
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 需求日期
        /// </summary>
        public DateTime? RequestionDate { get; set; }

        /// <summary>
        /// 请购时间
        /// </summary>
        public DateTime? OrderDate { get; set; }

        /// <summary>
        /// 请购部门
        /// </summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        [Display(Name = "OrderStatus"), Description("订单状态"), MaxLength(32, ErrorMessage = "订单状态 不能超过 32 个字符")]
        public string OrderStatus { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

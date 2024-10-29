/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsWorkShop.cs
*
*功 能： N / A
* 类 名： PsWorkShop
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:34  SimonHsiao   初版
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
    /// PsWorkShop (Model)
    /// </summary>
    [SugarTable("PsWorkShop", "PsWorkShop"), Entity(TableCnName = "PsWorkShop", TableName = "PsWorkShop")]
    public class PsWorkShop : BasePoco
    {

        /// <summary>
        /// 车间编号
        /// </summary>
        [Display(Name = "WorkShopNo"), Description("车间编号"), MaxLength(32, ErrorMessage = "车间编号 不能超过 32 个字符")]
        public string WorkShopNo { get; set; }

        /// <summary>
        /// 车间名称
        /// </summary>
        [Display(Name = "WorkShopName"), Description("车间名称"), MaxLength(32, ErrorMessage = "车间名称 不能超过 32 个字符")]
        public string WorkShopName { get; set; }

        /// <summary>
        /// 负责人ID
        /// </summary>
        public Guid? ChargeId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

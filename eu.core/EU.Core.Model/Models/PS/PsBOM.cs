/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsBOM.cs
*
*功 能： N / A
* 类 名： PsBOM
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:19  SimonHsiao   初版
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
    /// PsBOM (Model)
    /// </summary>
    [SugarTable("PsBOM", "PsBOM"), Entity(TableCnName = "PsBOM", TableName = "PsBOM")]
    public class PsBOM : BasePoco
    {

        /// <summary>
        /// 货品编号
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 制造车间ID
        /// </summary>
        public Guid? WorkShopId { get; set; }

        /// <summary>
        /// BOM版本
        /// </summary>
        [Display(Name = "Version"), Description("BOM版本"), MaxLength(32, ErrorMessage = "BOM版本 不能超过 32 个字符")]
        public string Version { get; set; }

        /// <summary>
        /// 批量
        /// </summary>
        public int? BulkQty { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }

        /// <summary>
        /// 是否最新
        /// </summary>
        public bool? IsLatest { get; set; }

        /// <summary>
        /// 工序流程
        /// </summary>
        [Display(Name = "Process"), Description("工序流程"), MaxLength(2000, ErrorMessage = "工序流程 不能超过 2000 个字符")]
        public string Process { get; set; }
    }
}

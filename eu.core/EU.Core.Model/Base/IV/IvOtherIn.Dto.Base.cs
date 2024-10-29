/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvOtherIn.cs
*
*功 能： N / A
* 类 名： IvOtherIn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 13:34:17  SimonHsiao   初版
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

namespace EU.Core.Model.Models
{

    /// <summary>
    /// 其他入库单 (Dto.Base)
    /// </summary>
    public class IvOtherInBase
    {

        /// <summary>
        /// 单号
        /// </summary>
        [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 入库类型
        /// </summary>
        [Display(Name = "InType"), Description("入库类型"), MaxLength(32, ErrorMessage = "入库类型 不能超过 32 个字符")]
        public string InType { get; set; }

        /// <summary>
        /// 作业日期
        /// </summary>
        public DateTime? OrderDate { get; set; }

        /// <summary>
        /// 建立人ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 入库仓库ID
        /// </summary>
        public Guid? StockId { get; set; }

        /// <summary>
        /// 入库货位ID
        /// </summary>
        public Guid? GoodsLocationId { get; set; }

        /// <summary>
        /// 部门ID
        /// </summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsBOMMaterial.cs
*
*功 能： N / A
* 类 名： PsBOMMaterial
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/5/6 14:58:20  SimonHsiao   初版
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
    /// PsBOMMaterial (Dto.Base)
    /// </summary>
    public class PsBOMMaterialBase
    {

        /// <summary>
        /// BOMID
        /// </summary>
        public Guid? BOMId { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int? SerialNumber { get; set; }

        /// <summary>
        /// 货品编号
        /// </summary>
        public Guid? MaterialId { get; set; }

        /// <summary>
        /// 用量
        /// </summary>
        [Display(Name = "Dosage"), Description("用量"), Column(TypeName = "decimal(20,4)")]
        public decimal? Dosage { get; set; }

        /// <summary>
        /// 用量基数，默认1，用量基数为分母，用量为分子（比如用量基数为1000，用量为1，则单个用量则为千分之一）
        /// </summary>
        [Display(Name = "DosageBase"), Description("用量基数，默认1，用量基数为分母，用量为分子（比如用量基数为1000，用量为1，则单个用量则为千分之一）"), Column(TypeName = "decimal(20,4)")]
        public decimal? DosageBase { get; set; }

        /// <summary>
        /// 损耗率，最好可设置阶梯损耗，比如0～1000损耗率为3%，1001～10000损耗率为2%，后续计算物料用量需要加上损耗
        /// </summary>
        [Display(Name = "WastageRate"), Description("损耗率，最好可设置阶梯损耗，比如0～1000损耗率为3%，1001～10000损耗率为2%，后续计算物料用量需要加上损耗"), Column(TypeName = "decimal(20,2)")]
        public decimal? WastageRate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}

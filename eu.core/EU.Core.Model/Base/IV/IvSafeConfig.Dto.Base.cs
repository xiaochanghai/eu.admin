/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvSafeConfig.cs
*
*功 能： N / A
* 类 名： IvSafeConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/12/18 15:51:17  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 安全库存设置 (Dto.Base)
/// </summary>
public class IvSafeConfigBase
{

    /// <summary>
    /// 物料ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("物料ID")]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 安全库存量
    /// </summary>
    [Display(Name = "Safe_QTY"), Description("安全库存量"), Column(TypeName = "decimal(20,8)")]
    public decimal? Safe_QTY { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

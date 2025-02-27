/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvSafeConfig.cs
*
* 功 能： N / A
* 类 名： IvSafeConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:01  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 安全库存设置 (Model)
/// </summary>
[SugarTable("IvSafeConfig", "安全库存设置"), Entity(TableCnName = "安全库存设置", TableName = "IvSafeConfig")]
public class IvSafeConfig : BasePoco
{

    /// <summary>
    /// 物料ID
    /// </summary>
    [Display(Name = "MaterialId"), Description("物料ID"), SugarColumn(IsNullable = true)]
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// 安全库存量
    /// </summary>
    [Display(Name = "Safe_QTY"), Description("安全库存量"), Column(TypeName = "decimal(20,8)"), SugarColumn(IsNullable = true, Length = 20, DecimalDigits = 8)]
    public decimal? Safe_QTY { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

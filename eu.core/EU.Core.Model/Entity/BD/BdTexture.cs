/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdTexture.cs
*
* 功 能： N / A
* 类 名： BdTexture
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/18 20:01:53  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 材质 (Model)
/// </summary>
[SugarTable("BdTexture", "材质"), Entity(TableCnName = "材质", TableName = "BdTexture")]
public class BdTexture : BasePoco
{

    /// <summary>
    /// 材质编号
    /// </summary>
    [Display(Name = "TextureNo"), Description("材质编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string TextureNo { get; set; }

    /// <summary>
    /// 材质名称
    /// </summary>
    [Display(Name = "TextureNames"), Description("材质名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string TextureNames { get; set; }

    /// <summary>
    /// 比重
    /// </summary>
    [Display(Name = "Proportion"), Description("比重"), SugarColumn(IsNullable = true)]
    public int? Proportion { get; set; }

    /// <summary>
    /// 基数
    /// </summary>
    [Display(Name = "BaseAmount"), Description("基数"), SugarColumn(IsNullable = true)]
    public int? BaseAmount { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

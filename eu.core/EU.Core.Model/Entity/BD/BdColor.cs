/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdColor.cs
*
* 功 能： N / A
* 类 名： BdColor
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/18 20:11:58  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 颜色 (Model)
/// </summary>
[SugarTable("BdColor", "颜色"), Entity(TableCnName = "颜色", TableName = "BdColor")]
public class BdColor : BasePoco
{

    /// <summary>
    /// 颜色编号
    /// </summary>
    [Display(Name = "ColorNo"), Description("颜色编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string ColorNo { get; set; }

    /// <summary>
    /// 颜色名称
    /// </summary>
    [Display(Name = "ColorNames"), Description("颜色名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string ColorNames { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

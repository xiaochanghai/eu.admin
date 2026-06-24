/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmGroup.cs
*
* 功 能： N / A
* 类 名： SmGroup
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/6/25 0:36:21  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// SmGroup (Model)
/// </summary>
[SugarTable("SmGroup", "SmGroup"), Entity(TableCnName = "SmGroup", TableName = "SmGroup")]
public class SmGroup : BasePoco
{

    /// <summary>
    /// 集团代码
    /// </summary>
    [Display(Name = "GroupCode"), Description("集团代码"), SugarColumn(IsNullable = true, Length = 64)]
    public string GroupCode { get; set; }

    /// <summary>
    /// 集团名称
    /// </summary>
    [Display(Name = "GroupName"), Description("集团名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string GroupName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

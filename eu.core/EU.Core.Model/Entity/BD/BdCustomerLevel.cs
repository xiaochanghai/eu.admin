/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCustomerLevel.cs
*
* 功 能： N / A
* 类 名： BdCustomerLevel
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/28 10:57:50  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 客户等级 (Model)
/// </summary>
[SugarTable("BdCustomerLevel", "客户等级"), Entity(TableCnName = "客户等级", TableName = "BdCustomerLevel")]
public class BdCustomerLevel : BasePoco
{

    /// <summary>
    /// 等级编号
    /// </summary>
    [Display(Name = "LevelNo"), Description("等级编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string LevelNo { get; set; }

    /// <summary>
    /// 等级名称
    /// </summary>
    [Display(Name = "LevelName"), Description("等级名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string LevelName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

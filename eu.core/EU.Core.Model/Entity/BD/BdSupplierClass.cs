/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSupplierClass.cs
*
* 功 能： N / A
* 类 名： BdSupplierClass
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/18 22:24:48  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 供应商分类 (Model)
/// </summary>
[SugarTable("BdSupplierClass", "供应商分类"), Entity(TableCnName = "供应商分类", TableName = "BdSupplierClass")]
public class BdSupplierClass : BasePoco
{

    /// <summary>
    /// 分类编号
    /// </summary>
    [Display(Name = "ClassNo"), Description("分类编号"), SugarColumn(IsNullable = true, Length = 64)]
    public string ClassNo { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [Display(Name = "ClassName"), Description("分类名称"), SugarColumn(IsNullable = true, Length = 64)]
    public string ClassName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

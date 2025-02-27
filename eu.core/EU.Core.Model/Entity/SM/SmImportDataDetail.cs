/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportDataDetail.cs
*
* 功 能： N / A
* 类 名： SmImportDataDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:49  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 系统导入数据明细 (Model)
/// </summary>
[SugarTable("SmImportDataDetail", "系统导入数据明细"), Entity(TableCnName = "系统导入数据明细", TableName = "SmImportDataDetail")]
public class SmImportDataDetail : BasePoco
{

    /// <summary>
    /// Execl列号
    /// </summary>
    [Display(Name = "LineNo"), Description("Execl列号"), SugarColumn(IsNullable = true)]
    public int? LineNo { get; set; }

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), SugarColumn(IsNullable = true, Length = 32)]
    public string SheetName { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col1"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col1 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col2"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col2 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col3"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col3 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col4"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col4 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col5"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col5 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col6"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col6 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col7"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col7 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col8"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col8 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col9"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col9 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col10"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col10 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col11"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col11 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col12"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col12 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col13"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col13 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col14"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col14 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col15"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col15 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col16"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col16 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col17"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col17 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col18"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col18 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col19"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col19 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col20"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col20 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col21"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col21 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col22"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col22 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col23"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col23 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col24"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col24 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col25"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col25 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col26"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col26 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col27"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col27 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col28"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col28 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col29"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col29 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col30"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col30 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col31"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col31 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col32"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col32 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col33"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col33 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col34"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col34 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col35"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col35 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col36"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col36 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col37"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col37 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col38"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col38 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col39"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col39 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col40"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col40 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col41"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col41 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col42"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col42 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col43"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col43 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col44"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col44 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col45"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col45 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col46"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col46 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col47"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col47 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col48"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col48 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col49"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col49 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col50"), Description("Col"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Col50 { get; set; }

    /// <summary>
    /// 是否错误
    /// </summary>
    [Display(Name = "IsError"), Description("是否错误"), SugarColumn(IsNullable = true)]
    public bool? IsError { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportDataDetail.cs
*
*功 能： N / A
* 类 名： SmImportDataDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/22 9:46:30  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 系统导入数据明细 (Dto.Base)
/// </summary>
public class SmImportDataDetailBase
{

    /// <summary>
    /// Execl列号
    /// </summary>
    public int? LineNo { get; set; }

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), MaxLength(32, ErrorMessage = "Sheet名 不能超过 32 个字符")]
    public string SheetName { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col1"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col1 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col2"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col2 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col3"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col3 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col4"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col4 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col5"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col5 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col6"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col6 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col7"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col7 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col8"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col8 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col9"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col9 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col10"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col10 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col11"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col11 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col12"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col12 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col13"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col13 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col14"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col14 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col15"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col15 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col16"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col16 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col17"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col17 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col18"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col18 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col19"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col19 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col20"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col20 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col21"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col21 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col22"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col22 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col23"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col23 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col24"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col24 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col25"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col25 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col26"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col26 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col27"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col27 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col28"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col28 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col29"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col29 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col30"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col30 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col31"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col31 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col32"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col32 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col33"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col33 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col34"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col34 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col35"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col35 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col36"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col36 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col37"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col37 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col38"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col38 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col39"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col39 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col40"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col40 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col41"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col41 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col42"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col42 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col43"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col43 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col44"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col44 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col45"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col45 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col46"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col46 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col47"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col47 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col48"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col48 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col49"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col49 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col50"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col50 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col51"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col51 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col52"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col52 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col53"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col53 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col54"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col54 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col55"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col55 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col56"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col56 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col57"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col57 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col58"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col58 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col59"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col59 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col60"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col60 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col61"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col61 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col62"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col62 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col63"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col63 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col64"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col64 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col65"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col65 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col66"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col66 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col67"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col67 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col68"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col68 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col69"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col69 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col70"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col70 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col71"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col71 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col72"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col72 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col73"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col73 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col74"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col74 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col75"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col75 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col76"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col76 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col77"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col77 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col78"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col78 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col79"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col79 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col80"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col80 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col81"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col81 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col82"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col82 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col83"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col83 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col84"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col84 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col85"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col85 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col86"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col86 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col87"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col87 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col88"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col88 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col89"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col89 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col90"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col90 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col91"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col91 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col92"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col92 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col93"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col93 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col94"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col94 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col95"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col95 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col96"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col96 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col97"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col97 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col98"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col98 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col99"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col99 { get; set; }

    /// <summary>
    /// Col
    /// </summary>
    [Display(Name = "Col100"), Description("Col"), MaxLength(2000, ErrorMessage = "Col 不能超过 2000 个字符")]
    public string Col100 { get; set; }

    /// <summary>
    /// 是否错误
    /// </summary>
    public bool? IsError { get; set; }
}

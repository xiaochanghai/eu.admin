/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLov.cs
*
*功 能： N / A
* 类 名： SmLov
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/4/21 1:10:41  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// SmLov (Dto.Base)
/// </summary>
public class SmLovBase
{

    /// <summary>
    /// LovCode
    /// </summary>
    [Display(Name = "LovCode"), Description("LovCode"), MaxLength(50, ErrorMessage = "LovCode 不能超过 50 个字符")]
    public string LovCode { get; set; }

    /// <summary>
    /// LovName
    /// </summary>
    [Display(Name = "LovName"), Description("LovName"), MaxLength(50, ErrorMessage = "LovName 不能超过 50 个字符")]
    public string LovName { get; set; }

    /// <summary>
    /// InureTime
    /// </summary>
    public DateTime? InureTime { get; set; }

    /// <summary>
    /// AbateTime
    /// </summary>
    public DateTime? AbateTime { get; set; }
}

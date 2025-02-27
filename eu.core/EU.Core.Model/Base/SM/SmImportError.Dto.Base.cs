/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportError.cs
*
* 功 能： N / A
* 类 名： SmImportError
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:51  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 系统导入模板异常信息 (Dto.Base)
/// </summary>
public class SmImportErrorBase : BasePoco
{

    /// <summary>
    /// 错误代码
    /// </summary>
    [Display(Name = "ErrorCode"), Description("错误代码"), MaxLength(32, ErrorMessage = "错误代码 不能超过 32 个字符")]
    public string ErrorCode { get; set; }

    /// <summary>
    /// 错误名称
    /// </summary>
    [Display(Name = "ErrorName"), Description("错误名称"), MaxLength(2000, ErrorMessage = "错误名称 不能超过 2000 个字符")]
    public string ErrorName { get; set; }

    /// <summary>
    /// 错误类型
    /// </summary>
    [Display(Name = "ErrorType"), Description("错误类型"), MaxLength(32, ErrorMessage = "错误类型 不能超过 32 个字符")]
    public string ErrorType { get; set; }

    /// <summary>
    /// 模块代码
    /// </summary>
    [Display(Name = "ModuleCode"), Description("模块代码"), MaxLength(32, ErrorMessage = "模块代码 不能超过 32 个字符")]
    public string ModuleCode { get; set; }

    /// <summary>
    /// Sheet名
    /// </summary>
    [Display(Name = "SheetName"), Description("Sheet名"), MaxLength(32, ErrorMessage = "Sheet名 不能超过 32 个字符")]
    public string SheetName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

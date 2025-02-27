/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCommonListSql.cs
*
* 功 能： N / A
* 类 名： SmCommonListSql
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:30:36  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 系统通用下拉 (Dto.Base)
/// </summary>
public class SmCommonListSqlBase : BasePoco
{

    /// <summary>
    /// 通用代码
    /// </summary>
    [Display(Name = "CommonCode"), Description("通用代码"), MaxLength(32, ErrorMessage = "通用代码 不能超过 32 个字符")]
    public string CommonCode { get; set; }

    /// <summary>
    /// 通用名称
    /// </summary>
    [Display(Name = "CommonName"), Description("通用名称"), MaxLength(32, ErrorMessage = "通用名称 不能超过 32 个字符")]
    public string CommonName { get; set; }

    /// <summary>
    /// 查询SQL
    /// </summary>
    [Display(Name = "SelectSql"), Description("查询SQL"), MaxLength(2000, ErrorMessage = "查询SQL 不能超过 2000 个字符")]
    public string SelectSql { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

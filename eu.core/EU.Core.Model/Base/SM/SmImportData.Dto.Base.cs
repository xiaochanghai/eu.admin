/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportData.cs
*
* 功 能： N / A
* 类 名： SmImportData
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/10/27 11:46:06  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 导入数据表 (Dto.Base)
/// </summary>
public class SmImportDataBase : BasePoco
{

    /// <summary>
    /// 导入文件名
    /// </summary>
    [Display(Name = "ImportFileName"), Description("导入文件名"), MaxLength(128, ErrorMessage = "导入文件名 不能超过 128 个字符")]
    public string ImportFileName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

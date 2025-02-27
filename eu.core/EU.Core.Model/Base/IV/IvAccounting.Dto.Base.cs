/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvAccounting.cs
*
* 功 能： N / A
* 类 名： IvAccounting
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:29:50  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 库存建帐 (Dto.Base)
/// </summary>
public class IvAccountingBase : BasePoco
{

    /// <summary>
    /// 单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("单号"), MaxLength(32, ErrorMessage = "单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 初始化日期
    /// </summary>
    [Display(Name = "OrderDate"), Description("初始化日期")]
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

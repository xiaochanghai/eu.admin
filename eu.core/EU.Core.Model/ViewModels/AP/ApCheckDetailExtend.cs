/* 代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoOrderDetail.cs
*
*功 能： N / A
* 类 名： PoOrderDetail
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
*V0.01 2021/2/8 13:58:29 SimonHsiao 初版
*
* Copyright(c) 2021 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 应付对账单明细
/// </summary>
public class ApCheckDetailExtend : ApCheckDetail
{

    /// <summary>
    /// 物料名称
    /// </summary>
    public string MaterialNo { get; set; }
    /// <summary>
    /// 物料名称
    /// </summary>
    public string MaterialName { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    public string Specifications { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 剩余对账数量
    /// </summary>
    public decimal? MaxCheckQTY { get; set; }
}
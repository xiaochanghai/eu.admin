/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdDeliveryWay.cs
*
* 功 能： N / A
* 类 名： BdDeliveryWay
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/18 22:20:13  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 送货方式 (Dto.Base)
/// </summary>
public class BdDeliveryWayBase : BasePoco
{

    /// <summary>
    /// 送货编号
    /// </summary>
    [Display(Name = "DeliveryNo"), Description("送货编号"), MaxLength(32, ErrorMessage = "送货编号 不能超过 32 个字符")]
    public string DeliveryNo { get; set; }

    /// <summary>
    /// 送货名称
    /// </summary>
    [Display(Name = "DeliveryName"), Description("送货名称"), MaxLength(32, ErrorMessage = "送货名称 不能超过 32 个字符")]
    public string DeliveryName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

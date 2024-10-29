/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrder.cs
*
*功 能： N / A
* 类 名： SdReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/8/28 11:57:11  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 退库单 (Dto.Base)
/// </summary>
public class SdReturnOrderBase
{

    /// <summary>
    /// 订单号
    /// </summary>
    [Display(Name = "OrderNo"), Description("订单号"), MaxLength(32, ErrorMessage = "订单号 不能超过 32 个字符")]
    public string OrderNo { get; set; }

    /// <summary>
    /// 订单类型 正式订单、样品订单、其他订单（默认正式订单）
    /// </summary>
    [Display(Name = "OrderCategory"), Description("订单类型 正式订单、样品订单、其他订单（默认正式订单）"), MaxLength(32, ErrorMessage = "订单类型 正式订单、样品订单、其他订单（默认正式订单） 不能超过 32 个字符")]
    public string OrderCategory { get; set; }

    /// <summary>
    /// 作业日期
    /// </summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// 客户单号
    /// </summary>
    [Display(Name = "CustomerOrderNo"), Description("客户单号"), MaxLength(64, ErrorMessage = "客户单号 不能超过 64 个字符")]
    public string CustomerOrderNo { get; set; }

    /// <summary>
    /// 预定交期
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 订单等级
    /// </summary>
    [Display(Name = "OrderLevel"), Description("订单等级"), MaxLength(32, ErrorMessage = "订单等级 不能超过 32 个字符")]
    public string OrderLevel { get; set; }

    /// <summary>
    /// 业务员ID
    /// </summary>
    public Guid? SalesmanId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    public Guid? StockId { get; set; }

    /// <summary>
    /// 货位ID
    /// </summary>
    public Guid? GoodsLocationId { get; set; }

    /// <summary>
    /// 退回原因
    /// </summary>
    [Display(Name = "ReturnReason"), Description("退回原因"), MaxLength(32, ErrorMessage = "退回原因 不能超过 32 个字符")]
    public string ReturnReason { get; set; }

    /// <summary>
    /// 退货日期
    /// </summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// 退货状态--待退回、已退回（保存后，单据状态为待退回，增加退回入库按钮，点击确认后，单据状态为已退回）
    /// </summary>
    [Display(Name = "OrderStatus"), Description("退货状态--待退回、已退回（保存后，单据状态为待退回，增加退回入库按钮，点击确认后，单据状态为已退回）"), MaxLength(32, ErrorMessage = "退货状态--待退回、已退回（保存后，单据状态为待退回，增加退回入库按钮，点击确认后，单据状态为已退回） 不能超过 32 个字符")]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}

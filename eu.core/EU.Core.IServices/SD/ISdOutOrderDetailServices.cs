/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrderDetail.cs
*
*功 能： N / A
* 类 名： SdOutOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/14 15:23:48  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 出库单明细(自定义服务接口)
/// </summary>	
public interface ISdOutOrderDetailServices : IBaseServices<SdOutOrderDetail, SdOutOrderDetailDto, InsertSdOutOrderDetailInput, EditSdOutOrderDetailInput>
{
    /// <summary>
    /// 变更销售单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    Task UpdateSalesOrderStatus(Guid? orderId);

    /// <summary>
    /// 变更出货通知单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    Task UpdateShipOrderStatus(Guid? orderId);
}
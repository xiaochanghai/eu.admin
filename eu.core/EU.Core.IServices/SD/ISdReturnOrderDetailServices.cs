/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrderDetail.cs
*
*功 能： N / A
* 类 名： SdReturnOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/29 13:06:20  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 退货单明细(自定义服务接口)
/// </summary>	
public interface ISdReturnOrderDetailServices : IBaseServices<SdReturnOrderDetail, SdReturnOrderDetailDto, InsertSdReturnOrderDetailInput, EditSdReturnOrderDetailInput>
{
    /// <summary>
    /// 变更出库单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    Task UpdateOutOrderStatus(Guid? orderId);
}
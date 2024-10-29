/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoArrivalOrder.cs
*
*功 能： N / A
* 类 名： PoArrivalOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/13 16:11:50  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 采购到货通知单(自定义服务接口)
/// </summary>	
public interface IPoArrivalOrderServices : IBaseServices<PoArrivalOrder, PoArrivalOrderDto, InsertPoArrivalOrderInput, EditPoArrivalOrderInput>
{
    Task<ServiceResult> BulkInsertDetailAsync(object entity, Guid id);

    Task<ServiceResult> BulkInsertInAsync(object entity);

    Task<ServiceResult> BulkOrderComplete(Guid[] ids);

    Task UpdateOrderStatus(Guid? orderId);
}
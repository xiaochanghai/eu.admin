/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoArrivalOrder.cs
*
*功 能： N / A
* 类 名： PoArrivalOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/13 16:11:49  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 采购到货通知单(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_PO)]
public class PoArrivalOrderController : BaseController<IPoArrivalOrderServices, PoArrivalOrder, PoArrivalOrderDto, InsertPoArrivalOrderInput, EditPoArrivalOrderInput>
{
    public PoArrivalOrderController(IPoArrivalOrderServices service) : base(service)
    {
    }

    #region 批量导入明细
    /// <summary>
    /// 批量导入明细
    /// </summary>
    /// <param name="entity">数据</param>
    /// <param name="id">订单ID</param>
    /// <returns></returns>
    [HttpPost("BulkInsertDetail/{id}")]
    public async Task<ServiceResult> BulkInsertDetailAsync([FromBody] object entity, Guid id)
    {
        return await _service.BulkInsertDetailAsync(entity, id);
    }
    #endregion

    #region 批量导入入库单
    /// <summary>
    /// 批量导入入库单
    /// </summary>
    /// <param name="entity">数据</param> 
    /// <returns></returns>
    [HttpPost("BulkInsertIn")]
    public async Task<ServiceResult> BulkInsertInAsync([FromBody] object entity)
    {
        return await _service.BulkInsertInAsync(entity);
    }
    #endregion

    #region 订单完结
    /// <summary>
    /// 订单完结
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpPost("BulkOrderComplete")]
    public async Task<ServiceResult> BulkOrderComplete([FromBody] Guid[] ids)
    {
        return await _service.BulkOrderComplete(ids);
    }
    #endregion
}
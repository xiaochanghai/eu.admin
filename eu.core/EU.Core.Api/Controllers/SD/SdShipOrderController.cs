/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdShipOrder.cs
*
*功 能： N / A
* 类 名： SdShipOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:50:12  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 发货单((Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SD)]
public class SdShipOrderController : BaseController<ISdShipOrderServices, SdShipOrder, SdShipOrderDto, InsertSdShipOrderInput, EditSdShipOrderInput>
{
    public SdShipOrderController(ISdShipOrderServices service) : base(service)
    {
    }

    #region 批量导入出货明细
    /// <summary>
    /// 批量导入出货明细
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("BulkInsertDetail/{id}")]
    public async Task<ServiceResult> BulkInsertShip([FromBody] object entity, Guid id)
    {
        return await _service.BulkInsertShipAsync(entity, id);
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

    #region 批量导入出库单
    /// <summary>
    /// 批量导入出库单
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [HttpPost("BulkInsertOut")]
    public async Task<ServiceResult> BulkInsertOut([FromBody] object entity)
    {
        return await _service.BulkInsertOutAsync(entity);
    }
    #endregion
}
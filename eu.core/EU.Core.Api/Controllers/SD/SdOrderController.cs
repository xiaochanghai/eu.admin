/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOrder.cs
*
* 功 能： N / A
* 类 名： SdOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/10/21 16:56:07  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 销售单(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SD)]
public class SdOrderController : BaseController<ISdOrderServices, SdOrder, SdOrderDto, InsertSdOrderInput, EditSdOrderInput>
{
    public SdOrderController(ISdOrderServices service) : base(service)
    {
    }
    #region 批量导入出货通知单
    /// <summary>
    /// 批量导入出货通知单
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [HttpPost("BulkInsertShip")]
    public async Task<ServiceResult> BulkInsertShip([FromBody] object entity)
    {
        return await _service.BulkInsertShipAsync(entity, "Ship");
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
        return await _service.BulkInsertShipAsync(entity, "Out");
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

    #region 订单变更
    /// <summary>
    /// 订单变更
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpPost("BulkOrderChange")]
    public async Task<ServiceResult> BulkOrderChange([FromBody] List<Guid> ids)
    {
        return await _service.BulkOrderChange(ids);
    }
    #endregion
}
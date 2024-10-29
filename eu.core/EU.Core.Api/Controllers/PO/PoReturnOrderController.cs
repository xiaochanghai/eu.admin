/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoReturnOrder.cs
*
* 功 能： N / A
* 类 名： PoReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/10/8 14:03:45  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 采购退货单(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_PO)]
public class PoReturnOrderController : BaseController<IPoReturnOrderServices, PoReturnOrder, PoReturnOrderDto, InsertPoReturnOrderInput, EditPoReturnOrderInput>
{
    public PoReturnOrderController(IPoReturnOrderServices service) : base(service)
    {
    }

    #region 订单完结
    /// <summary>
    /// 订单完结
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpPost("BulkOrderComplete")]
    public async Task<ServiceResult> BulkOrderComplete([FromBody] Guid[] ids) => await _service.BulkOrderComplete(ids);
    #endregion

    #region 过账
    /// <summary>
    /// 过账
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpPost("CarryTo")]
    public async Task<ServiceResult> BulkOrderCarryTo([FromBody] Guid[] ids) => await _service.BulkOrderCarryTo(ids);
    #endregion
}
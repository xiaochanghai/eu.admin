/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrder.cs
*
*功 能： N / A
* 类 名： SdReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/28 11:57:11  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 退库单(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SD)]
public class SdReturnOrderController : BaseController<ISdReturnOrderServices, SdReturnOrder, SdReturnOrderDto, InsertSdReturnOrderInput, EditSdReturnOrderInput>
{
    public SdReturnOrderController(ISdReturnOrderServices service) : base(service)
    {
    }

    #region 批量导入出库明细
    /// <summary>
    /// 批量导入出库明细
    /// </summary>
    /// <param name="entitys"></param>
    /// <param name="orderId"></param>
    /// <returns></returns>
    [HttpPost("BulkInsertDetail/{orderId}")]
    public async Task<ServiceResult> BulkInsertDetail([FromBody] List<SdReturnOrderDetail> entitys, Guid orderId) => await _service.BulkInsertDetailAsync(entitys, orderId);
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
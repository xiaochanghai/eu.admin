/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoOrder.cs
*
*功 能： N / A
* 类 名： PoOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/9 15:44:52  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 采购单(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_PO)]
public class PoOrderController : BaseController<IPoOrderServices, PoOrder, PoOrderDto, InsertPoOrderInput, EditPoOrderInput>
{
    public PoOrderController(IPoOrderServices service) : base(service)
    {
    }

    #region 批量导入出货通知单
    /// <summary>
    /// 批量导入出货通知单
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [HttpPost("BulkInsertNotice")]
    public async Task<ServiceResult> BulkInsertNotice([FromBody] object entity)
    {
        return await _service.BulkInsertNoticeOrInAsync(entity, "Notice");
    }
    #endregion

    #region 批量导入入库单
    /// <summary>
    /// 批量导入入库单
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [HttpPost("BulkInsertIn")]
    public async Task<ServiceResult> BulkInsertIn([FromBody] object entity)
    {
        return await _service.BulkInsertNoticeOrInAsync(entity, "In");
    }
    #endregion

}
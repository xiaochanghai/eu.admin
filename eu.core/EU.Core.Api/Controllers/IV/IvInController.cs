/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvIn.cs
*
* 功 能： N / A
* 类 名： IvIn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/12/18 15:40:25  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 库存入库单(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvInController : BaseController<IIvInServices, IvIn, IvInDto, InsertIvInInput, EditIvInInput>
{
    public IvInController(IIvInServices service) : base(service)
    {
    }

    #region 过账
    /// <summary>
    /// 过账
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpPost("Posting")]
    public async Task<ServiceResult> BulkOrderPosting([FromBody] Guid[] ids) => await _service.BulkOrderPostingAsync(ids);
    #endregion
}
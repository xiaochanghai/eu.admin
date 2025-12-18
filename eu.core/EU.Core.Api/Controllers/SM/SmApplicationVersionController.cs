/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationVersion.cs
*
* 功 能： N / A
* 类 名： SmApplicationVersion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/12/3 16:44:01  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// APP版本(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmApplicationVersionController : BaseController<ISmApplicationVersionServices, SmApplicationVersion, SmApplicationVersionDto, InsertSmApplicationVersionInput, EditSmApplicationVersionInput>
{
    public SmApplicationVersionController(ISmApplicationVersionServices service) : base(service)
    {
    }

    #region 获取最新版本信息
    /// <summary>
    /// 获取最新版本信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("Latest"), AllowAnonymous]
    public async Task<ServiceResult<SmApplicationVersion>> Latest() => await _service.Latest();
    #endregion
}
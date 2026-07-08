/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmMobilePageConfig.cs
*
* 功 能： N / A
* 类 名： SmMobilePageConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/7/8  Claude   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 移动端页面配置(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmMobilePageConfigController : BaseController<ISmMobilePageConfigServices, SmMobilePageConfig, SmMobilePageConfigDto, InsertSmMobilePageConfigInput, EditSmMobilePageConfigInput>
{
    public SmMobilePageConfigController(ISmMobilePageConfigServices service) : base(service)
    {
    }

    #region 发布配置
    /// <summary>
    /// 发布页面配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns></returns>
    [HttpPost("Publish/{id}")]
    public async Task<ServiceResult> Publish(Guid id)
    {
        return await _service.PublishAsync(id);
    }
    #endregion
}

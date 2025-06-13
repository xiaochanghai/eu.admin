/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleSql.cs
*
*功 能： N / A
* 类 名： SmModuleSql
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 17:07:01  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 系统模块SQL(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmModuleSqlController : BaseController<ISmModuleSqlServices, SmModuleSql, SmModuleSqlDto, InsertSmModuleSqlInput, EditSmModuleSqlInput>
{
    public SmModuleSqlController(ISmModuleSqlServices service) : base(service)
    {
    }

    #region 获取模块信息
    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns></returns>
    [HttpGet]
    [Route("ByModuleId/{moduleId}")]
    public async Task<dynamic> GetModuleInfo(Guid moduleId)
    {
        return await _service.GetByModuleId(moduleId);
    }
    #endregion

    #region 获取模块信息
    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns></returns>
    [HttpPost, Route("GetModuleFullSql/{moduleId}")]
    public async Task<ServiceResult<string>> GetModuleFullSql(Guid moduleId)
    {
        return await _service.GetModuleFullSql(moduleId);
    }
    #endregion

}
/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleModule.cs
*
*功 能： N / A
* 类 名： SmRoleModule
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 22:11:38  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 角色模块权限(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmRoleModuleController : BaseController<ISmRoleModuleServices, SmRoleModule, SmRoleModuleDto, InsertSmRoleModuleInput, EditSmRoleModuleInput>
{
    public SmRoleModuleController(ISmRoleModuleServices service) : base(service)
    {
    }

    #region 添加模块权限 
    /// <summary>
    /// 批量导入模块权限
    /// </summary>
    /// <param name="roleModuleVm"></param>
    /// <returns></returns>
    [HttpPost, Route("BatchInsertRoleModule")]
    public async Task<ServiceResult> BatchInsertRoleModule([FromBody] RoleModuleVM roleModuleVm) => await _service.BatchInsertRoleModule(roleModuleVm);

    /// <summary>
    /// 更新角色模块权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="moduleList">模块</param>
    /// <returns></returns>
    [HttpPost("UpdateRoleModule/{roleId}")]
    public async Task<ServiceResult> UpdateRoleModule(Guid roleId, [FromBody] List<string> moduleList) => await _service.UpdateRoleModule(roleId, moduleList);

    #endregion

    #region 获取模块数据
    /// <summary>
    /// 获取模块数据
    /// </summary>
    /// <returns></returns>
    [HttpGet, Route("GetAllModuleList")]
    public async Task<ServiceResult<ModuleTree>> GetAllModuleList() => await _service.GetAllModuleList();
    #endregion

    #region 获取角色模块数据
    /// <summary>
    /// 获取角色模块数据
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    [HttpGet, Route("GetRoleModule/{roleId}")]
    public async Task<ServiceResult<List<string>>> GetRoleModule(Guid roleId) => await _service.GetRoleModule(roleId);
    #endregion
}
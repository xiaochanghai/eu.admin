/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleFunction.cs
*
*功 能： N / A
* 类 名： SmRoleFunction
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/21 1:05:41  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
using EU.Core.Common.Caches;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 功能定义 (Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmRoleFunctionController : BaseController<ISmRoleFunctionServices, SmRoleFunction, SmRoleFunctionDto, InsertSmRoleFunctionInput, EditSmRoleFunctionInput>
{
    RedisCacheService RedisCacheService = new RedisCacheService(1);
    public SmRoleFunctionController(ISmRoleFunctionServices service) : base(service)
    {
    }

    #region 获取模块按钮权限
    /// <summary>
    /// 获取模块按钮权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="moduleId">模块ID</param>
    /// <returns></returns>
    [HttpGet("GetModuleFunction")]
    public async Task<ServiceResult<dynamic>> GetModuleFunction(Guid roleId, Guid moduleId)
    {
        return await _service.GetModuleFunction(roleId, moduleId);
    }
    #endregion

    #region 保存模块按钮权限
    /// <summary>
    /// 保存模块按钮权限
    /// </summary>
    /// <param name="roleFuncVm"></param>
    /// <returns></returns>
    [HttpPut("SaveModuleFunction")]
    public async Task<ServiceResult> SaveModuleFunction([FromBody] RoleFuncVM roleFuncVm)
    {
        return await _service.SaveModuleFunction(roleFuncVm);
    }
    #endregion 

    #region 获取功能权限定义
    /// <summary>
    /// 获取功能权限定义
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAllFuncPriv")]
    public async Task<ServiceResult<DataTree>> GetAllFuncPriv()
    {
        return await _service.GetAllFuncPriv();

    }
    #endregion

    #region 保存功能权限定义
    /// <summary>
    /// 保存功能权限定义
    /// </summary>
    /// <param name="roleFuncPric"></param>
    /// <returns></returns>
    [HttpPost("SaveRoleFuncPriv")]
    public async Task<ServiceResult> SaveRoleFuncPriv([FromBody] RoleFuncPric roleFuncPric)
    {
        return await _service.SaveRoleFuncPriv(roleFuncPric);
    }
    #endregion

    #region 获取角色功能定义
    /// <summary>
    /// 获取角色功能定义
    /// </summary>
    /// <param name="RoleId">角色ID</param>
    /// <returns></returns>
    [HttpGet("GetRoleFuncPriv/{RoleId}")]
    public async Task<ServiceResult<List<Guid?>>> GetRoleFuncPriv(Guid RoleId)
    {
        return await _service.GetRoleFuncPriv(RoleId);
    }
    #endregion
}
/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleDataScopeController.cs
*
* 功 能： 角色数据权限控制器
* 类 名： SmRoleDataScopeController
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/23  EU Team   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：EU Team                              │
*└──────────────────────────────────┘
*/

namespace EU.Core.Api.Controllers;

/// <summary>
/// 角色数据权限 (Controller)
/// 提供数据权限的树形结构查询、权限保存、权限查询等 API
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmRoleDataScopeController : BaseController<ISmRoleDataScopeServices, SmRoleDataScope, SmRoleDataScopeDto, InsertSmRoleDataScopeInput, EditSmRoleDataScopeInput>
{
    public SmRoleDataScopeController(ISmRoleDataScopeServices service) : base(service)
    {
    }

    #region 获取数据权限树

    /// <summary>
    /// 获取所有数据权限树（集团-公司树形结构）
    /// </summary>
    /// <returns>集团列表（每个集团包含下属公司）</returns>
    [HttpGet("GetAllDataScopeTree")]
    public async Task<ServiceResult<List<DataScopeTree>>> GetAllDataScopeTree()
    {
        return await _service.GetAllDataScopeTree();
    }

    #endregion

    #region 更新数据权限

    /// <summary>
    /// 更新角色数据权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="scopeKeys">权限键值列表</param>
    /// <returns>更新结果</returns>
    [HttpPost("UpdateDataScope/{roleId}")]
    public async Task<ServiceResult> UpdateDataScope(Guid roleId, [FromBody] List<Guid> scopeKeys)
    {
        return await _service.UpdateDataScope(roleId, scopeKeys);
    }

    #endregion

    #region 获取角色数据权限

    /// <summary>
    /// 获取角色的数据权限（返回公司ID列表）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>公司 ID 列表</returns>
    [HttpGet("QueryRole/{roleId}")]
    public async Task<ServiceResult<List<Guid>>> GetRoleDataScope(Guid roleId)
    {
        return await _service.GetRoleDataScope(roleId);
    }

    #endregion

    #region 获取用户数据权限

    /// <summary>
    /// 获取用户的实际数据范围（供业务系统调用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户可访问的公司ID列表</returns>
    [HttpGet("GetUserDataScope/{userId}")]
    public async Task<ServiceResult<UserDataScopeModel>> GetUserDataScope(Guid userId)
    {
        return await _service.GetUserDataScope(userId);
    }

    #endregion
}

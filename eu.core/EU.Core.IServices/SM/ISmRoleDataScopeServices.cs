/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleDataScope.cs
*
* 功 能： 角色数据权限
* 类 名： SmRoleDataScope
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

namespace EU.Core.IServices;

/// <summary>
/// 角色数据权限(自定义服务接口)
/// </summary>
public interface ISmRoleDataScopeServices : IBaseServices<SmRoleDataScope, SmRoleDataScopeDto, InsertSmRoleDataScopeInput, EditSmRoleDataScopeInput>
{
    /// <summary>
    /// 获取所有数据权限树（集团-公司树形结构）
    /// </summary>
    /// <returns>集团列表（每个集团包含下属公司）</returns>
    Task<ServiceResult<List<DataScopeTree>>> GetAllDataScopeTree();

    /// <summary>
    /// 更新角色数据权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="scopeKeys">权限键值列表（All / Group_{id} / Company_{id}）</param>
    /// <returns>更新结果</returns>
    Task<ServiceResult> UpdateDataScope(Guid roleId, List<Guid> scopeKeys);

    /// <summary>
    /// 获取角色的数据权限（返回键值列表）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>键值列表（All / Group_{id} / Company_{id}）</returns>
    Task<ServiceResult<List<Guid>>> GetRoleDataScope(Guid roleId);

    /// <summary>
    /// 获取用户的实际数据范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户可访问的公司ID列表</returns>
    Task<ServiceResult<UserDataScopeModel>> GetUserDataScope(Guid userId);
}

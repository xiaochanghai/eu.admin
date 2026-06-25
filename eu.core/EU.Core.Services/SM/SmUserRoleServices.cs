/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUserRole.cs
*
*功 能： N / A
* 类 名： SmUserRole
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:21:42  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 用户角色 (服务)
/// </summary>
public class SmUserRoleServices : BaseServices<SmUserRole, SmUserRoleDto, InsertSmUserRoleInput, EditSmUserRoleInput>, ISmUserRoleServices
{
    #region 常量定义
    private const string AllRolesKey = "All";
    private const string RoleTreeRootTitle = "请选择用户的功能角色";
    #endregion

    private readonly IBaseRepository<SmUserRole> _dal;

    public SmUserRoleServices(IBaseRepository<SmUserRole> dal)
    {
        _dal = dal;
        BaseDal = dal;
    }

    /// <summary>
    /// 查询指定用户的角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户角色列表</returns>
    public async Task<ServiceResult<List<SmUserRole>>> QueryUserRole(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
                return Failed<List<SmUserRole>>("用户ID不能为空");

            var list = await Query(x => x.SmUserId == userId);
            return Success(list, ResponseText.QUERY_SUCCESS);
        }
        catch (Exception ex)
        {
            return Failed<List<SmUserRole>>($"查询用户角色失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 查询所有可用角色，返回树形结构
    /// </summary>
    /// <returns>角色树</returns>
    public async Task<ServiceResult<RoleTree>> QueryRole()
    {
        try
        {
            var children = await Db.Queryable<SmRoles>()
                .Where(x => x.IsActive == true)
                .OrderByDescending(x => x.CreatedTime)
                .Select(x => new RoleTree
                {
                    title = x.RoleName,
                    key = x.ID.ToString().ToLower(),
                    isLeaf = true
                })
                .ToListAsync();

            var roleTree = new RoleTree
            {
                key = AllRolesKey,
                title = RoleTreeRootTitle,
                children = children
            };

            return Success(roleTree, ResponseText.QUERY_SUCCESS);
        }
        catch (Exception ex)
        {
            return Failed<RoleTree>($"查询角色列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量保存用户角色关联关系
    /// </summary>
    /// <param name="userRoleVm">用户角色视图模型</param>
    /// <returns>操作结果</returns>
    public async Task<ServiceResult> BatchInsertUserRole(UserRoleVM userRoleVm)
    {
        // 参数验证
        var validationResult = ValidateUserRoleInput(userRoleVm);
        if (!validationResult.Success)
            return validationResult;

        try
        {
            var userId = userRoleVm.UserId;

            // 转换角色ID列表（排除"All"，并转换为Guid）
            var roleIdList = userRoleVm.RoleList
                .Where(r => r != AllRolesKey)
                .Select(r => Guid.TryParse(r, out var guid) ? guid : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g.Value)
                .ToHashSet();

            // 使用事务确保数据一致性
            await Db.Ado.BeginTranAsync();
            try
            {
                // 查询当前用户的所有有效角色
                var existingUserRoles = await Query(x => x.IsDeleted == false && x.SmUserId == userId && x.SmRoleId != null);
                var existingRoleIds = existingUserRoles.Select(x => x.SmRoleId.Value).ToHashSet();

                // 找出需要删除的角色（存在于数据库但不在新列表中）
                var deleteIds = existingUserRoles
                    .Where(x => !roleIdList.Contains(x.SmRoleId.Value))
                    .Select(x => x.ID)
                    .ToList();

                // 批量标记删除
                if (deleteIds.Any())
                {
                    await Db.Updateable<SmUserRole>()
                        .SetColumns(x => x.IsDeleted == true)
                        .Where(x => deleteIds.Contains(x.ID))
                        .ExecuteCommandAsync();
                }

                // 找出需要新增的角色（在新列表中但不在数据库中）
                var rolesToAdd = roleIdList
                    .Where(roleId => !existingRoleIds.Contains(roleId))
                    .Select(roleId => new SmUserRole
                    {
                        SmRoleId = roleId,
                        SmUserId = userId
                    })
                    .ToList();

                // 批量添加新角色
                if (rolesToAdd.Any())
                {
                    await base.Add(rolesToAdd);
                }

                await Db.Ado.CommitTranAsync();

                // 重新初始化缓存
                await Utility.ReInitCache(Db);

                return Success("用户角色保存成功！");
            }
            catch (Exception ex)
            {
                await Db.Ado.RollbackTranAsync();
                return Failed($"保存用户角色失败: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return Failed($"保存用户角色失败: {ex.Message}");
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 验证用户角色输入参数
    /// </summary>
    /// <param name="userRoleVm">用户角色视图模型</param>
    /// <returns>验证结果</returns>
    private ServiceResult ValidateUserRoleInput(UserRoleVM userRoleVm)
    {
        if (userRoleVm == null)
            return Failed("参数不能为空");

        if (userRoleVm.UserId == Guid.Empty)
            return Failed("用户ID不能为空");

        if (userRoleVm.RoleList == null)
            return Failed("角色列表不能为空");

        return Success();
    }

    #endregion
}
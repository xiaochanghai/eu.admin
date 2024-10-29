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
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 用户角色 (服务)
/// </summary>
public class SmUserRoleServices : BaseServices<SmUserRole, SmUserRoleDto, InsertSmUserRoleInput, EditSmUserRoleInput>, ISmUserRoleServices
{
    private readonly IBaseRepository<SmUserRole> _dal;
    public SmUserRoleServices(IBaseRepository<SmUserRole> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    public async Task<ServiceResult<List<SmUserRole>>> QueryUserRole(Guid userId)
    {
        var list = await Query(x => x.SmUserId == userId);
        return ServiceResult<List<SmUserRole>>.OprateSuccess(list, ResponseText.QUERY_SUCCESS);
    }

    public async Task<ServiceResult<RoleTree>> QueryRole()
    {
        var roleTree = new RoleTree();
        roleTree.key = "All";
        roleTree.title = "请选择用户的功能角色";
        roleTree.children = await Db.Queryable<SmRoles>()
            .OrderByDescending(x => x.CreatedTime)
            .Where(x => x.IsActive == true)
            .Select(x => new RoleTree
            {
                title = x.RoleName,
                key = x.ID.ToString().ToLower(),
                isLeaf = true,
                children = new ()

            }).ToListAsync();

        return ServiceResult<RoleTree>.OprateSuccess(roleTree, ResponseText.QUERY_SUCCESS);
    }

    public async Task<ServiceResult> BatchInsertUserRole(UserRoleVM userRoleVm)
    {
        var roleList = userRoleVm.RoleList;
        var UserId = userRoleVm.UserId;
        if (roleList.Contains("All"))
            roleList.Remove("All");

        var deleteData = await Query(x => x.IsDeleted == false && x.SmUserId == UserId && !roleList.Contains(x.SmRoleId.ToString()));
        for (int i = 0; i < deleteData.Count; i++)
        {
            deleteData[i].IsDeleted = true;
            await base.Update(deleteData[i], ["IsDeleted"], null);
        }

        var data = await Query(x => x.IsDeleted == false && x.SmUserId == UserId && roleList.Contains(x.SmRoleId.ToString()));
        data.ForEach(x =>
        {
            roleList.Remove(x.SmRoleId.ToString());
        });
        var smUserRoles = roleList.Select(x => new SmUserRole() { SmRoleId = Guid.Parse(x.ToString()), SmUserId = UserId }).ToList();
        if (smUserRoles.Any())
            await base.Add(smUserRoles);

        Utility.ReInitCache();
        return ServiceResult.OprateSuccess("用户角色保存成功！");
    }
}
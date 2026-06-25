/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCompany.cs
*
*功 能： N / A
* 类 名： SmCompany
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 16:25:45  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 组织 (服务)
/// </summary>
public class SmCompanyServices : BaseServices<SmCompany, SmCompanyDto, InsertSmCompanyInput, EditSmCompanyInput>, ISmCompanyServices
{
    private readonly IBaseRepository<SmCompany> _dal;
    public SmCompanyServices(IBaseRepository<SmCompany> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    /// <summary>
    /// 删除公司时级联删除数据权限并清除缓存
    /// </summary>
    public override async Task<bool> Delete(Guid[] ids)
    {
        // 先查询受影响的用户（在删除权限记录之前）
        var affectedUserIds = await GetAffectedUserIds(ids);

        var result = await base.Delete(ids);

        if (result)
        {
            // 级联删除权限记录
            await Db.Deleteable<SmRoleDataScope>()
                .Where(x => x.CompanyId != null && ids.Contains(x.CompanyId.Value))
                .ExecuteCommandAsync();

            // 清除相关用户缓存
            ClearAffectedUsersCache(affectedUserIds);

            await Utility.ReInitCache(Db);
        }

        return result;
    }

    /// <summary>
    /// 获取受影响的用户ID列表（在删除权限记录之前调用）
    /// </summary>
    private async Task<List<Guid>> GetAffectedUserIds(Guid[] companyIds)
    {
        try
        {
            var roleIds = await Db.Queryable<SmRoleDataScope>()
                .Where(x => x.CompanyId != null && companyIds.Contains(x.CompanyId.Value))
                .Select(x => x.SmRoleId)
                .Distinct()
                .ToListAsync();

            if (!roleIds.Any()) return new List<Guid>();

            var userIds = await Db.Queryable<SmUserRole>()
                .Where(x => x.SmRoleId != null && roleIds.Contains(x.SmRoleId.Value))
                .Select(x => x.SmUserId.Value)
                .Distinct()
                .ToListAsync();

            return userIds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"查询受影响用户失败: {ex.Message}");
            return new List<Guid>();
        }
    }

    /// <summary>
    /// 清除受影响用户的缓存
    /// </summary>
    private void ClearAffectedUsersCache(List<Guid> userIds)
    {
        try
        {
            foreach (var userId in userIds)
            {
                DataScopeHelper.ClearCache(userId);
            }
        }
        catch (Exception ex)
        {
            // 缓存清除失败不影响主业务流程
            Console.WriteLine($"缓存清除失败: {ex.Message}");
        }
    }
}
/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* DataScopeHelper.cs
*
* 功 能： 数据权限验证工具类
* 类 名： DataScopeHelper
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

using EU.Core.Common.Caches;
using EU.Core.Model;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Helper;

/// <summary>
/// 数据权限验证工具类
/// 提供便捷的数据权限过滤和验证方法
/// </summary>
public class DataScopeHelper
{

    private static RedisCacheService _redisInstance;
    private static RedisCacheService redis => _redisInstance ??= RedisCacheService.Create(1);



    public static async Task<UserDataScopeModel> GetUserDataScope(ISqlSugarClient db, Guid userId)
    {
        try
        {
            //var model = new UserDataScopeModel();

            var cacheKey = $"UserDataScope_{userId}";
            var model = await redis.GetAsync<UserDataScopeModel>(cacheKey);

            if (model != null)
                return model;

            model = new UserDataScopeModel();
            // 1. 获取用户的所有角色
            var userRoles = await db.Queryable<SmUserRole>()
                .Where(x => x.SmUserId == userId)
                .Select(x => x.SmRoleId)
                .ToListAsync();

            // 场景1：用户没有角色 → 返回空列表（拒绝访问）
            if (!userRoles.Any())
                return model;

            // 2. 获取这些角色的数据权限（公司 ID 列表）
            var companyIds = await db.Queryable<SmRoleDataScope>()
                .Where(x => userRoles.Contains(x.SmRoleId))
                .Select(x => x.CompanyId.Value)
                .Distinct()
                .ToListAsync();

            // 场景2：有角色但无数据权限 → 返回空列表（拒绝访问）
            // 场景3：有数据权限 → 返回公司ID列表
            model.CompanyIds = companyIds;
            redis.AddObject(cacheKey, model, TimeSpan.FromDays(1));

            return model;
        }
        catch (Exception ex)
        {
            throw new Exception($"获取用户数据权限失败: {ex.Message}");
        }
    }
    /// <summary>
    /// 构建查询过滤表达式
    /// </summary>
    /// <typeparam name="T">实体类型（必须实现 IDataScopeEntity）</typeparam>
    /// <param name="scope">用户数据范围</param>
    /// <returns>过滤表达式</returns>
    public static System.Linq.Expressions.Expression<Func<T, bool>> BuildFilterExpression<T>(UserDataScopeModel scope)
        where T : class, IDataScopeEntity
    {
        // 如果没有权限，返回 false（拒绝访问）
        if (!scope.CompanyIds.Any())
        {
            return x => false;
        }

        // 有权限，返回 IN 条件
        return x => scope.CompanyIds.Contains(x.CompanyId.Value);
    }

    /// <summary>
    /// 应用数据权限过滤到查询
    /// </summary>
    /// <typeparam name="T">实体类型（必须实现 IDataScopeEntity）</typeparam>
    /// <param name="query">原始查询</param>
    /// <param name="userId">用户ID</param>
    /// <returns>过滤后的查询</returns>
    public async Task<ISugarQueryable<T>> ApplyDataScope<T>(ISqlSugarClient db, ISugarQueryable<T> query, Guid userId)
        where T : class, IDataScopeEntity
    {
        var scope = await GetUserDataScope(db, userId);
        var filter = BuildFilterExpression<T>(scope);
        return query.Where(filter);
    }

    /// <summary>
    /// 检查用户是否有权限访问指定数据
    /// </summary>
    /// <typeparam name="T">实体类型（必须实现 IDataScopeEntity）</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="entity">要检查的实体</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> HasPermission<T>(ISqlSugarClient db, Guid userId, T entity)
        where T : class, IDataScopeEntity
    {
        var scope = await GetUserDataScope(db, userId);

        // 如果没有权限，直接返回 false
        if (!scope.CompanyIds.Any())
        {
            return false;
        }

        // 检查是否在权限列表中
        return scope.CompanyIds.Contains(entity.CompanyId.Value);
    }

    /// <summary>
    /// 清除用户数据范围缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    public static void ClearCache(Guid userId)
    {
        var cacheKey = $"UserDataScope_{userId}";
        RedisCacheService.Create(4).Remove(cacheKey);
    }

    /// <summary>
    /// 批量清除用户数据范围缓存
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    public static void ClearCacheBatch(List<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            ClearCache(userId);
        }
    }
}

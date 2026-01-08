using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Module;

public class FunctionPrivilege
{
    private static RedisCacheService __redis;
    private static RedisCacheService _redis => __redis ??= RedisCacheService.Create(2);
    private const string CacheKey = nameof(CacheKeys.SmFunctionPrivilege);

    #region 获取权限定义
    /// <summary>
    /// 根据模块代码获取权限定义列表
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public static async Task<List<SmFunctionPrivilege>> QueryByModuleCodeAsync(ISqlSugarClient _Db, string moduleCode)
    {
        try
        {
            var cacheData = _redis.Get<List<SmFunctionPrivilege>>(CacheKey, moduleCode);
            if (cacheData != null)
                return cacheData;

            var result = await _Db.Queryable<SmFunctionPrivilege, SmModules>(
                    (a, b) => new JoinQueryInfos(JoinType.Inner, a.SmModuleId == b.ID))
                .Where((a, b) => b.ModuleCode == moduleCode)
                .Select((a, b) => a)
                .ToListAsync();

            _redis.AddObject(CacheKey, moduleCode, result);
            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取单个权限定义
    /// </summary>
    /// <param name="id">权限ID</param>
    /// <returns></returns>
    public static async Task<SmFunctionPrivilege?> QueryByIdAsync(ISqlSugarClient _Db, Guid id)
    {
        try
        {
            var cacheData = _redis.Get<SmFunctionPrivilege>(CacheKey, id.ToString());
            if (cacheData != null)
                return cacheData;

            var result = await _Db.Queryable<SmFunctionPrivilege>()
                .Where(x => x.ID == id)
                .FirstAsync();

            if (result != null)
                _redis.AddObject(CacheKey, id.ToString(), result);

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    /// <summary>
    /// 清除所有权限缓存
    /// </summary>
    public static void ClearCache()
    {
        try
        {
            _redis.Remove(CacheKey);
        }
        catch (Exception)
        {
            throw;
        }
    }
}

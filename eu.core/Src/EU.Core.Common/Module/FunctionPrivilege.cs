using EU.Core.Common;
using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Module;

public class FunctionPrivilege
{
    private static RedisCacheService Redis = new(2);
    private static ISqlSugarClient Db => App.GetService<ISqlSugarClient>(false);
    private static string key = CacheKeys.SmFunctionPrivilege.ToString();

    #region 获取权限定义
    /// <summary>
    /// 获取权限定义
    /// </summary>
    /// <returns></returns>
    public static async Task<List<SmFunctionPrivilege>> Query(string moduleCode)
    {
        var moduleList = Redis.Get<List<SmFunctionPrivilege>>(key, moduleCode);
        if (moduleList == null)
        {
            moduleList = await Db.Queryable<SmFunctionPrivilege, SmModules>((a, b) => new JoinQueryInfos(JoinType.Inner, a.SmModuleId == b.ID && a.IsDeleted == b.IsDeleted))
                .Where((a, b) => a.IsDeleted == false && b.IsDeleted == false && b.ModuleCode == moduleCode)
                .Select((a, b) => a)
                .ToListAsync();
            Redis.AddObject(key, moduleCode, moduleList);
        }
        return moduleList;
    }

    public static async Task<SmFunctionPrivilege> Query(Guid id)
    {
        var moduleList = Redis.Get<List<SmFunctionPrivilege>>(key, id.ToString());
        if (moduleList == null)
        {
            moduleList = await Db.Queryable<SmFunctionPrivilege>()
                .Where(x => x.IsDeleted == false)
                .ToListAsync();
            Redis.AddObject(key, id.ToString(), moduleList);
        }
        return moduleList.Where(x => x.ID == id).FirstOrDefault();
    }
    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    public static void Init()
    {
        Redis.Remove(key);
    }
}

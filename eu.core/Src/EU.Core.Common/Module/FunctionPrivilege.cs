using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Helper;
using EU.Core.Model.Models;

namespace EU.Core.Module;

public class FunctionPrivilege
{
    private static RedisCacheService Redis = new(2);

    #region 获取权限定义
    /// <summary>
    /// 获取权限定义
    /// </summary>
    /// <returns></returns>
    public static List<SmFunctionPrivilege> Query(string moduleCode)
    {
        var moduleList = Redis.Get<List<SmFunctionPrivilege>>(CacheKeys.SmFunctionPrivilege.ToString(), moduleCode);
        if (moduleList == null)
        {
            string sql = $@"SELECT A.*
                                FROM SmFunctionPrivilege A
                                     JOIN SmModules B ON A.SmModuleId = B.ID AND A.IsDeleted = B.IsDeleted
                                WHERE B.ModuleCode = '{moduleCode}' AND A.IsDeleted = 'false'";
            moduleList = DBHelper.QueryList<SmFunctionPrivilege>(sql);
            Redis.AddObject(CacheKeys.SmFunctionPrivilege.ToString(), moduleCode, moduleList);
        }
        return moduleList;
    }

    public static SmFunctionPrivilege Query(Guid id)
    {
        var moduleList = Redis.Get<List<SmFunctionPrivilege>>(CacheKeys.SmFunctionPrivilege.ToString(), id.ToString());
        if (moduleList == null)
        {
            string sql = $@"SELECT A.*
                                FROM SmFunctionPrivilege A 
                                WHERE A.IsDeleted = 'false'";
            moduleList = DBHelper.QueryList<SmFunctionPrivilege>(sql);
            Redis.AddObject(CacheKeys.SmFunctionPrivilege.ToString(), id.ToString(), moduleList);
        }
        return moduleList.Where(x => x.ID == id).FirstOrDefault();
    }
    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    public static void Init()
    {
        Redis.Remove(CacheKeys.SmFunctionPrivilege.ToString());
    }
}

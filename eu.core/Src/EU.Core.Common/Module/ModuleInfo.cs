using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Helper;
using EU.Core.Model.Entity;

namespace EU.Core.Common.Module;

public class ModuleInfo
{
    private static RedisCacheService Redis = new(2);

    #region 获取模块
    /// <summary>
    /// 获取模块
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public static SmModules GetModuleInfo(string moduleCode)
    {
        var module = Redis.Get<SmModules>("SM_MODULE", moduleCode);
        if (module == null)
        {
            var moduleList = GetModuleList();
            module = moduleList.Where(x => x.ModuleCode == moduleCode).FirstOrDefault();

            Redis.Remove("SM_MODULE");
            moduleList.ForEach(item => Redis.AddObject("SM_MODULE", item.ModuleCode, item));
        }
        return module;
    }

    public static List<SmModules> GetLowerModules(string moduleCode)
    {
        var modules = Redis.Get<List<SmModules>>("SM_MODULE_LOWER", moduleCode);
        if (modules == null)
        {
            var module = GetModuleInfo(moduleCode);
            modules = GetModuleList().Where(x => x.BelongModuleId == module.ID).ToList();
            Redis.AddObject("SM_MODULE_LOWER", moduleCode, modules);
        }
        return modules;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static List<SmModules> GetModuleList()
    {
        var code = CacheKeys.SmModule.ToString();
        var moduleList = Redis.Get<List<SmModules>>(code);
        if (moduleList == null)
        {
            string sql = "SELECT A.* FROM SmModules A WHERE A.IsDeleted='false' ORDER BY A.ModuleCode ASC";
            moduleList = DBHelper.QueryList<SmModules>(sql);
            Redis.AddObject(code, moduleList);
        }
        return moduleList;
    }

    public static string GetModuleNameById(Guid? ID)
    {
        string name = string.Empty;
        var moduleList = GetModuleList();
        var module = moduleList.Where(x => x.ID == ID).FirstOrDefault();
        if (module != null)
            name = module.ModuleName;
        return name;
    }
    #endregion

    #region 获取模块是否自动执行查询
    /// <summary>
    /// 获取模块是否自动执行查询
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public static bool? GetIsExecQuery(string moduleCode)
    {
        try
        {
            bool? result = false;
            var Module = GetModuleInfo(moduleCode);
            if (Module != null)
                result = Module.IsExecQuery;
            return result;
        }
        catch (Exception) { throw; }
    }
    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    public static void Init()
    {
        Redis.Remove("SM_MODULE");
        Redis.Remove("SmModules");
        GetModuleList();
        GetModuleInfo("");
    }

    public static string FormatSqlVariable(string sqlString)
    {
        try
        {
            if (sqlString.IndexOf("[CompanyId]") > -1)
                sqlString = sqlString.Replace("[CompanyId]", Utility.GetCompanyId());

            if (sqlString.IndexOf("[QueryGroupId]") > -1)
                sqlString = sqlString.Replace("[QueryGroupId]", Utility.GetGroupId());


            if (sqlString.IndexOf("[UserId]") > -1)
                sqlString = sqlString.Replace("[UserId]", Utility.GetUserIdString());

            //if (sqlString.IndexOf("[UserCode]") > -1)
            //{
            //    sqlString = sqlString.Replace("[UserCode]", UserCode);
            //}

            //if (sqlString.IndexOf("[Language]") > -1)
            //{
            //    sqlString = sqlString.Replace("[Language]", Resource.GetCultureInfoName());
            //}

            //if (sqlString.IndexOf("[EmployeeId]") > -1)
            //{
            //    sqlString = sqlString.Replace("[EmployeeId]", EmployeeId);
            //}

            return sqlString;
        }
        catch (Exception) { throw; }
    }

}

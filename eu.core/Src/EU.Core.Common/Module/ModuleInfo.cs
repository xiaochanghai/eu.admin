using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Helper;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Module;

/// <summary>
/// 模块信息管理类
/// 提供模块信息的缓存管理、查询和格式化等功能
/// </summary>
public class ModuleInfo
{
    #region 私有字段

    /// <summary>
    /// Redis 缓存服务实例（数据库索引: 2）
    /// </summary>
    private static RedisCacheService _redisInstance;
    private static RedisCacheService Redis => _redisInstance ??= RedisCacheService.Create(2);

    private static string key = "SM_MODULE";

    /// <summary>
    /// 数据库上下文
    /// </summary>
    private static ISqlSugarClient Db => App.GetService<ISqlSugarClient>(false);

    #endregion

    #region 模块信息查询

    /// <summary>
    /// 根据模块代码获取模块信息
    /// 优先从缓存读取，缓存不存在时从数据库加载并更新缓存
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>模块信息对象，不存在时返回 null</returns>
    public static SmModules GetModuleInfo(string moduleCode)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
            return null;

        var module = Redis.Get<SmModules>(key, moduleCode);
        if (module == null)
        {
            var moduleList = GetModuleList();
            module = moduleList.FirstOrDefault(x => x.ModuleCode == moduleCode);

            // 后台异步写入 Redis 缓存，避免 sync 操作堆积堵塞请求线程
            _ = WarmUpModuleCacheAsync(moduleList);
        }
        return module;
    }

    private static async Task WarmUpModuleCacheAsync(List<SmModules> moduleList)
    {
        try
        {
            await Redis.RemoveAsync(key);
            for (int i = 0; i < moduleList.Count; i++)
            {
                var item = moduleList[i];
                await Redis.AddObjectAsync(key, item.ModuleCode, item);
            }
        }
        catch { /* 缓存写入失败不影响主流程 */ }
    }

    /// <summary>
    /// 获取指定模块的所有下级模块
    /// </summary>
    /// <param name="moduleCode">父模块代码</param>
    /// <returns>下级模块列表</returns>
    public static List<SmModules> GetLowerModules(string moduleCode)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
            return new List<SmModules>();

        var cacheKey = $"SM_MODULE_LOWER_{moduleCode}";
        var modules = Redis.Get<List<SmModules>>("SM_MODULE_LOWER", moduleCode);

        if (modules == null)
        {
            var module = GetModuleInfo(moduleCode);
            if (module != null)
            {
                modules = GetModuleList().Where(x => x.BelongModuleId == module.ID).ToList();
            }
            else
            {
                modules = new List<SmModules>();
            }
            Redis.AddObject("SM_MODULE_LOWER", moduleCode, modules);
        }

        return modules;
    }

    /// <summary>
    /// 获取所有模块列表
    /// 优先从缓存读取，缓存不存在时从数据库加载
    /// </summary>
    /// <returns>模块列表，按模块代码排序</returns>
    public static List<SmModules> GetModuleList()
    {
        var code = CacheKeys.SmModule.ToString();
        var moduleList = Redis.Get<List<SmModules>>(code);

        if (moduleList == null)
        {
            moduleList = Db.Queryable<SmModules>()
                .OrderBy(x => x.ModuleCode)
                .ToList();
            Redis.AddObject(code, moduleList);
        }

        return moduleList ?? new List<SmModules>();
    }

    /// <summary>
    /// 根据模块 ID 获取模块名称
    /// </summary>
    /// <param name="ID">模块 ID</param>
    /// <returns>模块名称，不存在时返回空字符串</returns>
    public static string GetModuleNameById(Guid? ID)
    {
        if (ID == null || ID == Guid.Empty)
            return string.Empty;

        var moduleList = GetModuleList();
        var module = moduleList.FirstOrDefault(x => x.ID == ID);

        return module?.ModuleName ?? string.Empty;
    }

    /// <summary>
    /// 根据模块 ID 获取模块代码
    /// </summary>
    /// <param name="ID">模块 ID</param>
    /// <returns>模块代码，不存在时返回空字符串</returns>
    public static string GetModuleCodeById(Guid? ID)
    {
        if (ID == null || ID == Guid.Empty)
            return string.Empty;

        var moduleList = GetModuleList();
        var module = moduleList.FirstOrDefault(x => x.ID == ID);

        return module?.ModuleCode ?? string.Empty;
    }

    #endregion

    #region 模块配置查询

    /// <summary>
    /// 获取模块是否自动执行查询的配置
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>是否自动执行查询，模块不存在时返回 false</returns>
    public static bool? GetIsExecQuery(string moduleCode)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
            return false;

        var module = GetModuleInfo(moduleCode);
        return module?.IsExecQuery ?? false;
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 初始化模块缓存
    /// 清除现有缓存并重新加载所有模块数据
    /// </summary>
    public static void Init()
    {
        var code = CacheKeys.SmModule.ToString();

        // 清除所有模块相关缓存
        Redis.Remove("SM_MODULE");
        Redis.Remove(code);

        // 重新加载模块列表到缓存
        GetModuleList();
    }

    /// <summary>
    /// 清除所有模块缓存
    /// </summary>
    public static void ClearCache()
    {
        var code = CacheKeys.SmModule.ToString();
        Redis.Remove("SM_MODULE");
        Redis.Remove("SM_MODULE_LOWER");
        Redis.Remove(code);
    }

    #endregion

    #region SQL 变量格式化

    /// <summary>
    /// 格式化 SQL 字符串中的变量占位符
    /// 将特定的占位符替换为实际的用户上下文值
    /// </summary>
    /// <param name="sqlString">包含占位符的 SQL 字符串</param>
    /// <returns>格式化后的 SQL 字符串</returns>
    /// <remarks>
    /// 支持的占位符：
    /// - [CompanyId]: 当前公司 ID
    /// - [QueryGroupId]: 当前查询组 ID
    /// - [UserId]: 当前用户 ID
    /// </remarks>
    public static string FormatSqlVariable(string sqlString)
    {
        if (string.IsNullOrWhiteSpace(sqlString))
            return sqlString;

        // 替换公司 ID
        if (sqlString.Contains("[CompanyId]"))
            sqlString = sqlString.Replace("[CompanyId]", Utility.GetCompanyId());

        // 替换查询组 ID
        if (sqlString.Contains("[QueryGroupId]"))
            sqlString = sqlString.Replace("[QueryGroupId]", Utility.GetGroupId());

        // 替换用户 ID
        if (sqlString.Contains("[UserId]"))
            sqlString = sqlString.Replace("[UserId]", Utility.GetUserIdString());

        return sqlString;
    }

    #endregion
}

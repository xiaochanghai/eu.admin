using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Models;

namespace EU.Core.Common.Helper;

/// <summary>
/// 系统参数方法类
/// </summary>
public class ConfigCache
{
    public static RedisCacheService redis = new(3);

    /// <summary>
    /// 获取参数值
    /// </summary>
    /// <param name="key">参数key</param>
    /// <returns></returns>
    public static string GetValue(string key)
    {
        var value = redis.Get<SmConfig>(CacheKeys.SmConfig.ToString(), key);
        if (value == null)
        {
            Init();
            value = new RedisCacheService(3).Get<SmConfig>(CacheKeys.SmConfig.ToString(), key);
            return value?.ConfigValue;
        }
        else
            return value.ConfigValue;
    }

    //public static SmConfig GetSmConfig(string code)
    //{
    //    SmConfig cache = new RedisCacheService(2).Get<SmConfig>(CacheKeys.SmConfig.ToString(), code);
    //    if (cache == null)
    //    {
    //        Init();
    //        cache = new RedisCacheService(2).Get<SmConfig>(CacheKeys.SmConfig.ToString(), code);
    //    }
    //    return cache;
    //}

    /// <summary>
    /// 初始化系统参数
    /// </summary>
    public static void Init()
    {
        redis.Remove(CacheKeys.SmConfig.ToString());

        string sql = "SELECT * FROM SmConfig WHERE IsActive='true' AND IsDeleted='false'";
        var list = DBHelper.QueryList<SmConfig>(sql);
        foreach (var item in list)
            Add(item.ConfigCode, item);
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="key">参数key</param>
    /// <param name="item">参数实体</param>
    public static void Add(string key, SmConfig item = null)
    {
        redis.AddObject(CacheKeys.SmConfig.ToString(), key, item);
    }
}

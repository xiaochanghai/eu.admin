using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Helper;

/// <summary>
/// 系统配置参数缓存帮助类
/// 提供系统配置参数的缓存管理功能，支持异步操作和线程安全的初始化
/// </summary>
public class ConfigCache
{
    #region 私有字段

    /// <summary>
    /// Redis 缓存服务实例（数据库索引: 3）
    /// </summary>
    private static RedisCacheService _redisInstance;
    private static RedisCacheService redis => _redisInstance ??= RedisCacheService.Create(3);

    /// <summary>
    /// 配置参数在 Redis 中的缓存键
    /// </summary>
    private static readonly string cacheKey = CacheKeys.SmConfig.ToString();

    /// <summary>
    /// 初始化操作的线程同步锁对象
    /// </summary>
    private static readonly SemaphoreSlim initLock = new(1, 1);

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取指定配置参数的值
    /// 如果缓存中不存在该配置，会自动触发初始化操作
    /// </summary>
    /// <param name="db">数据库上下文</param>
    /// <param name="key">配置参数的键值</param>
    /// <returns>配置参数的值，如果不存在返回 null</returns>
    /// <exception cref="ArgumentNullException">当 db 或 key 为 null 时抛出</exception>
    public static async Task<string> GetValueAsync(ISqlSugarClient db, string key)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        // 首次尝试从缓存获取
        var value = await redis.GetAsync<SmConfig>(cacheKey, key);

        // 未命中时检查 Redis 缓存状态，缓存键不存在则重新加载。
        if (value == null)
        {
            await InitAsync(db);
            value = await redis.GetAsync<SmConfig>(cacheKey, key);
        }

        return value?.ConfigValue;
    }

    /// <summary>
    /// 批量获取配置参数值
    /// </summary>
    /// <param name="db">数据库上下文</param>
    /// <param name="keys">配置参数键值集合</param>
    /// <returns>键值对字典，键为配置代码，值为配置值</returns>
    public static async Task<Dictionary<string, string>> GetValuesAsync(ISqlSugarClient db, params string[] keys)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));
        if (keys == null || keys.Length == 0)
            return new Dictionary<string, string>();

        // 根据 Redis 缓存键是否存在决定是否加载。
        await InitAsync(db);

        var result = new Dictionary<string, string>(keys.Length);
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                var config = await redis.GetAsync<SmConfig>(cacheKey, key);
                result[key] = config?.ConfigValue;
            }
        }

        return result;
    }

    /// <summary>
    /// 初始化系统配置参数缓存
    /// 根据 Redis 缓存键判断是否需要加载，使用 SemaphoreSlim 串行化当前进程内的初始化操作。
    /// </summary>
    /// <param name="db">数据库上下文</param>
    /// <param name="forceRefresh">是否强制刷新缓存，默认为 false</param>
    /// <returns>加载的配置项数量；缓存键已存在且未强制刷新时返回 0。</returns>
    /// <exception cref="ArgumentNullException">当 db 为 null 时抛出</exception>
    public static async Task<int> InitAsync(ISqlSugarClient db, bool forceRefresh = false)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));

        // 缓存状态以 Redis 为准，避免本地标记与实际缓存不一致。
        if (!forceRefresh && await redis.ExistsAsync(cacheKey))
            return 0;

        await initLock.WaitAsync();
        try
        {
            // 再次检查，避免重复初始化
            if (!forceRefresh && await redis.ExistsAsync(cacheKey))
                return 0;

            // 清除旧缓存
            redis.Remove(cacheKey);

            // 从数据库加载所有配置
            var list = await db.Queryable<SmConfig>().ToListAsync();

            // 批量添加到缓存
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (!string.IsNullOrWhiteSpace(item.ConfigCode))
                    {
                        redis.AddObject(cacheKey, item.ConfigCode, item);
                    }
                }
            }

            return list?.Count ?? 0;
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <summary>
    /// 添加或更新配置参数到缓存
    /// </summary>
    /// <param name="key">配置参数的键值</param>
    /// <param name="item">配置参数实体对象，为 null 时表示删除该配置</param>
    /// <exception cref="ArgumentNullException">当 key 为 null 或空白时抛出</exception>
    public static void Add(string key, SmConfig item = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        redis.AddObject(cacheKey, key, item);
    }

    /// <summary>
    /// 从缓存中移除指定的配置参数
    /// </summary>
    /// <param name="key">配置参数的键值</param>
    public static async Task Remove(string key)
    {
        if (key.IsNotEmptyOrNull())
            await redis.RemoveObject(cacheKey, key);
    }

    /// <summary>
    /// 清空所有配置参数缓存
    /// </summary>
    public static void Clear()
    {
        redis.Remove(cacheKey);
    }

    #endregion
}

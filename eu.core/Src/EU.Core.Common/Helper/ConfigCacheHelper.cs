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
    private static readonly RedisCacheService redis = new(3);

    /// <summary>
    /// 配置参数在 Redis 中的缓存键
    /// </summary>
    private static readonly string cacheKey = CacheKeys.SmConfig.ToString();

    /// <summary>
    /// 初始化操作的线程同步锁对象
    /// </summary>
    private static readonly SemaphoreSlim initLock = new(1, 1);

    /// <summary>
    /// 标识配置是否已初始化
    /// </summary>
    private static volatile bool isInitialized = false;

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

        // 如果未找到且缓存未初始化，则执行初始化
        if (value == null && !isInitialized)
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

        // 确保缓存已初始化
        if (!isInitialized)
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
    /// 使用 SemaphoreSlim 实现线程安全的双检锁模式，确保在高并发场景下只初始化一次
    /// </summary>
    /// <param name="db">数据库上下文</param>
    /// <param name="forceRefresh">是否强制刷新缓存，默认为 false</param>
    /// <returns>初始化的配置项数量</returns>
    /// <exception cref="ArgumentNullException">当 db 为 null 时抛出</exception>
    public static async Task<int> InitAsync(ISqlSugarClient db, bool forceRefresh = false)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));

        // 双检锁：如果已初始化且非强制刷新，直接返回
        if (isInitialized && !forceRefresh)
            return 0;

        await initLock.WaitAsync();
        try
        {
            // 再次检查，避免重复初始化
            if (isInitialized && !forceRefresh)
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

            // 标记为已初始化
            isInitialized = true;

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
        isInitialized = false;
    }

    #endregion
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EU.Core.Common.Caches;

/// <summary>
/// Redis缓存服务：提供Redis数据存储和操作功能
/// </summary>
public class RedisCacheService : IDisposable
{
    #region 私有字段

    /// <summary>
    /// Redis数据库实例
    /// </summary>
    protected IDatabase _cache;

    /// <summary>
    /// Redis连接多路复用器
    /// </summary>
    private readonly IConnectionMultiplexer _connection;

    /// <summary>
    /// Redis实例名称
    /// </summary>
    private readonly string _instance;

    /// <summary>
    /// Redis数据库编号
    /// </summary>
    private readonly int _num = 0;

    /// <summary>
    /// Redis键前缀
    /// </summary>
    private readonly string _redisKeyPrefix;

    /// <summary>
    /// 内存缓存实例，用于本地缓存
    /// </summary>
    public static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化Redis缓存服务（通过依赖注入）
    /// </summary>
    /// <param name="connection">Redis连接多路复用器</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="num">数据库编号：0：默认，1：用户左侧菜单，2：模块信息相关，3：系统参数相关，4：用户信息，5：SignalR 数据</param>
    public RedisCacheService(IConnectionMultiplexer connection, IConfiguration configuration, int num = 0)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _num = num;
        _cache = _connection.GetDatabase(_num);
        _redisKeyPrefix = configuration["Redis:InstanceName"] ?? "nc";
        _instance = "nc";
    }

    /// <summary>
    /// 创建 RedisCacheService 实例（从服务定位器获取依赖）
    /// 注意：建议优先使用构造函数注入，此方法用于向后兼容
    /// </summary>
    /// <param name="num">数据库编号</param>
    /// <returns>RedisCacheService 实例</returns>
    public static RedisCacheService Create(int num = 0)
    {
        var connection = App.GetService<IConnectionMultiplexer>();
        var configuration = App.GetService<IConfiguration>();
        return new RedisCacheService(connection, configuration, num);
    }

    #endregion

    #region 基础操作

    /// <summary>
    /// 清空当前数据库中的所有数据
    /// </summary>
    public void Clear()
    {
        if (_connection != null && Ping())
        {
            var endpoints = _connection.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = _connection.GetServer(endpoint);
                server.FlushDatabase(_num);
            }
        }
    }

    /// <summary>
    /// 检测Redis服务器连接状态
    /// </summary>
    /// <returns>连接是否正常</returns>
    public bool Ping()
    {
        try
        {
            var endpoints = _connection.GetEndPoints(true);
            if (endpoints.Length == 0)
                return false;

            IServer server = _connection.GetServer(endpoints[0]);
            var pingTime = server.Ping();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 异步检测Redis服务器连接状态
    /// </summary>
    /// <returns>连接是否正常</returns>
    public async Task<bool> PingAsync()
    {
        try
        {
            var endpoints = _connection.GetEndPoints(true);
            if (endpoints.Length == 0)
                return false;

            IServer server = _connection.GetServer(endpoints[0]);
            var pingTime = await server.PingAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取带实例前缀的Redis键
    /// </summary>
    /// <param name="key">原始键</param>
    /// <returns>带前缀的键</returns>
    public string GetKeyForRedis(string key)
    {
        return _instance + key;
    }

    #endregion

    #region 键操作

    /// <summary>
    /// 验证缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public bool Exists(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        return _cache.KeyExists(key);

    }

    /// <summary>
    /// 异步验证缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        return await _cache.KeyExistsAsync(key);
    }

    //public List<string> Exists1(string key)
    //{
    //    if (string.IsNullOrEmpty(key))
    //        throw new ArgumentNullException(nameof(key));

    //    var keys = _cache.keyex
    //    return keys.Select(x => x.ObjToString()).ToList();

    //}

    #endregion

    #region 列表操作

    /// <summary>
    /// 向列表左侧添加值
    /// </summary>
    /// <param name="key">列表键</param>
    /// <param name="val">值</param>
    public void ListLeftPush(string key, string val)
    {
        key = _redisKeyPrefix + key;
        _cache.ListLeftPush(key, val);
    }

    /// <summary>
    /// 向列表右侧添加值
    /// </summary>
    /// <param name="key">列表键</param>
    /// <param name="val">值</param>
    public void ListRightPush(string key, string val)
    {
        key = _redisKeyPrefix + key;
        _cache.ListRightPush(key, val);
    }

    /// <summary>
    /// 从列表右侧弹出值并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">列表键</param>
    /// <returns>反序列化后的对象</returns>
    public T ListDequeue<T>(string key) where T : class
    {
        key = _redisKeyPrefix + key;
        RedisValue redisValue = _cache.ListRightPop(key);
        if (!redisValue.HasValue)
            return null;
        return JsonConvert.DeserializeObject<T>(redisValue);
    }

    /// <summary>
    /// 从列表右侧弹出值
    /// </summary>
    /// <param name="key">列表键</param>
    /// <returns>弹出的值</returns>
    public object ListDequeue(string key)
    {
        key = _redisKeyPrefix + key;
        RedisValue redisValue = _cache.ListRightPop(key);
        if (!redisValue.HasValue)
            return null;
        return redisValue;
    }

    /// <summary>
    /// 移除列表中的数据
    /// </summary>
    /// <param name="key">列表键</param>
    /// <param name="keepIndex">保留的起始索引（保留从该索引到最后的元素）</param>
    public void ListRemove(string key, int keepIndex)
    {
        key = _redisKeyPrefix + key;
        _cache.ListTrim(key, keepIndex, -1);
    }

    #endregion

    #region 删除操作

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否删除成功</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public bool Remove(string key)
    {
        if (key.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        return _cache.KeyDelete(key);
    }

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否删除成功</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public async Task<bool> RemoveAsync(string key)
    {
        if (key.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        return await _cache.KeyDeleteAsync(key);
    }

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<bool> RemoveObject(string key, string hashField)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        return await _cache.HashDeleteAsync(key, hashField);
    }

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存键（Guid类型）</param>
    /// <returns>是否删除成功</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public bool Remove(Guid? key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        var key1 = key.ObjToString();
        return Remove(key1);
    }

    /// <summary>
    /// 批量删除缓存
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <exception cref="ArgumentNullException">键集合为空时抛出异常</exception>
    public void RemoveAll(IEnumerable<string> keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        foreach (var item in keys)
        {
            Remove(_redisKeyPrefix + item);
        }
    }

    #endregion

    #region 获取操作

    /// <summary>
    /// 获取缓存并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值</returns>
    public T Get<T>(string key) where T : class
    {
        key = _redisKeyPrefix + key;
        var value = _cache.StringGet(key);

        if (!value.HasValue)
            return null;

        return JsonConvert.DeserializeObject<T>(value);
    }

    /// <summary>
    /// 获取缓存并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">缓存键（Guid类型）</param>
    /// <returns>缓存值</returns>
    public T Get<T>(Guid? key) where T : class
    {
        if (key is null)
            return null;

        return Get<T>(key.ObjToString());
    }

    /// <summary>
    /// 获取字符串类型的缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值</returns>
    public string Get(string key) => _cache.StringGet(_redisKeyPrefix + key).ToString();

    /// <summary>
    /// 异步获取缓存并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值</returns>
    public async Task<T> GetAsync<T>(string key) where T : class
    {
        key = _redisKeyPrefix + key;
        var value = await _cache.StringGetAsync(key);

        if (!value.HasValue)
            return null;

        return JsonConvert.DeserializeObject<T>(value);
    }

    /// <summary>
    /// 异步获取缓存并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">缓存键（Guid类型）</param>
    /// <returns>缓存值</returns>
    public async Task<T> GetAsync<T>(Guid? key) where T : class
    {
        if (key is null)
            return null;

        return await GetAsync<T>(key.ObjToString());
    }

    /// <summary>
    /// 获取缓存集合
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <returns>键值对字典</returns>
    public IDictionary<string, object> GetAll(IEnumerable<string> keys)
    {
        var dict = new Dictionary<string, object>();
        foreach (var item in keys)
        {
            dict.Add(item, Get(_redisKeyPrefix + item));
        }
        return dict;
    }

    #endregion

    #region 修改操作

    /// <summary>
    /// 修改缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">新的缓存值</param>
    /// <returns>是否修改成功</returns>
    /// <exception cref="ArgumentNullException">键为空或Redis连接失败时抛出异常</exception>
    public bool Replace(string key, object value)
    {
        if (string.IsNullOrEmpty(key) || !Ping())
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Exists(key) && !Remove(key))
            return false;

        return AddObject(key, value);
    }

    /// <summary>
    /// 修改缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">新的缓存值</param>
    /// <param name="expiresSliding">滑动过期时长</param>
    /// <param name="expiressAbsoulte">绝对过期时长</param>
    /// <returns>是否修改成功</returns>
    /// <exception cref="ArgumentNullException">键为空或Redis连接失败时抛出异常</exception>
    public bool Replace(string key, object value, TimeSpan expiresSliding, TimeSpan expiressAbsoulte)
    {
        if (string.IsNullOrEmpty(key) || !Ping())
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Exists(key) && !Remove(key))
            return false;

        if (value is string stringValue)
        {
            return Add(key, stringValue, expiresSliding);
        }
        return AddObject(key, value, expiresSliding);
    }

    /// <summary>
    /// 修改缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">新的缓存值</param>
    /// <param name="expiresIn">缓存时长</param>
    /// <param name="isSliding">是否滑动过期</param>
    /// <returns>是否修改成功</returns>
    /// <exception cref="ArgumentNullException">键为空或Redis连接失败时抛出异常</exception>
    public bool Replace(string key, object value, TimeSpan expiresIn, bool isSliding = false)
    {
        if (string.IsNullOrEmpty(key) || !Ping())
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Exists(key) && !Remove(key))
            return false;

        if (value is string stringValue)
        {
            return Add(key, stringValue);
        }
        return AddObject(key, value);
    }

    #endregion

    #region 添加操作

    /// <summary>
    /// 添加对象缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiresIn">过期时间</param>
    /// <param name="isSliding">是否滑动过期</param>
    /// <returns>是否添加成功</returns>
    public bool AddObject(string key, object value, TimeSpan? expiresIn = null, bool isSliding = false)
    {
        return _cache.StringSet(_redisKeyPrefix + key, JsonConvert.SerializeObject(value), expiresIn, false);
    }

    /// <summary>
    /// 添加对象缓存
    /// </summary>
    /// <param name="key">缓存键（Guid类型）</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiresIn">过期时间</param>
    /// <param name="isSliding">是否滑动过期</param>
    /// <returns>是否添加成功</returns>
    public bool AddObject(Guid? key, object value, TimeSpan? expiresIn = null, bool isSliding = false)
    {
        if (key is null)
            return false;
        return AddObject(key.ObjToString(), value, expiresIn, isSliding);
    }

    /// <summary>
    /// 添加字符串缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiresIn">过期时间</param>
    /// <param name="isSliding">是否滑动过期（Redis中无效）</param>
    /// <returns>是否添加成功</returns>
    public bool Add(string key, string value, TimeSpan? expiresIn = null, bool isSliding = false)
    {
        return _cache.StringSet(_redisKeyPrefix + key, value, expiresIn, false);
    }

    public bool Add(Guid key, string value, TimeSpan? expiresIn = null, bool isSliding = false) => Add(key.ObjToString(), value, expiresIn, isSliding);

    #endregion

    #region 哈希表操作

    /// <summary>
    /// 在哈希表中设置字段值
    /// </summary>
    /// <param name="key">哈希表键</param>
    /// <param name="hashField">字段名</param>
    /// <param name="value">字段值（对象）</param>
    /// <returns>是否设置成功</returns>
    public bool AddObject(string key, string hashField, object value)
    {
        return Add(key, hashField, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 异步在哈希表中设置字段值
    /// </summary>
    /// <param name="key">哈希表键</param>
    /// <param name="hashField">字段名</param>
    /// <param name="value">字段值（对象）</param>
    /// <returns>是否设置成功</returns>
    public async Task<bool> AddObjectAsync(string key, string hashField, object value)
    {
        return await AddAsync(key, hashField, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 在哈希表中设置字段值
    /// </summary>
    /// <param name="key">哈希表键</param>
    /// <param name="hashField">字段名</param>
    /// <param name="value">字段值（字符串）</param>
    /// <returns>是否设置成功</returns>
    public bool Add(string key, string hashField, string value)
    {
        return _cache.HashSet(_redisKeyPrefix + key, hashField, value);
    }

    /// <summary>
    /// 异步在哈希表中设置字段值
    /// </summary>
    /// <param name="key">哈希表键</param>
    /// <param name="hashField">字段名</param>
    /// <param name="value">字段值（字符串）</param>
    /// <returns>是否设置成功</returns>
    public async Task<bool> AddAsync(string key, string hashField, string value)
    {
        return await _cache.HashSetAsync(_redisKeyPrefix + key, hashField, value);
    }

    /// <summary>
    /// 获取哈希表中的字段值并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">哈希表键</param>
    /// <param name="hashField">字段名</param>
    /// <returns>字段值</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public T Get<T>(string key, string hashField) where T : class
    {
        if (key.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        var value = _cache.HashGet(key, hashField);

        if (!value.HasValue)
            return null;
        return JsonConvert.DeserializeObject<T>(value);
    }

    /// <summary>
    /// 异步获取哈希表中的字段值并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">哈希表键</param>
    /// <param name="hashField">字段名</param>
    /// <returns>字段值</returns>
    /// <exception cref="ArgumentNullException">键为空时抛出异常</exception>
    public async Task<T> GetAsync<T>(string key, string hashField) where T : class
    {
        if (key.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(key));
        key = _redisKeyPrefix + key;
        var value = await _cache.HashGetAsync(key, hashField);

        if (!value.HasValue)
            return null;
        return JsonConvert.DeserializeObject<T>(value);
    }

    #endregion

    #region 资源释放

    /// <summary>
    /// 释放资源
    /// 注意：IConnectionMultiplexer 由 DI 容器管理，不在此处释放
    /// </summary>
    public void Dispose()
    {
        // 连接由 DI 容器管理，不需要在此处释放
        GC.SuppressFinalize(this);
    }

    #endregion
}

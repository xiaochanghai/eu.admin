using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EU.Core.Common.Caches;

/// <summary>
/// Redis操作类
/// </summary>
public class RedisCacheService
{
    protected IDatabase _cache;

    private ConnectionMultiplexer _connection;
    //private readonly RedisOptions cacheOptions = App.GetOptions<RedisOptions>();

    private readonly string _instance;
    private readonly int _num = 0;
    private readonly string _connectionString = AppSettings.app(["Redis", "ConnectionString"]).ToString();
    private readonly string _redisKeyPrefix = AppSettings.app(["Redis", "InstanceName"]).ToString();
    public static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// 1：用户左侧菜单，2：模块信息相关，3：系统参数相关，4：用户信息，5：SignalR 数据
    /// </summary>
    /// <param name="num"></param>
    public RedisCacheService(int num = 0)
    {
        _connection = ConnectionMultiplexer.Connect(_connectionString);
        _num = num;
        _cache = _connection.GetDatabase(_num);
        _instance = "nc";
    }

    public void Clear()
    {
        if (_connection != null)
        {
            if (Ping())
            {
                var endpoints = _connection.GetEndPoints(true);
                foreach (var endpoint in endpoints)
                {
                    var server = _connection.GetServer(endpoint);
                    server.FlushDatabase(_num);
                }
            }
        }
    }
    public bool Ping()
    {
        try
        {
            string hostAndPort = _connectionString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
            IServer server = _connection.GetServer(hostAndPort);
            var pingTime = server.Ping();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<bool> PingAsync()
    {
        try
        {
            string hostAndPort = _connectionString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
            IServer server = _connection.GetServer(hostAndPort);
            var pingTime = await server.PingAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public string GetKeyForRedis(string key)
    {
        return _instance + key;
    }
    /// <summary>
    /// 验证缓存项是否存在
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <returns></returns>
    public bool Exists(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        return _cache.KeyExists(key);
    }

    public void ListLeftPush(string key, string val)
    {
        key = _redisKeyPrefix + key;
        _cache.ListLeftPush(key, val);
    }

    public void ListRightPush(string key, string val)
    {
        key = _redisKeyPrefix + key;
        _cache.ListRightPush(key, val);
    }


    public T ListDequeue<T>(string key) where T : class
    {
        key = _redisKeyPrefix + key;
        RedisValue redisValue = _cache.ListRightPop(key);
        if (!redisValue.HasValue)
            return null;
        return JsonConvert.DeserializeObject<T>(redisValue);
    }
    public object ListDequeue(string key)
    {
        key = _redisKeyPrefix + key;
        RedisValue redisValue = _cache.ListRightPop(key);
        if (!redisValue.HasValue)
            return null;
        return redisValue;
    }

    /// <summary>
    /// 移除list中的数据，keepIndex为保留的位置到最后一个元素如list 元素为1.2.3.....100
    /// 需要移除前3个数，keepindex应该为4
    /// </summary>
    /// <param name="key"></param>
    /// <param name="keepIndex"></param>
    public void ListRemove(string key, int keepIndex)
    {
        key = _redisKeyPrefix + key;
        _cache.ListTrim(key, keepIndex, -1);
    }

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <returns></returns>
    public bool Remove(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        key = _redisKeyPrefix + key;
        return _cache.KeyDelete(key);
    }
    /// <summary>
    /// 批量删除缓存
    /// </summary>
    /// <param name="key">缓存Key集合</param>
    /// <returns></returns>
    public void RemoveAll(IEnumerable<string> keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        keys.ToList().ForEach(item => Remove(_redisKeyPrefix + item));
    }
    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <returns></returns>
    public T Get<T>(string key) where T : class
    {

        key = _redisKeyPrefix + key;
        var value = _cache.StringGet(key);

        if (!value.HasValue)
            return null;

        return JsonConvert.DeserializeObject<T>(value);

    }
    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <returns></returns>
    public string Get(string key)
    {
        return _cache.StringGet(_redisKeyPrefix + key).ToString();
    }
    /// <summary>
    /// 获取缓存集合
    /// </summary>
    /// <param name="keys">缓存Key集合</param>
    /// <returns></returns>
    public IDictionary<string, object> GetAll(IEnumerable<string> keys)
    {
        var dict = new Dictionary<string, object>();
        keys.ToList().ForEach(item => dict.Add(item, Get(_redisKeyPrefix + item)));
        return dict;
    }

    ///  return JsonConvert.DeserializeObject(value);
    /// <summary>
    /// 修改缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="value">新的缓存Value</param>
    /// <returns></returns>
    public bool Replace(string key, object value)
    {
        if (key == null || !Ping())
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Exists(key))
            if (!Remove(key))
                return false;

        return AddObject(key, value);

    }
    /// <summary>
    /// 修改缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="value">新的缓存Value</param>
    /// <param name="expiresSliding">滑动过期时长（如果在过期时间内有操作，则以当前时间点延长过期时间）</param>
    /// <param name="expiressAbsoulte">绝对过期时长</param>
    /// <returns></returns>
    public bool Replace(string key, object value, TimeSpan expiresSliding, TimeSpan expiressAbsoulte)
    {
        if (key == null || !Ping())
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Exists(key))
            if (!Remove(key))
                return false;
        if (value.GetType().Name == "String")
        {
            return Add(key, value.ToString(), expiresSliding);
        }
        return AddObject(key, value, expiresSliding);


    }
    /// <summary>
    /// 修改缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="value">新的缓存Value</param>
    /// <param name="expiresIn">缓存时长</param>
    /// <param name="isSliding">是否滑动过期（如果在过期时间内有操作，则以当前时间点延长过期时间）</param>
    /// <returns></returns>
    public bool Replace(string key, object value, TimeSpan expiresIn, bool isSliding = false)
    {
        if (key == null || !Ping())
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Exists(key))
            if (!Remove(key)) return false;
        if (value.GetType().Name == "String")
        {
            return Add(key, value.ToString());
        }
        return AddObject(key, value);

    }
    public void Dispose()
    {
        if (_connection != null)
            _connection.Dispose();
        GC.SuppressFinalize(this);
    }
    public bool AddObject(string key, object value, TimeSpan? expiresIn = null, bool isSliding = false)
    {
        return _cache.StringSet(_redisKeyPrefix + key, JsonConvert.SerializeObject(value), expiresIn);

    }

    /// <summary>
    /// 添加缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="value">缓存Value</param>
    /// <param name="expiresIn">缓存时长</param>
    /// <param name="isSliding">是否滑动过期（如果在过期时间内有操作，则以当前时间点延长过期时间,Redis中无效）</param>
    /// <returns></returns>
    public bool Add(string key, string value, TimeSpan? expiresIn = null, bool isSliding = false)
    {
        return _cache.StringSet(_redisKeyPrefix + key, value, expiresIn);

    }

    /// <summary>
    /// 在键到值存储的散列中设置字段。如果密钥不存在，则创建一个包含散列的新密钥。如果字段在散列中已经存在，则会覆盖它。
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="hashField">键值</param>
    /// <param name="value">缓存Value</param>
    /// <returns></returns>
    public bool AddObject(string key, string hashField, object value)
    {
        return Add(key, hashField, JsonConvert.SerializeObject(value));
    }
    public async Task<bool> AddObjectAsync(string key, string hashField, object value)
    {
        return await AddAsync(key, hashField, JsonConvert.SerializeObject(value));
    }

    /// <summary>
    /// 在键到值存储的散列中设置字段。如果密钥不存在，则创建一个包含散列的新密钥。如果字段在散列中已经存在，则会覆盖它。
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="hashField">键值</param>
    /// <param name="value">缓存Value</param>
    /// <returns></returns>
    public bool Add(string key, string hashField, string value)
    {
        return _cache.HashSet(_redisKeyPrefix + key, hashField, value);
    }

    public async Task<bool> AddAsync(string key, string hashField, string value)
    {
        return await _cache.HashSetAsync(_redisKeyPrefix + key, hashField, value);

    }

    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <param name="key">缓存Key</param>
    /// <param name="hashField">键值</param>
    /// <returns></returns>
    public T Get<T>(string key, string hashField) where T : class
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        key = _redisKeyPrefix + key;
        var value = _cache.HashGet(key, hashField);

        if (!value.HasValue)
            return null;
        return JsonConvert.DeserializeObject<T>(value);
    }

    public async Task<T> GetAsync<T>(string key, string hashField) where T : class
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        key = _redisKeyPrefix + key;
        var value = await _cache.HashGetAsync(key, hashField);

        if (!value.HasValue)
            return null;
        return JsonConvert.DeserializeObject<T>(value);
    }
}

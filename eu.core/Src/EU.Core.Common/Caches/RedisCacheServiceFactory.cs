using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace EU.Core.Common.Caches;

/// <summary>
/// Redis 缓存服务工厂接口
/// </summary>
public interface IRedisCacheServiceFactory
{
    /// <summary>
    /// 创建指定数据库的 Redis 缓存服务实例
    /// </summary>
    /// <param name="databaseNumber">数据库编号 (0-15)</param>
    /// <returns>RedisCacheService 实例</returns>
    RedisCacheService Create(int databaseNumber = 0);
}

/// <summary>
/// Redis 缓存服务工厂实现
/// </summary>
public class RedisCacheServiceFactory : IRedisCacheServiceFactory
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IConfiguration _configuration;

    public RedisCacheServiceFactory(IConnectionMultiplexer connection, IConfiguration configuration)
    {
        _connection = connection;
        _configuration = configuration;
    }

    public RedisCacheService Create(int databaseNumber = 0)
    {
        if (databaseNumber < 0 || databaseNumber > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(databaseNumber), "Redis 数据库编号必须在 0-15 之间");
        }

        return new RedisCacheService(_connection, _configuration, databaseNumber);
    }
}

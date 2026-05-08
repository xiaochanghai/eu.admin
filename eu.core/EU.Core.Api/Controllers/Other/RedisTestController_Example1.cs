using EU.Core.Common.Caches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using EU.Core.Model;

namespace EU.Core.Api.Controllers;

/// <summary>
/// Redis 测试控制器 - 方式一：注入依赖手动创建（推荐）
/// </summary>
[Route("api/[controller]")]
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_Hidden)]
public class RedisTestController_Example1 : ControllerBase
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IConfiguration _configuration;

    // 不同数据库的实例
    private readonly RedisCacheService _db0;  // 默认数据库
    private readonly RedisCacheService _db1;  // 用户菜单数据库
    private readonly RedisCacheService _db2;  // 模块信息数据库
    private readonly RedisCacheService _db3;  // 系统参数数据库
    private readonly RedisCacheService _db4;  // 用户信息数据库
    private readonly RedisCacheService _db5;  // SignalR 数据库

    /// <summary>
    /// 构造函数：注入连接和配置，创建不同数据库的实例
    /// </summary>
    public RedisTestController_Example1(
        IConnectionMultiplexer connection,
        IConfiguration configuration)
    {
        _connection = connection;
        _configuration = configuration;

        // 创建不同数据库的实例
        _db0 = new RedisCacheService(_connection, _configuration, 0);
        _db1 = new RedisCacheService(_connection, _configuration, 1);
        _db2 = new RedisCacheService(_connection, _configuration, 2);
        _db3 = new RedisCacheService(_connection, _configuration, 3);
        _db4 = new RedisCacheService(_connection, _configuration, 4);
        _db5 = new RedisCacheService(_connection, _configuration, 5);
    }

    /// <summary>
    /// 测试不同数据库的操作
    /// </summary>
    [HttpGet("test-multiple-databases")]
    public ServiceResult<object> TestMultipleDatabases()
    {
        try
        {
            // 在数据库 0 中操作
            _db0.Add("db0:user:1001", "用户1001的数据");
            var db0Value = _db0.Get("db0:user:1001");

            // 在数据库 1 中操作（用户菜单）
            _db1.AddObject("menu:user:1001", new { UserId = 1001, Menu = "菜单配置" });
            var db1Value = _db1.Get<dynamic>("menu:user:1001");

            // 在数据库 2 中操作（模块信息）
            _db2.Add("module:info", "模块配置信息");
            var db2Value = _db2.Get("module:info");

            // 在数据库 3 中操作（系统参数）
            _db3.Add("system:config", "系统配置");
            var db3Value = _db3.Get("system:config");

            // 在数据库 4 中操作（用户信息）
            _db4.AddObject("user:profile:1001", new { Name = "张三", Age = 25 });
            var db4Value = _db4.Get<dynamic>("user:profile:1001");

            // 在数据库 5 中操作（SignalR）
            _db5.Add("signalr:connection:conn123", "连接信息");
            var db5Value = _db5.Get("signalr:connection:conn123");

            var result = new
            {
                数据库0_默认 = db0Value,
                数据库1_用户菜单 = db1Value,
                数据库2_模块信息 = db2Value,
                数据库3_系统参数 = db3Value,
                数据库4_用户信息 = db4Value,
                数据库5_SignalR = db5Value,
                说明 = "每个数据库的数据相互隔离，互不干扰"
            };

            // 清理测试数据
            _db0.Remove("db0:user:1001");
            _db1.Remove("menu:user:1001");
            _db2.Remove("module:info");
            _db3.Remove("system:config");
            _db4.Remove("user:profile:1001");
            _db5.Remove("signalr:connection:conn123");

            return ServiceResult<object>.OprateSuccess(result, "多数据库操作测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 动态指定数据库编号进行操作
    /// </summary>
    [HttpGet("test-dynamic-database/{dbNumber}")]
    public ServiceResult<object> TestDynamicDatabase(int dbNumber)
    {
        try
        {
            if (dbNumber < 0 || dbNumber > 15)
            {
                return ServiceResult<object>.OprateFailed("数据库编号必须在 0-15 之间");
            }

            // 动态创建指定数据库的实例
            var redis = new RedisCacheService(_connection, _configuration, dbNumber);

            // 执行操作
            var testKey = $"db{dbNumber}:test:key";
            var testValue = $"这是数据库{dbNumber}的测试数据";

            redis.Add(testKey, testValue, TimeSpan.FromMinutes(5));
            var getValue = redis.Get(testKey);
            var exists = redis.Exists(testKey);

            var result = new
            {
                数据库编号 = dbNumber,
                测试键 = testKey,
                存储的值 = testValue,
                获取的值 = getValue,
                键是否存在 = exists,
                操作状态 = "成功"
            };

            // 清理测试数据
            redis.Remove(testKey);

            return ServiceResult<object>.OprateSuccess(result, $"数据库{dbNumber}操作成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试特定数据库的所有操作
    /// </summary>
    [HttpGet("test-database-operations/{dbNumber}")]
    public ServiceResult<object> TestDatabaseOperations(int dbNumber)
    {
        try
        {
            if (dbNumber < 0 || dbNumber > 15)
            {
                return ServiceResult<object>.OprateFailed("数据库编号必须在 0-15 之间");
            }

            var redis = new RedisCacheService(_connection, _configuration, dbNumber);

            // 1. 字符串操作
            redis.Add("string:test", "字符串值", TimeSpan.FromMinutes(5));
            var stringValue = redis.Get("string:test");

            // 2. 对象操作
            var testObj = new { Id = 1001, Name = "测试对象", Time = DateTime.Now };
            redis.AddObject("object:test", testObj, TimeSpan.FromMinutes(5));
            var objectValue = redis.Get<dynamic>("object:test");

            // 3. 哈希表操作
            redis.AddObject("hash:test", "field1", "值1");
            redis.AddObject("hash:test", "field2", "值2");
            var hashValue1 = redis.Get<string>("hash:test", "field1");
            var hashValue2 = redis.Get<string>("hash:test", "field2");

            // 4. 列表操作
            redis.ListRightPush("list:test", "元素1");
            redis.ListRightPush("list:test", "元素2");
            redis.ListRightPush("list:test", "元素3");
            var listItem = redis.ListDequeue("list:test");

            var result = new
            {
                数据库编号 = dbNumber,
                字符串操作 = new { 键 = "string:test", 值 = stringValue },
                对象操作 = new { 键 = "object:test", 值 = objectValue },
                哈希表操作 = new
                {
                    键 = "hash:test",
                    字段1 = hashValue1,
                    字段2 = hashValue2
                },
                列表操作 = new
                {
                    键 = "list:test",
                    弹出的元素 = listItem?.ToString()
                },
                测试状态 = "所有操作成功"
            };

            // 清理测试数据
            redis.Remove("string:test");
            redis.Remove("object:test");
            redis.Remove("hash:test");
            redis.Remove("list:test");

            return ServiceResult<object>.OprateSuccess(result, $"数据库{dbNumber}所有操作测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }
}

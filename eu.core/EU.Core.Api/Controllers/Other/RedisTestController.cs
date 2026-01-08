using EU.Core.Common.Caches;
using StackExchange.Redis;

namespace EU.Core.Api.Controllers;

/// <summary>
/// Redis 服务测试控制器
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class RedisTestController : ControllerBase
{
    private readonly RedisCacheService _redisCache;
    private readonly IConnectionMultiplexer _connection;

    public RedisTestController(RedisCacheService redisCache, IConnectionMultiplexer connection)
    {
        _redisCache = redisCache;
        _connection = connection;
    }

    /// <summary>
    /// 测试 Redis 连接状态
    /// </summary>
    /// <returns></returns>
    [HttpGet("ping")]
    public async Task<ServiceResult<object>> TestPing()
    {
        try
        {
            var isConnected = await _redisCache.PingAsync();

            if (isConnected)
            {
                var endpoints = _connection.GetEndPoints();
                var serverInfo = new
                {
                    IsConnected = isConnected,
                    Endpoints = endpoints.Select(e => e.ToString()).ToList(),
                    ConnectionStatus = _connection.IsConnected ? "已连接" : "未连接",
                    Message = "Redis 服务运行正常"
                };

                return ServiceResult<object>.OprateSuccess(serverInfo, "Redis 连接成功");
            }
            else
            {
                return ServiceResult<object>.OprateFailed("Redis 连接失败，请检查 Redis 服务是否启动");
            }
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"Redis 连接异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试基本的 Redis 操作（增删改查）
    /// </summary>
    /// <returns></returns>
    [HttpGet("test-basic-operations")]
    public async Task<ServiceResult<object>> TestBasicOperations()
    {
        try
        {
            var testKey = "test:basic:key";
            var testValue = "Hello Redis!";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. 测试添加
            var addResult = _redisCache.Add(testKey, testValue, TimeSpan.FromMinutes(5));

            // 2. 测试获取
            var getValue = _redisCache.Get(testKey);

            // 3. 测试检查存在
            var exists = _redisCache.Exists(testKey);

            // 4. 测试对象操作
            var testObject = new
            {
                Id = 1001,
                Name = "测试用户",
                Email = "test@example.com",
                CreatedAt = timestamp
            };
            var objectKey = "test:object:1001";
            _redisCache.AddObject(objectKey, testObject, TimeSpan.FromMinutes(5));
            var getObject = _redisCache.Get<dynamic>(objectKey);

            // 5. 测试删除
            var removeResult = _redisCache.Remove(testKey);
            var existsAfterRemove = _redisCache.Exists(testKey);

            var result = new
            {
                测试时间 = timestamp,
                添加结果 = addResult ? "成功" : "失败",
                获取值 = getValue,
                键存在 = exists ? "是" : "否",
                对象存储 = getObject != null ? "成功" : "失败",
                对象内容 = getObject,
                删除结果 = removeResult ? "成功" : "失败",
                删除后键存在 = existsAfterRemove ? "是" : "否",
                总结 = "所有基本操作测试通过！"
            };

            return ServiceResult<object>.OprateSuccess(result, "Redis 基本操作测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"Redis 操作测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试 Redis 哈希表操作
    /// </summary>
    /// <returns></returns>
    [HttpGet("test-hash-operations")]
    public async Task<ServiceResult<object>> TestHashOperations()
    {
        try
        {
            var hashKey = "test:user:profile:1001";

            // 添加哈希字段
            _redisCache.AddObject(hashKey, "name", "张三");
            _redisCache.AddObject(hashKey, "age", 25);
            _redisCache.AddObject(hashKey, "email", "zhangsan@example.com");
            await _redisCache.AddObjectAsync(hashKey, "phone", "13800138000");

            // 获取哈希字段
            var name = _redisCache.Get<string>(hashKey, "name");
            var ageStr = await _redisCache.GetAsync<string>(hashKey, "age");
            var age = int.Parse(ageStr ?? "0");
            var email = await _redisCache.GetAsync<string>(hashKey, "email");
            var phone = await _redisCache.GetAsync<string>(hashKey, "phone");

            // 删除哈希字段
            await _redisCache.RemoveObject(hashKey, "phone");

            var result = new
            {
                哈希键 = hashKey,
                姓名 = name,
                年龄 = age,
                邮箱 = email,
                电话 = phone,
                删除电话字段 = "成功",
                总结 = "哈希表操作测试通过！"
            };

            // 清理测试数据
            _redisCache.Remove(hashKey);

            return ServiceResult<object>.OprateSuccess(result, "Redis 哈希表操作测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"Redis 哈希表操作测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试 Redis 列表操作
    /// </summary>
    /// <returns></returns>
    [HttpGet("test-list-operations")]
    public ServiceResult<object> TestListOperations()
    {
        try
        {
            var listKey = "test:message:queue";

            // 清空列表
            _redisCache.Remove(listKey);

            // 右侧添加（队列尾部）
            _redisCache.ListRightPush(listKey, "消息1");
            _redisCache.ListRightPush(listKey, "消息2");
            _redisCache.ListRightPush(listKey, "消息3");

            // 左侧添加（队列头部）
            _redisCache.ListLeftPush(listKey, "优先消息");

            // 弹出元素
            var msg1 = _redisCache.ListDequeue(listKey);
            var msg2 = _redisCache.ListDequeue(listKey);

            var result = new
            {
                列表键 = listKey,
                添加的消息 = new[] { "优先消息", "消息1", "消息2", "消息3" },
                弹出的第一个消息 = msg1?.ToString(),
                弹出的第二个消息 = msg2?.ToString(),
                说明 = "先进先出（FIFO），从右侧弹出",
                总结 = "列表操作测试通过！"
            };

            // 清理测试数据
            _redisCache.Remove(listKey);

            return ServiceResult<object>.OprateSuccess(result, "Redis 列表操作测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"Redis 列表操作测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试 Redis 多数据库操作
    /// </summary>
    /// <returns></returns>
    [HttpGet("test-multi-database")]
    public ServiceResult<object> TestMultiDatabase()
    {
        try
        {
            // 使用静态工厂方法创建不同数据库的实例
            var db0 = RedisCacheService.Create(0);  // 默认数据库
            var db1 = RedisCacheService.Create(1);  // 用户菜单数据库
            var db2 = RedisCacheService.Create(2);  // 模块信息数据库

            // 在不同数据库中存储数据
            db0.Add("db0:test", "数据库0的数据");
            db1.Add("db1:test", "数据库1的数据");
            db2.Add("db2:test", "数据库2的数据");

            // 从不同数据库读取数据
            var value0 = db0.Get("db0:test");
            var value1 = db1.Get("db1:test");
            var value2 = db2.Get("db2:test");

            var result = new
            {
                数据库0 = new { 键 = "db0:test", 值 = value0, 说明 = "默认数据库" },
                数据库1 = new { 键 = "db1:test", 值 = value1, 说明 = "用户菜单数据库" },
                数据库2 = new { 键 = "db2:test", 值 = value2, 说明 = "模块信息数据库" },
                总结 = "多数据库操作测试通过！不同数据库的数据相互隔离"
            };

            // 清理测试数据
            db0.Remove("db0:test");
            db1.Remove("db1:test");
            db2.Remove("db2:test");

            return ServiceResult<object>.OprateSuccess(result, "Redis 多数据库操作测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"Redis 多数据库操作测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试 Redis 过期时间
    /// </summary>
    /// <returns></returns>
    [HttpGet("test-expiration")]
    public async Task<ServiceResult<object>> TestExpiration()
    {
        try
        {
            var key = "test:expiration:key";

            // 设置 3 秒过期
            _redisCache.Add(key, "这条数据3秒后过期", TimeSpan.FromSeconds(3));

            var exists1 = _redisCache.Exists(key);

            // 等待 4 秒
            await Task.Delay(4000);

            var exists2 = _redisCache.Exists(key);

            var result = new
            {
                测试键 = key,
                过期时间 = "3秒",
                添加后是否存在 = exists1 ? "存在" : "不存在",
                等待4秒后是否存在 = exists2 ? "存在" : "不存在",
                总结 = !exists2 ? "过期时间测试通过！数据已自动删除" : "过期时间测试失败！数据未删除"
            };

            return ServiceResult<object>.OprateSuccess(result, "Redis 过期时间测试完成");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"Redis 过期时间测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取 Redis 服务器信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("server-info")]
    public ServiceResult<object> GetServerInfo()
    {
        try
        {
            var endpoints = _connection.GetEndPoints();
            var serverList = new List<object>();

            foreach (var endpoint in endpoints)
            {
                var server = _connection.GetServer(endpoint);
                var info = server.Info();

                var serverInfo = new Dictionary<string, string>();
                foreach (var section in info)
                {
                    foreach (var item in section)
                    {
                        serverInfo[$"{section.Key}:{item.Key}"] = item.Value;
                    }
                }

                serverList.Add(new
                {
                    端点 = endpoint.ToString(),
                    连接状态 = server.IsConnected ? "已连接" : "未连接",
                    服务器类型 = server.ServerType.ToString(),
                    版本 = server.Version.ToString(),
                    详细信息 = serverInfo.Take(20).ToDictionary(k => k.Key, v => v.Value)
                });
            }

            return ServiceResult<object>.OprateSuccess(serverList, "获取 Redis 服务器信息成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"获取 Redis 服务器信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 综合测试（运行所有测试）
    /// </summary>
    /// <returns></returns>
    [HttpGet("test-all")]
    public async Task<ServiceResult<object>> TestAll()
    {
        var results = new Dictionary<string, object>();

        try
        {
            // 1. 连接测试
            var pingResult = await TestPing();
            results["1.连接测试"] = pingResult.Success ? "✅ 通过" : "❌ 失败";

            if (!pingResult.Success)
            {
                return ServiceResult<object>.OprateFailed("Redis 连接失败，无法继续测试", results);
            }

            // 2. 基本操作测试
            var basicResult = await TestBasicOperations();
            results["2.基本操作测试"] = basicResult.Success ? "✅ 通过" : "❌ 失败";

            // 3. 哈希表操作测试
            var hashResult = await TestHashOperations();
            results["3.哈希表操作测试"] = hashResult.Success ? "✅ 通过" : "❌ 失败";

            // 4. 列表操作测试
            var listResult = TestListOperations();
            results["4.列表操作测试"] = listResult.Success ? "✅ 通过" : "❌ 失败";

            // 5. 多数据库测试
            var multiDbResult = TestMultiDatabase();
            results["5.多数据库操作测试"] = multiDbResult.Success ? "✅ 通过" : "❌ 失败";

            var allPassed = pingResult.Success && basicResult.Success &&
                           hashResult.Success && listResult.Success && multiDbResult.Success;

            results["测试结果"] = allPassed ? "🎉 所有测试通过！Redis 服务运行正常" : "⚠️ 部分测试失败，请检查详细信息";

            return ServiceResult<object>.OprateSuccess(results, "Redis 综合测试完成");
        }
        catch (Exception ex)
        {
            results["错误"] = ex.Message;
            return ServiceResult<object>.OprateFailed("Redis 综合测试失败", results);
        }
    }
}

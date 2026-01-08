using EU.Core.Common.Caches;

namespace EU.Core.Api.Controllers;

/// <summary>
/// Redis 测试控制器 - 方式二：使用静态工厂方法（向后兼容）
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class RedisTestController_Example2 : ControllerBase
{
    /// <summary>
    /// 测试使用静态工厂方法创建不同数据库实例
    /// </summary>
    [HttpGet("test-static-factory")]
    public ServiceResult<object> TestStaticFactory()
    {
        try
        {
            // 使用静态工厂方法创建不同数据库的实例
            var db0 = RedisCacheService.Create(0);  // 默认数据库
            var db1 = RedisCacheService.Create(1);  // 用户菜单
            var db2 = RedisCacheService.Create(2);  // 模块信息
            var db3 = RedisCacheService.Create(3);  // 系统参数
            var db4 = RedisCacheService.Create(4);  // 用户信息
            var db5 = RedisCacheService.Create(5);  // SignalR

            // 在不同数据库中存储数据
            db0.Add("key0", "数据库0的值");
            db1.Add("key1", "数据库1的值");
            db2.Add("key2", "数据库2的值");
            db3.Add("key3", "数据库3的值");
            db4.Add("key4", "数据库4的值");
            db5.Add("key5", "数据库5的值");

            // 读取数据
            var result = new
            {
                DB0_默认 = db0.Get("key0"),
                DB1_用户菜单 = db1.Get("key1"),
                DB2_模块信息 = db2.Get("key2"),
                DB3_系统参数 = db3.Get("key3"),
                DB4_用户信息 = db4.Get("key4"),
                DB5_SignalR = db5.Get("key5"),
                说明 = "使用静态工厂方法 Create(dbNumber) 创建实例"
            };

            // 清理
            db0.Remove("key0");
            db1.Remove("key1");
            db2.Remove("key2");
            db3.Remove("key3");
            db4.Remove("key4");
            db5.Remove("key5");

            return ServiceResult<object>.OprateSuccess(result, "静态工厂方法测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 动态使用工厂方法操作指定数据库
    /// </summary>
    [HttpGet("factory-dynamic/{dbNumber}")]
    public ServiceResult<object> FactoryDynamic(int dbNumber)
    {
        try
        {
            if (dbNumber < 0 || dbNumber > 15)
            {
                return ServiceResult<object>.OprateFailed("数据库编号必须在 0-15 之间");
            }

            // 使用静态工厂方法
            var redis = RedisCacheService.Create(dbNumber);

            // 执行测试操作
            var key = $"factory:test:{dbNumber}";
            var value = $"通过工厂方法访问 DB{dbNumber}";

            redis.Add(key, value);
            var getValue = redis.Get(key);
            var exists = redis.Exists(key);

            var result = new
            {
                数据库编号 = dbNumber,
                创建方式 = "RedisCacheService.Create(dbNumber)",
                测试键 = key,
                存储值 = value,
                获取值 = getValue,
                键存在 = exists
            };

            redis.Remove(key);

            return ServiceResult<object>.OprateSuccess(result, $"工厂方法访问 DB{dbNumber} 成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }
}

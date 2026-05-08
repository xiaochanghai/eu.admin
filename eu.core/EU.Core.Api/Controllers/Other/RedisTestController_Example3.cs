using EU.Core.Common.Caches;

namespace EU.Core.Api.Controllers;

/// <summary>
/// Redis 测试控制器 - 方式三：使用工厂模式（最优雅）
/// </summary>
[Route("api/[controller]"), ApiExplorerSettings(GroupName = Grouping.GroupName_Hidden)]
[ApiController]
public class RedisTestController_Example3 : ControllerBase
{
    private readonly IRedisCacheServiceFactory _factory;

    /// <summary>
    /// 构造函数：注入工厂
    /// </summary>
    public RedisTestController_Example3(IRedisCacheServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 测试使用工厂创建不同数据库实例
    /// </summary>
    [HttpGet("test-factory")]
    public ServiceResult<object> TestFactory()
    {
        try
        {
            // 使用工厂创建不同数据库的实例
            var db0 = _factory.Create(0);  // 默认数据库
            var db1 = _factory.Create(1);  // 用户菜单
            var db2 = _factory.Create(2);  // 模块信息
            var db3 = _factory.Create(3);  // 系统参数
            var db4 = _factory.Create(4);  // 用户信息
            var db5 = _factory.Create(5);  // SignalR

            // 在不同数据库中存储数据
            db0.Add("factory:key0", "工厂模式-数据库0");
            db1.Add("factory:key1", "工厂模式-数据库1");
            db2.Add("factory:key2", "工厂模式-数据库2");
            db3.Add("factory:key3", "工厂模式-数据库3");
            db4.Add("factory:key4", "工厂模式-数据库4");
            db5.Add("factory:key5", "工厂模式-数据库5");

            // 读取数据
            var result = new
            {
                DB0_默认 = db0.Get("factory:key0"),
                DB1_用户菜单 = db1.Get("factory:key1"),
                DB2_模块信息 = db2.Get("factory:key2"),
                DB3_系统参数 = db3.Get("factory:key3"),
                DB4_用户信息 = db4.Get("factory:key4"),
                DB5_SignalR = db5.Get("factory:key5"),
                说明 = "使用工厂模式 IRedisCacheServiceFactory 创建实例",
                优点 = new[]
                {
                    "1. 符合依赖注入最佳实践",
                    "2. 便于单元测试（可以 mock factory）",
                    "3. 代码更清晰、更易维护",
                    "4. 避免直接依赖静态方法"
                }
            };

            // 清理
            db0.Remove("factory:key0");
            db1.Remove("factory:key1");
            db2.Remove("factory:key2");
            db3.Remove("factory:key3");
            db4.Remove("factory:key4");
            db5.Remove("factory:key5");

            return ServiceResult<object>.OprateSuccess(result, "工厂模式测试成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 动态使用工厂创建指定数据库实例
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

            // 使用工厂创建实例
            var redis = _factory.Create(dbNumber);

            // 执行测试操作
            var key = $"factory:dynamic:test:{dbNumber}";
            var value = $"工厂模式动态访问 DB{dbNumber}";

            redis.Add(key, value);
            var getValue = redis.Get(key);
            var exists = redis.Exists(key);

            var result = new
            {
                数据库编号 = dbNumber,
                创建方式 = "IRedisCacheServiceFactory.Create(dbNumber)",
                测试键 = key,
                存储值 = value,
                获取值 = getValue,
                键存在 = exists,
                工厂优势 = "通过 DI 容器管理，符合 SOLID 原则"
            };

            redis.Remove(key);

            return ServiceResult<object>.OprateSuccess(result, $"工厂模式访问 DB{dbNumber} 成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量操作多个数据库
    /// </summary>
    [HttpGet("batch-operations")]
    public ServiceResult<object> BatchOperations()
    {
        try
        {
            var results = new List<object>();

            // 批量处理多个数据库
            for (int i = 0; i <= 5; i++)
            {
                var redis = _factory.Create(i);
                var key = $"batch:db{i}:key";
                var value = $"批量操作-数据库{i}";

                redis.Add(key, value, TimeSpan.FromMinutes(5));
                var getValue = redis.Get(key);

                results.Add(new
                {
                    数据库 = i,
                    键 = key,
                    值 = getValue,
                    状态 = "成功"
                });

                redis.Remove(key);
            }

            return ServiceResult<object>.OprateSuccess(new
            {
                操作结果 = results,
                总结 = "工厂模式非常适合批量操作多个数据库"
            }, "批量操作成功");
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.OprateFailed($"批量操作失败: {ex.Message}");
        }
    }
}

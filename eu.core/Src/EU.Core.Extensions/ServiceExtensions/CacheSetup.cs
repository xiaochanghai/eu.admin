using EU.Core.Common;
using EU.Core.Common.Caches;
using EU.Core.Common.Option;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EU.Core.Extensions;

public static class CacheSetup
{
	/// <summary>
	/// 统一注册缓存
	/// </summary>
	/// <param name="services"></param>
	public static void AddCacheSetup(this IServiceCollection services)
	{
		var cacheOptions = App.GetConfig<RedisOptions>();
		Console.WriteLine("统一注册缓存 Enable:" + cacheOptions.Enable);
		Console.WriteLine("统一注册缓存 ConnectionString:" + cacheOptions.ConnectionString);
        if (cacheOptions?.Enable == true)
		{
			// 配置启动Redis服务，虽然可能影响项目启动速度，但是不能在运行的时候报错，所以是合理的
			services.AddSingleton<IConnectionMultiplexer>(sp =>
			{
				//获取连接字符串
				var configuration = ConfigurationOptions.Parse(cacheOptions.ConnectionString, true);
				configuration.ResolveDns = true;
				configuration.ConnectTimeout = 5000;
				configuration.SyncTimeout = 5000;
				configuration.AbortOnConnectFail = false;
				configuration.ConnectRetry = 3;
				return ConnectionMultiplexer.Connect(configuration);
			});
			services.AddSingleton<ConnectionMultiplexer>(p => p.GetService<IConnectionMultiplexer>() as ConnectionMultiplexer);
			//使用Redis
			services.AddStackExchangeRedisCache(options =>
			{
				options.ConnectionMultiplexerFactory = () => Task.FromResult(App.GetService<IConnectionMultiplexer>(false));
				if (!cacheOptions.InstanceName.IsNullOrEmpty()) options.InstanceName = cacheOptions.InstanceName;
			});

			services.AddTransient<IRedisBasketRepository, RedisBasketRepository>();

			// 注册 RedisCacheService（使用依赖注入）
			services.AddScoped<RedisCacheService>();

			// 注册 RedisCacheService 工厂（用于创建不同数据库的实例）
			services.AddScoped<IRedisCacheServiceFactory, RedisCacheServiceFactory>();
		}
		else
		{
			//使用内存
			services.AddMemoryCache();
			services.AddDistributedMemoryCache();
		}

		services.AddSingleton<ICaching, Caching>();
	}
}
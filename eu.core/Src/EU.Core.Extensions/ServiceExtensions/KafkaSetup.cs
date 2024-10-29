using EU.Core.Common;
using EU.Core.EventBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.Extensions;

/// <summary>
/// 注入Kafka相关配置
/// </summary>
public static class KafkaSetup
{
    public static void AddKafkaSetup(this IServiceCollection services,IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        if (AppSettings.app(new string[] { "Kafka", "Enabled" }).ObjToBool())
        {
            services.Configure<KafkaOptions>(configuration.GetSection("kafka"));
            services.AddSingleton<IKafkaConnectionPool,KafkaConnectionPool>();
        }
    }
}

using EU.Core.Common;
using EU.Core.Common.DB;
using EU.Core.Common.Seed;
using EU.Core.Extensions;
using EU.Core.Repository.UnitOfWorks;
using EU.Core.Services.Extensions;
using EU.Core.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EU.Core.Jobs;

/// <summary>
/// 任务处理中心
/// </summary>
public class Helper
{
    public static void Init(ServiceCollection services)
    {
        var basePath = AppContext.BaseDirectory;
        services.AddSingleton(new AppSettings(basePath));

        services.AddTransient<IUnitOfWorkManage, UnitOfWorkManage>();
        services.AddScoped<DBSeed>();
        services.AddScoped<MyContext>();

        services.AddScoped<SqlSugar.ISqlSugarClient>(o =>
        {
            return new SqlSugar.SqlSugarScope(new SqlSugar.ConnectionConfig()
            {
                ConnectionString = BaseDBConfig.GetMainConnectionDb().Connection,    //必填, 数据库连接字符串
                DbType = (SqlSugar.DbType)BaseDBConfig.GetMainConnectionDb().DbType, //必填, 数据库类型
                IsAutoCloseConnection = true,                           //默认false, 时候知道关闭数据库连接, 设置为true无需使用using或者Close操作
            });
        });

        services.AddAppServices();
        //services.AddTransient<ISmQuartzJobServices, SmQuartzJobServices>();
        //services.AddTransient<IBaseRepository<SmQuartzJob>, BaseRepository<SmQuartzJob>>();
        //services.AddTransient<ISmQuartzJobLogServices, SmQuartzJobLogServices>();
        //services.AddTransient<IBaseRepository<SmQuartzJobLog>, BaseRepository<SmQuartzJobLog>>();

        // 注入事件服务
        services.AddLogging();
        services.AddSingleton<IHostApplicationLifetime, ConsoleHostApplicationLifetime>();
        services.AddJobSetup();

        var mqttOptions = AppSettings.appSingle<MqttBrokerSetting>("MqttBroker");
        if (mqttOptions.Enabled)
            services.AddMqttSetup(mqttOptions);

    }
}

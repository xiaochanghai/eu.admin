using EU.Core;
using EU.Core.Common;
using EU.Core.Common.Helper;
using EU.Core.Extensions;
using EU.Core.Jobs;
using EU.Core.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        DbSetup.DapperSqlMapper();
        Helper.Init(services);

        DBHelper.CheckServiceAvailable();

        // 检查 RabbitMQ
        RabbitMQHelper.CheckRabbitMQServiceAvailable();
        DBHelper.Init();

        var sp = services.BuildServiceProvider();

        var mqttEnabled = AppSettings.app("MqttBroker", "Enabled");

        if (mqttEnabled.ObjToBool())
        {
            var lifetime = sp.GetRequiredService<IHostApplicationLifetime>() as ConsoleHostApplicationLifetime;
            lifetime?.RegisterConsoleCancel();
            sp.ConfigureMqttEvents();
            await StartHostedServicesAsync(sp);
        }

        var schedulerCenter = sp.GetService<ISchedulerCenter>();
        // 任务处理中心
        TaskCenter taskCenter = new TaskCenter(schedulerCenter);
        await taskCenter.Start();
        Thread.Sleep(Timeout.Infinite);
    }

    private static async Task StartHostedServicesAsync(ServiceProvider serviceProvider)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }
    }
}

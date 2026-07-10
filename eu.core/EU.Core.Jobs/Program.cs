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

        using var sp = services.BuildServiceProvider();
        var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
        using var stoppingCts = new CancellationTokenSource();
        lifetime.ApplicationStopping.Register(() => stoppingCts.Cancel());

        (lifetime as ConsoleHostApplicationLifetime)?.RegisterConsoleCancel();

        var mqttEnabled = AppSettings.app("MqttBroker", "Enabled");

        if (mqttEnabled.ObjToBool())
        {
            sp.ConfigureMqttEvents();
            await StartHostedServicesAsync(sp);
        }

        var schedulerCenter = sp.GetService<ISchedulerCenter>();
        // 任务处理中心
        TaskCenter taskCenter = new TaskCenter(schedulerCenter);
        await taskCenter.Start();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingCts.Token);
        }
        catch (OperationCanceledException) when (stoppingCts.IsCancellationRequested)
        {
        }
    }

    private static async Task StartHostedServicesAsync(ServiceProvider serviceProvider)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }
    }
}

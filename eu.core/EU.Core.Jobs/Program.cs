using EU.Core.Common.Helper;
using EU.Core.Extensions;
using EU.Core.Jobs;
using EU.Core.Tasks;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        DbSetup.DapperSqlMapper();
        Helper.Init(services);

        DBHelper.CheckServiceAvailable();
        DBHelper.Init();

        var sp = services.BuildServiceProvider();
        var schedulerCenter = sp.GetService<ISchedulerCenter>();
        // 任务处理中心
        TaskCenter taskCenter = new TaskCenter(schedulerCenter);
        await taskCenter.Start();
        Thread.Sleep(Timeout.Infinite);

    }
}

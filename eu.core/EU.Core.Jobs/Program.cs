using EU.Core.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using EU.Core.Jobs;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        Helper.Init(services);

        var sp = services.BuildServiceProvider();
        var schedulerCenter = sp.GetService<ISchedulerCenter>();
        // 任务处理中心
        TaskCenter taskCenter = new TaskCenter(schedulerCenter);
        taskCenter.Start();
        Thread.Sleep(Timeout.Infinite);

    }
}

using EU.Core;
using EU.Core.Common;
using EU.Core.Common.DB;
using EU.Core.Common.Helper;
using EU.Core.Common.LogHelper;
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
        CheckServiceAvailable();
        DBHelper.Init();
        var sp = services.BuildServiceProvider();
        var schedulerCenter = sp.GetService<ISchedulerCenter>();
        // 任务处理中心
        TaskCenter taskCenter = new TaskCenter(schedulerCenter);
        await taskCenter.Start();
        Thread.Sleep(Timeout.Infinite);

    }

    public static void SendLog(string msg)
    {
        Logger.WriteLog($"[{DateTime.Now.ConvertToSecondString()}] {msg}");
    }

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    public static void CheckServiceAvailable()
    {
        var mainDbId = AppSettings.app(["MainDB"]).ObjToString();

        var listdatabase = AppSettings.app<MutiDBOperate>("DBS")
           .Where(i => i.Enabled).ToList();
        var mainConnetctDb = listdatabase.Find(x => x.ConnId == mainDbId);
        var conn = new MySqlConnector.MySqlConnectionStringBuilder(mainConnetctDb.Connection);

        while (true)
        {
            if (Utility.IsPortOpen(conn.Server, (int)conn.Port, TimeSpan.FromSeconds(3.0)))
            {
                SendLog("[数据库] 服务状态正常");
                break;
            }
            else
            {
                SendLog("[数据库] 服务状态异常, 等待 5 秒后重试");
                Thread.Sleep(5000);
            }
        }

        //while (true)
        //{
        //    if (Utility.IsPortOpen(AppSettingHelper.RabbitMQ_HostName, int.Parse(AppSettingHelper.RabbitMQ_Port), TimeSpan.FromSeconds(3.0)))
        //    {
        //        SendLog("[RabbitMQ] 服务状态正常");
        //        break;
        //    }
        //    else
        //    {
        //        SendLog("[RabbitMQ] 服务状态异常, 等待 5 秒后重试");
        //        Thread.Sleep(5000);
        //    }
        //}

    }
}

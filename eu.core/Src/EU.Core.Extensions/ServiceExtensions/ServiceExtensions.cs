using EU.Core.Common;
using EU.Core.Common.Const;
using EU.Core.Common.Helper;
using EU.Core.Common.LogHelper;
using EU.Core.Tasks;

namespace EU.Core.Extensions;

public static class ServiceExtensions
{


    public static void Init()
    {

        //TaskCallback消息订阅
        if (AppSettings.app(["RabbitMQ", "Enabled"]).ObjToBool())
        {
            Logger.WriteLog("[TaskCallback]启动消息订阅");
            RabbitMQHelper.ConsumeMsg<TaskCallbackMsg>(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, msg =>
            {
                ThreadPool.QueueUserWorkItem(TaskHelper.TaskCallback, msg);
                return ConsumeAction.Accept;
            });
        }


    }

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    //public static void CheckServiceAvailable()
    //{
    //    var conn = new MySqlConnector.MySqlConnectionStringBuilder(AppSettingHelper.DatabaseConnectionString);
    //    while (true)
    //    {
    //        if (Common.IsPortOpen(conn.Server, (int)conn.Port, TimeSpan.FromSeconds(3.0)))
    //        {
    //            LoggerHelper.SendLog("[数据库] 服务状态正常");
    //            break;
    //        }
    //        else
    //        {
    //            LoggerHelper.SendLog("[数据库] 服务状态异常, 等待 5 秒后重试");
    //            Thread.Sleep(5000);
    //        }
    //    }

    //    while (true)
    //    {
    //        if (Common.IsPortOpen(AppSettingHelper.RabbitMQ_HostName, int.Parse(AppSettingHelper.RabbitMQ_Port), TimeSpan.FromSeconds(3.0)))
    //        {
    //            LoggerHelper.SendLog("[RabbitMQ] 服务状态正常");
    //            break;
    //        }
    //        else
    //        {
    //            LoggerHelper.SendLog("[RabbitMQ] 服务状态异常, 等待 5 秒后重试");
    //            Thread.Sleep(5000);
    //        }
    //    }

    //    while (true)
    //    {
    //        try
    //        {
    //            var client = new CSRedisClient(AppSettingHelper.RedisConnectionString);
    //            if (client.Ping())
    //            {
    //                LoggerHelper.SendLog("[Redis] 服务状态正常");
    //                break;
    //            }
    //        }
    //        catch { }

    //        LoggerHelper.SendLog("[Redis] 服务状态异常, 等待 5 秒后重试");
    //        Thread.Sleep(5000);
    //    }
    //} 
}

using EU.Core.Common;
using EU.Core.Common.Const;
using EU.Core.Common.Helper;
using EU.Core.Common.LogHelper;
using EU.Core.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Threading.Tasks;

namespace EU.Core.Jobs;

/// <summary>
/// 任务处理中心
/// </summary>
public class TaskCenter
{
    private readonly ISchedulerCenter _schedulerCenter;

    static TaskCenter()
    {
        //ReloadOnChange = true 当appsettings.json被修改时重新加载

    }

    /// <summary>
    /// 初始化
    /// </summary>
    public TaskCenter(ISchedulerCenter schedulerCenter)
    {
        _schedulerCenter = schedulerCenter;
    }

    #region 启动任务服务
    /// <summary>
    /// 启动任务服务
    /// </summary>
    public async Task Start()
    {
        var container = new ServiceCollection();

        await _schedulerCenter.InitJobAsync();
        if (AppSettings.app(["RabbitMQ", "Enabled"]).ObjToBool())
        {
            Logger.WriteLog("[Task]启动消息订阅");
            RabbitMQHelper.ConsumeMsg<TaskMsg>(RabbitMQConsts.CLIENT_ID_TASK_JOB, msg =>
            {
                ThreadPool.QueueUserWorkItem(TaskHelper.TaskHandleAsync, msg);
                return ConsumeAction.Accept;
            });
        }
        else Logger.WriteLog("[Task] 未启动消息订阅");
        //RabbitMQHelper.ConsumeMsg<TaskMonitor>(RabbitMQConsts.CLIENT_ID_TASK_MONITOR, msg =>
        //{
        //    Logger.WriteLog($"[Task] {RabbitMQConsts.CLIENT_ID_TASK_MONITOR} msg:{msg}");
        //    return ConsumeAction.Accept;
        //});
    }

    #endregion

}
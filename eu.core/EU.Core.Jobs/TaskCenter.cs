using System;
using EU.Core.Common;
using EU.Core.Common.Const;
using EU.Core.Common.Helper;
using EU.Core.Common.LogHelper;
using EU.Core.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
    public void Start()
    {
        var container = new ServiceCollection();

        _schedulerCenter.InitJobAsync();
        Logger.WriteLog("[Task]启动消息订阅");
        if (AppSettings.app(["RabbitMQ", "Enabled"]).ObjToBool())
        {
            RabbitMQHelper.ConsumeMsg<TaskMsg>(RabbitMQConsts.CLIENT_ID_TASK_JOB, msg =>
            {
                System.Threading.ThreadPool.QueueUserWorkItem(TaskHelper.TaskHandleAsync, msg);
                return ConsumeAction.Accept;
            });
        }
        //RabbitMQHelper.ConsumeMsg<TaskMonitor>(RabbitMQConsts.CLIENT_ID_TASK_MONITOR, msg =>
        //{
        //    Logger.WriteLog($"[Task] {RabbitMQConsts.CLIENT_ID_TASK_MONITOR} msg:{msg}");
        //    return ConsumeAction.Accept;
        //});
    }

    #endregion

}
using EU.Core.Common.Helper;
using EU.Core.Common;
using EU.Core.Model.Models;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Triggers;
using Quartz.Spi;
using System.Collections.Specialized;
using System.Reflection;
using EU.Core.Common.LogHelper;
using EU.Core.Model.ViewModels;
using EU.Core.Model;
using EU.Core.Common.Const;

namespace EU.Core.Tasks;

/// <summary>
/// 任务调度管理中心
/// </summary>
/// <summary>
/// 任务调度管理中心
/// </summary>
public class SchedulerCenterServer : ISchedulerCenter
{
    private Task<IScheduler> _scheduler;
    private readonly IJobFactory _iocjobFactory;
    public SchedulerCenterServer(IJobFactory jobFactory)
    {
        _iocjobFactory = jobFactory;
        _scheduler = GetSchedulerAsync();
    }
    private Task<IScheduler> GetSchedulerAsync()
    {
        if (_scheduler != null)
            return this._scheduler;
        else
        {
            // 从Factory中获取Scheduler实例
            var collection = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" },
            };
            var factory = new StdSchedulerFactory(collection);
            return _scheduler = factory.GetScheduler();
        }
    }

    /// <summary>
    /// 开启任务调度
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<string>> StartScheduleAsync()
    {
        var result = new ServiceResult<string>();
        try
        {
            this._scheduler.Result.JobFactory = this._iocjobFactory;
            if (!this._scheduler.Result.IsStarted)
            {
                //等待任务运行完成
                await this._scheduler.Result.Start();
                Logger.WriteLog($"任务调度开启！");
                result.Success = true;
                result.Message = $"任务调度开启成功";
                return result;
            }
            else
            {
                result.Success = false;
                result.Message = $"任务调度已经开启";
                return result;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 停止任务调度
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<string>> StopScheduleAsync()
    {
        var result = new ServiceResult<string>();
        try
        {
            if (!this._scheduler.Result.IsShutdown)
            {
                //等待任务运行完成
                await this._scheduler.Result.Shutdown();
                await Console.Out.WriteLineAsync("任务调度停止！");
                result.Success = true;
                result.Message = $"任务调度停止成功";
                return result;
            }
            else
            {
                result.Success = false;
                result.Message = $"任务调度已经停止";
                return result;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 添加一个计划任务（映射程序集指定IJob实现类）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tasksQz"></param>
    /// <returns></returns>
    public async Task<ServiceResult<string>> AddScheduleJobAsync(TasksQz tasksQz)
    {
        var result = new ServiceResult<string>();

        if (tasksQz != null)
        {
            try
            {
                var jobKey = new JobKey(tasksQz.Id.ToString(), tasksQz.JobGroup);
                if (await _scheduler.Result.CheckExists(jobKey))
                {
                    result.Success = false;
                    result.Message = $"该任务计划已经在执行:【{tasksQz.Name}】,请勿重复启动！";
                    return result;
                }
                #region 设置开始时间和结束时间

                if (tasksQz.BeginTime == null)
                {
                    tasksQz.BeginTime = DateTime.Now;
                }
                var starRunTime = DateBuilder.NextGivenSecondDate(tasksQz.BeginTime, 1);//设置开始时间
                if (tasksQz.EndTime == null)
                {
                    tasksQz.EndTime = DateTime.MaxValue.AddDays(-1);
                }
                var endRunTime = DateBuilder.NextGivenSecondDate(tasksQz.EndTime, 1);//设置暂停时间

                #endregion

                #region 通过反射获取程序集类型和类   

                var assembly = Assembly.Load(new AssemblyName(tasksQz.AssemblyName));
                var jobType = assembly.GetType(tasksQz.AssemblyName + "." + tasksQz.ClassName);

                #endregion
                //判断任务调度是否开启
                if (!_scheduler.Result.IsStarted)
                {
                    await StartScheduleAsync();
                }

                //传入反射出来的执行程序集
                var job = new JobDetailImpl(tasksQz.Id.ToString(), tasksQz.JobGroup, jobType);
                job.JobDataMap.Add("JobParam", tasksQz.JobParams);
                //job.JobDataMap.Add("TenantId", tasksQz.TenantId);
                ITrigger trigger;

                #region 泛型传递
                //IJobDetail job = JobBuilder.Create<T>()
                //    .WithIdentity(sysSchedule.Name, sysSchedule.JobGroup)
                //    .Build();
                #endregion

                if (tasksQz.Cron != null && CronExpression.IsValidExpression(tasksQz.Cron) && tasksQz.TriggerType > 0)
                {
                    trigger = CreateCronTrigger(tasksQz);

                    ((CronTriggerImpl)trigger).MisfireInstruction = MisfireInstruction.CronTrigger.DoNothing;
                }
                else
                {
                    trigger = CreateSimpleTrigger(tasksQz);
                }

                // 告诉Quartz使用我们的触发器来安排作业
                await _scheduler.Result.ScheduleJob(job, trigger);
                //await Task.Delay(TimeSpan.FromSeconds(120));
                //await Console.Out.WriteLineAsync("关闭了调度器！");
                //await _scheduler.Result.Shutdown();
                result.Success = true;
                result.Message = $"【{tasksQz.Name}】成功";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"任务计划异常:【{ex.Message}】";
                return result;
            }
        }
        else
        {
            result.Success = false;
            result.Message = $"任务计划不存在:【{tasksQz?.Name}】";
            return result;
        }
    }

    /// <summary>
    /// 任务是否存在?
    /// </summary>
    /// <returns></returns>
    public async Task<bool> IsExistScheduleJobAsync(TasksQz sysSchedule)
    {
        var jobKey = new JobKey(sysSchedule.Id.ToString(), sysSchedule.JobGroup);
        if (await _scheduler.Result.CheckExists(jobKey))
            return true;
        else
            return false;
    }
    /// <summary>
    /// 移除一个指定的计划任务
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<string>> StopScheduleJobAsync(TasksQz sysSchedule)
    {
        var result = new ServiceResult<string>();
        try
        {
            var jobKey = new JobKey(sysSchedule.Id.ToString(), sysSchedule.JobGroup);
            if (!await _scheduler.Result.CheckExists(jobKey))
            {
                result.Success = false;
                result.Message = $"未找到要暂停的任务:【{sysSchedule.Name}】";
                return result;
            }
            else
            {
                await this._scheduler.Result.DeleteJob(jobKey);
                result.Success = true;
                result.Message = $"【{sysSchedule.Name}】成功";
                return result;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 恢复指定的计划任务
    /// </summary>
    /// <param name="sysSchedule"></param>
    /// <returns></returns>
    public async Task<ServiceResult<string>> ResumeJob(TasksQz sysSchedule)
    {
        var result = new ServiceResult<string>();
        try
        {
            var jobKey = new JobKey(sysSchedule.Id.ToString(), sysSchedule.JobGroup);
            if (!await _scheduler.Result.CheckExists(jobKey))
            {
                result.Success = false;
                result.Message = $"未找到要恢复的任务:【{sysSchedule.Name}】";
                return result;
            }
            await this._scheduler.Result.ResumeJob(jobKey);
            result.Success = true;
            result.Message = $"【{sysSchedule.Name}】成功";
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    /// <summary>
    /// 暂停指定的计划任务
    /// </summary>
    /// <param name="sysSchedule"></param>
    /// <returns></returns>
    public async Task<ServiceResult<string>> PauseJob(TasksQz sysSchedule)
    {
        var result = new ServiceResult<string>();
        try
        {
            var jobKey = new JobKey(sysSchedule.Id.ToString(), sysSchedule.JobGroup);
            if (!await _scheduler.Result.CheckExists(jobKey))
            {
                result.Success = false;
                result.Message = $"未找到要暂停的任务:【{sysSchedule.Name}】";
                return result;
            }
            await this._scheduler.Result.PauseJob(jobKey);
            result.Success = true;
            result.Message = $"【{sysSchedule.Name}】成功";
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    #region 状态状态帮助方法
    public async Task<List<TaskInfoDto>> GetTaskStaus(TasksQz sysSchedule)
    {

        var ls = new List<TaskInfoDto>();
        var noTask = new List<TaskInfoDto>{ new TaskInfoDto {
                jobId = sysSchedule.Id.ToString(),
                jobGroup = sysSchedule.JobGroup,
                triggerId = "",
                triggerGroup = "",
                triggerStatus = "不存在"
            } };
        var jobKey = new JobKey(sysSchedule.Id.ToString(), sysSchedule.JobGroup);
        var job = await this._scheduler.Result.GetJobDetail(jobKey);
        if (job == null)
        {
            return noTask;
        }
        //info.Append(string.Format("任务ID:{0}\r\n任务名称:{1}\r\n", job.Key.Name, job.Description)); 
        var triggers = await this._scheduler.Result.GetTriggersOfJob(jobKey);
        if (triggers == null || triggers.Count == 0)
            return noTask;
        foreach (var trigger in triggers)
        {
            var triggerStaus = await this._scheduler.Result.GetTriggerState(trigger.Key);
            string state = GetTriggerState(triggerStaus.ToString());
            ls.Add(new TaskInfoDto
            {
                jobId = job.Key.Name,
                jobGroup = job.Key.Group,
                triggerId = trigger.Key.Name,
                triggerGroup = trigger.Key.Group,
                triggerStatus = state
            });
            //info.Append(string.Format("触发器ID:{0}\r\n触发器名称:{1}\r\n状态:{2}\r\n", item.Key.Name, item.Description, state));

        }
        return ls;
    }
    public string GetTriggerState(string key)
    {
        string state = null;
        if (key != null)
            key = key.ToUpper();
        switch (key)
        {
            case "1":
                state = "暂停";
                break;
            case "2":
                state = "完成";
                break;
            case "3":
                state = "出错";
                break;
            case "4":
                state = "阻塞";
                break;
            case "0":
                state = "正常";
                break;
            case "-1":
                state = "不存在";
                break;
            case "BLOCKED":
                state = "阻塞";
                break;
            case "COMPLETE":
                state = "完成";
                break;
            case "ERROR":
                state = "出错";
                break;
            case "NONE":
                state = "不存在";
                break;
            case "NORMAL":
                state = "正常";
                break;
            case "PAUSED":
                state = "暂停";
                break;
        }
        return state;
    }
    #endregion

    #region 创建触发器帮助方法

    /// <summary>
    /// 创建SimpleTrigger触发器（简单触发器）
    /// </summary>
    /// <param name="sysSchedule"></param>
    /// <param name="starRunTime"></param>
    /// <param name="endRunTime"></param>
    /// <returns></returns>
    private ITrigger CreateSimpleTrigger(TasksQz sysSchedule)
    {
        if (sysSchedule.CycleRunTimes > 0)
        {
            var trigger = TriggerBuilder.Create()
            .WithIdentity(sysSchedule.Id.ToString(), sysSchedule.JobGroup)
            .StartAt(sysSchedule.BeginTime.Value)
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(sysSchedule.IntervalSecond)
                .WithRepeatCount(sysSchedule.CycleRunTimes - 1))
            .EndAt(sysSchedule.EndTime.Value)
            .Build();
            return trigger;
        }
        else
        {
            var trigger = TriggerBuilder.Create()
            .WithIdentity(sysSchedule.Id.ToString(), sysSchedule.JobGroup)
            .StartAt(sysSchedule.BeginTime.Value)
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(sysSchedule.IntervalSecond)
                .RepeatForever()
            )
            .EndAt(sysSchedule.EndTime.Value)
            .Build();
            return trigger;
        }
        // 触发作业立即运行，然后每10秒重复一次，无限循环

    }
    /// <summary>
    /// 创建类型Cron的触发器
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private ITrigger CreateCronTrigger(TasksQz sysSchedule)
    {
        // 作业触发器
        return TriggerBuilder.Create()
               .WithIdentity(sysSchedule.Id.ToString(), sysSchedule.JobGroup)
               .StartAt(sysSchedule.BeginTime.Value)//开始时间
               .EndAt(sysSchedule.EndTime.Value)//结束数据
               .WithCronSchedule(sysSchedule.Cron)//指定cron表达式
               .ForJob(sysSchedule.Id.ToString(), sysSchedule.JobGroup)//作业名称
               .Build();
    }
    #endregion


    /// <summary>
    /// 立即执行 一个任务
    /// </summary>
    /// <param name="tasksQz"></param>
    /// <returns></returns>
    public async Task<ServiceResult<string>> ExecuteJobAsync(TasksQz tasksQz)
    {
        var result = new ServiceResult<string>();
        try
        {
            var jobKey = new JobKey(tasksQz.Id.ToString(), tasksQz.JobGroup);

            //判断任务是否存在，存在则 触发一次，不存在则先添加一个任务，触发以后再 停止任务
            if (!await _scheduler.Result.CheckExists(jobKey))
            {
                //不存在 则 添加一个计划任务
                await AddScheduleJobAsync(tasksQz);

                //触发执行一次
                await _scheduler.Result.TriggerJob(jobKey);

                //停止任务
                await StopScheduleJobAsync(tasksQz);

                result.Success = true;
                result.Message = $"立即执行计划任务:【{tasksQz.Name}】成功";
            }
            else
            {
                await _scheduler.Result.TriggerJob(jobKey);
                result.Success = true;
                result.Message = $"立即执行计划任务:【{tasksQz.Name}】成功";
            }
        }
        catch (Exception ex)
        {
            result.Message = $"立即执行计划任务失败:【{ex.Message}】";
        }

        return result;
    }

    #region 初始化任务
    /// <summary>
    /// 初始化任务
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<string>> InitJobAsync()
    {
        var result = new ServiceResult<string>();
        try
        {
            Logger.WriteLog($"开始初始化任务！");

            var systemTaskItems = await TaskHelper.GetSmQuartzJobsAsync();

            var jobs = systemTaskItems
                .Where(o => !string.IsNullOrEmpty(o.ClassName) && o.Status == JobConsts.TASK_RUN_STATE_READY)
                .Select(o => new TasksQz()
                {
                    Id = o.ID,
                    Name = o.JobName,
                    JobGroup = "JOB",
                    AssemblyName = "EU.Core.Tasks",
                    ClassName = o.ClassName,
                    Cron = o.ScheduleRule,
                    TriggerType = 1
                }).ToList();
            foreach (var item in jobs)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(item.ClassName))
                        continue;
                    var curTime = DateTime.UtcNow.AddHours(8);
                    var expression = new CronExpression(item.Cron);
                    expression.TimeZone = TimeZoneInfo.Utc;
                    var m_NextTime = expression?.GetNextValidTimeAfter(curTime).Value.DateTime;

                    var du = new DbUpdate("SmQuartzJob");
                    du.IsInitDefaultValue = false;
                    du.Set("NextExecuteTime", m_NextTime);
                    du.Where("ID", "=", item.Id);
                    DBHelper.ExecuteScalar(du.GetSql());

                    //var m_Lefttime = (int)((m_NextTime ?? curTime.AddSeconds(-1)) - curTime).TotalSeconds;

                    var result1 = AddScheduleJobAsync(item).Result;
                    if (result1.Success)
                        Logger.WriteLog($"{item.Name} 启动成功！，下次执行时间：{DateTimeHelper.ConvertToSecondString(m_NextTime)}");
                    else
                        Logger.WriteLog($"{item.Name}启动失败！错误信息：{result1.Message}");
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"{item.Name}启动失败！错误信息：{ex.Message}");
                    result.Message = $"初始化计划任务失败:【{ex.Message}】";
                }
            }
            Logger.WriteLog($"初始化任务结束！");

            result.Message = $"初始化计划任务成功！";
        }
        catch (Exception ex)
        {
            result.Message = $"初始化计划任务失败:【{ex.Message}】";
        }

        return result;
    }
    #endregion
}

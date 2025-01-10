namespace EU.Core.Tasks;

public class JobBase
{
    public ISmQuartzJobServices _taskServices;
    public ISmQuartzJobLogServices _taskLogServices;
    public string jobCode;
    public string jobName;
    public JobBase(ISmQuartzJobServices tasksQzServices, ISmQuartzJobLogServices tasksLogServices)
    {
        _taskServices = tasksQzServices;
        _taskLogServices = tasksLogServices;

        //加载启用状态
        //lock (TaskHelper.m_ClientQuartzs)
        //{
        //    //if (!TaskHelper.m_ClientQuartzs.Any(o => o.m_Code == code))
        //    TaskHelper.m_ClientQuartzs.Add(this);
        //}
    }
    /// <summary>
    /// 执行指定任务
    /// </summary>
    /// <param name="context"></param>
    /// <param name="action"></param>
    public async Task<string> ExecuteJob(IJobExecutionContext context, Func<Task> func)
    {
        //记录Job
        SmQuartzJobLog tasksLog = new();
        //JOBID
        var jobid = context.JobDetail.Key.Name.ObjToGuid();
        //JOB组名
        string groupName = context.JobDetail.Key.Group;
        var model = await _taskServices.QueryById(jobid);
        var jobPars = context.JobDetail.JobDataMap;
        jobCode = model.JobCode;
        jobName = model.JobName;

        //日志
        tasksLog.SmQuartzJobId = jobid;
        tasksLog.BeginTime = Utility.GetSysDate();
        string jobHistory = $"【任务：{jobName}，组别：{groupName}】【{tasksLog.BeginTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}】【执行开始】";
        try
        {
            await func();//执行任务
            tasksLog.EndTime = Utility.GetSysDate();
            tasksLog.ExecuteResult = true;
            jobHistory += $"，【{tasksLog.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}】【执行成功】";

            //tasksLog.RunPars = jobPars.GetString("JobParam");
        }
        catch (Exception ex)
        {
            tasksLog.EndTime = Utility.GetSysDate();
            tasksLog.ExecuteResult = false;
            //JobExecutionException e2 = new JobExecutionException(ex);
            //true  是立即重新执行任务 
            //e2.RefireImmediately = true;
            tasksLog.ErrMessage = ex.Message;
            tasksLog.ErrStackTrace = ex.StackTrace;
            jobHistory += $"，【{tasksLog.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}】【执行失败:{ex.Message}】";
        }
        finally
        {
            tasksLog.TotalTime = (tasksLog.EndTime.Value - tasksLog.BeginTime.Value).TotalSeconds.ObjToDecimal();
            jobHistory += $"(耗时:{tasksLog.TotalTime}秒)";
            var curTime = DateTime.UtcNow.AddHours(8);
            CronExpression expression = new(model.ScheduleRule);
            expression.TimeZone = TimeZoneInfo.Utc;
            var m_NextTime = expression?.GetNextValidTimeAfter(curTime).Value.DateTime;
            //var m_Lefttime = (int)((m_NextTime ?? curTime.AddSeconds(-1)) - curTime).TotalSeconds;
            jobHistory += $"，下次执行时间：{DateTimeHelper.ConvertToSecondString(m_NextTime)}";
            tasksLog.Remark = jobHistory;
            if (_taskServices != null)
            {
                if (model != null)
                {
                    //if(_taskLogServices != null) await _taskLogServices.Add(tasksLog);
                    //model.RunTimes += 1;
                    //if (model.TriggerType == 0) model.CycleHasRunTimes += 1;
                    //if (model.TriggerType == 0 && model.CycleRunTimes != 0 && model.CycleHasRunTimes >= model.CycleRunTimes) model.IsStart = false;//循环完善,当循环任务完成后,停止该任务,防止下次启动再次执行
                    //var separator = "<br>";
                    // 这里注意数据库字段的长度问题，超过限制，会造成数据库remark不更新问题。
                    model.LastResult = jobHistory;
                    model.NextExecuteTime = m_NextTime;
                    model.LastExecuteTime = tasksLog.EndTime;
                    model.LastCost = tasksLog.TotalTime;
                    await _taskServices.Update(jobid.Value, JsonHelper.ObjToJson(model), ["LastResult", "LastExecuteTime", "NextExecuteTime", "LastCost"]);
                }
            }
            await _taskLogServices.Add(tasksLog);
        }
        SendLog(jobHistory);
        return jobHistory;
    }


    #region 当前日志
    /// <summary>
    /// 提供最新的200条日志记录
    /// </summary>
    /// <summary>
    /// 日志
    /// </summary>
    /// <param name="msg"></param>
    public void SendLog(string msg)
    {
        TaskHelper.AddQuartzLog(jobCode, msg, false);
        Logger.WriteLog($"[{jobName}] {msg}");
    }
    #endregion
}

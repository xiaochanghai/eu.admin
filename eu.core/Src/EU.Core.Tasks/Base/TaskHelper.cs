using EU.Core.Common.Const;
using EU.Core.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Text;

namespace EU.Core.Tasks;

/// <summary>
/// 任务管理
/// </summary>
public class TaskHelper
{
    #region 客户端列表信息(Job和Adapter在任务启动时存储)

    /// <summary>
    /// 客户端信息
    /// </summary>
    public static List<JobBase> m_ClientQuartzs = new();

    /// <summary>
    /// 客户端日志
    /// </summary>
    public static Dictionary<string, List<string>> m_ClientQuartzLogs = new();


    /// <summary>
    /// 客户端信息
    /// </summary>
    public static List<SmQuartzJob> m_SmQuartzJob = new();
    #endregion

    #region 接收并消息处理(Job和Adapter使用)

    /// <summary>
    /// 接收并消息处理
    /// </summary>
    /// <param name="o"></param>
    public static async void TaskHandleAsync(object o)
    {
        var msg = o as TaskMsg;
        try
        {
            Logger.WriteLog($"[Task]接收到消息：{msg.ToString()}");
            //var client = m_Clients.Where(c => c.m_Code == msg.TaskCode).FirstOrDefault();
            var taskItem = await GetSmQuartzJob(msg.TaskId);
            if (taskItem == null && msg.Oprate != JobConsts.TASK_OPERATE_INIT_TENANR_TASK)
            {
                Logger.WriteLog($"[Task]消息处理异常,任务编码不存在：{msg.TaskCode}");
                return;
            }

            var container = new ServiceCollection();
            container.AddJobSetup();

            var sp = container.BuildServiceProvider();
            var _schedulerCenter = sp.GetService<ISchedulerCenter>();

            //初始化租户任务
            if (msg.Oprate == JobConsts.TASK_OPERATE_INIT_TENANR_TASK)
            {
                Logger.WriteLog("收到消息，初始化租户任务开始");
                var items = await GetSmQuartzJobsAsync();
                var jobs = items
               .Where(o => !string.IsNullOrEmpty(o.ClassName))
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
                    var result1 = _schedulerCenter.AddScheduleJobAsync(item).Result;
                    if (result1.Success)
                        Logger.WriteLog($"QuartzNetJob {item.Name} 启动成功！");
                    else
                        Logger.WriteLog($"QuartzNetJob {item.Name}启动失败！错误信息：{result1.Message}");
                }

                Logger.WriteLog("收到消息，初始化任务完成");
            }
            else
            {
                var qz = new TasksQz()
                {
                    Id = taskItem.ID,
                    Name = taskItem.JobName,
                    JobGroup = "JOB",
                    AssemblyName = "EU.Core.Tasks",
                    ClassName = taskItem.ClassName,
                    Cron = taskItem.ScheduleRule,
                    TriggerType = 1
                };

                switch (msg.Oprate)
                {
                    //修改参数
                    case JobConsts.TASK_OPERATE_ARGS:
                        {
                            AddQuartzLog(msg.TaskCode, "收到消息，修改参数");
                            EditAgrs(msg, msg.TaskId);
                            qz.Cron = msg.Args;
                            var ResuleModel = await _schedulerCenter.StopScheduleJobAsync(qz);
                            if (ResuleModel.Success)
                            {
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>停止成功=>{ResuleModel.Message}");
                                var result1 = await _schedulerCenter.AddScheduleJobAsync(qz);
                                if (result1.Success)
                                    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>启动成功=>{result1.Message}");
                                else
                                {
                                    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>启动失败=>{result1.Message}");
                                    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>重新启动=>{result1.Message}");
                                    result1 = await _schedulerCenter.AddScheduleJobAsync(qz);
                                }
                            }
                            else
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>停止失败=>{ResuleModel.Message}");
                            var curTime = DateTime.UtcNow.AddHours(8);
                            var expression = new CronExpression(msg.Args);
                            expression.TimeZone = TimeZoneInfo.Utc;
                            var m_NextTime = expression?.GetNextValidTimeAfter(curTime).Value.DateTime;
                            AddQuartzLog(msg.TaskCode, $"任务ID：【{msg.TaskId}】,任务代码：【{msg.TaskCode}】，变更后参数：【{msg.Args}】，下次执行时间：{DateTimeHelper.ConvertToSecondString(m_NextTime)}");
                            AddQuartzLog(msg.TaskCode, "消息处理完毕，参数已修改");
                            AddQuartzLog(msg.TaskCode, "配置文件上报完毕");
                            break;
                        }
                    //修改配置
                    case JobConsts.TASK_OPERATE_CONF:
                        {
                            //client.SendLog("收到消息，获取配置文件");
                            //RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, new TaskCallbackMsg
                            //{
                            //    MsgId = msg.MsgId,
                            //    Content = client.GetConfigFiles()
                            //});
                            //client.SendLog("配置文件上报完毕");
                            AddQuartzLog(msg.TaskCode, "收到消息，获取配置文件");
                            var list = new List<string>();
                            try
                            {
                                var directory = new DirectoryInfo($"{AppDomain.CurrentDomain.BaseDirectory}");
                                directory.GetFiles()
                                     .ToList()
                                     .OrderBy(o => o.CreationTime)
                                     .ToList()
                                     .ForEach(fname =>
                                     {
                                         if (fname.Name != "appsettings.json" && fname.Name != "Adapter.json" && fname.Name != "OtherDb.json")
                                             return;

                                         list.Add(fname.Name);
                                     });
                                RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK,
                                    new TaskCallbackMsg
                                    {
                                        MsgId = msg.MsgId,
                                        Content = list
                                    });
                            }
                            catch (Exception ex)
                            {
                                AddQuartzLog(msg.TaskCode, $"获取配置文件列表失败：{ex.Message}");
                            }
                            AddQuartzLog(msg.TaskCode, "配置文件上报完毕");
                            break;
                        }
                    //修改配置 - 上传配置文件
                    case JobConsts.TASK_OPERATE_CONF_FILE_UPLOAD:
                        {
                            AddQuartzLog(msg.TaskCode, $"收到消息，获取配置文件 {msg.Args}");
                            string content = string.Empty;
                            string fname = $"{AppDomain.CurrentDomain.BaseDirectory}{Path.DirectorySeparatorChar}{msg.Args}";
                            if (File.Exists(fname))
                            {
                                using (StreamReader sr = new StreamReader(fname, Encoding.UTF8))
                                {
                                    content = sr.ReadToEnd();
                                }
                            }
                            RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, new TaskCallbackMsg
                            {
                                MsgId = msg.MsgId,
                                Content = content
                            });
                            AddQuartzLog(msg.TaskCode, $"配置文件 {msg.Args} 上报完毕");
                            break;
                        }
                    //修改配置 - 保存配置文件
                    case JobConsts.TASK_OPERATE_CONF_FILE_SAVE:
                        {
                            AddQuartzLog(msg.TaskCode, $"收到消息，保存配置文件 {msg.Args}");
                            string fname = $"{AppDomain.CurrentDomain.BaseDirectory}{Path.DirectorySeparatorChar}{msg.Args}";
                            if (File.Exists(fname))
                            {
                                using (StreamWriter sr = new StreamWriter(fname, false, Encoding.UTF8))
                                {
                                    sr.Write(msg.Content);
                                }
                            }
                            AddQuartzLog(msg.TaskCode, $"配置文件 {msg.Args} 保存完毕");
                            break;
                        }
                    //立即执行
                    case JobConsts.TASK_OPERATE_START:
                        {
                            var ResuleModel = await _schedulerCenter.ExecuteJobAsync(qz);
                            if (ResuleModel.Success)
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>执行成功=>{ResuleModel.Message}");
                            else
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>执行失败=>{ResuleModel.Message}");
                            //new System.Threading.Thread(client.ExecOnce).Start();
                            break;
                        }
                    //停止任务
                    case JobConsts.TASK_OPERATE_STOP:
                        {
                            //client.ChangeStop();
                            AddQuartzLog(msg.TaskCode, "收到消息，停止任务");
                            OprateTask(JobConsts.TASK_RUN_STATE_STOP, msg.TaskId);
                            var ResuleModel = await _schedulerCenter.PauseJob(qz);
                            if (ResuleModel.Success)
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>暂停成功=>{ResuleModel.Message}");
                            else
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>暂停失败=>{ResuleModel.Message}");

                            AddQuartzLog(msg.TaskCode, "消息处理完毕，任务已停止");
                            break;
                        }
                    //启用任务
                    case JobConsts.TASK_OPERATE_ENABLE:
                        {
                            AddQuartzLog(msg.TaskCode, "收到消息，启用任务");
                            OprateTask(JobConsts.TASK_RUN_STATE_READY, msg.TaskId);
                            var ResuleModel = await _schedulerCenter.ResumeJob(qz);
                            if (ResuleModel.Success)
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>启动成功=>{ResuleModel.Message}");
                            else
                            {
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>启动失败=>{ResuleModel.Message}");
                                ResuleModel = await _schedulerCenter.AddScheduleJobAsync(qz);
                                if (ResuleModel.Success)
                                    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>启动成功=>{ResuleModel.Message}");
                                else
                                    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>启动失败=>{ResuleModel.Message}");
                            }

                            AddQuartzLog(msg.TaskCode, "消息处理完毕，任务已启用");
                            break;
                        }
                    //禁用任务
                    case JobConsts.TASK_OPERATE_DISABLED:
                        {
                            AddQuartzLog(msg.TaskCode, "收到消息，停止任务");
                            //client.Disabled();
                            OprateTask(JobConsts.TASK_RUN_STATE_DISABLED, msg.TaskId);

                            var ResuleModel = await _schedulerCenter.StopScheduleJobAsync(qz);
                            if (ResuleModel.Success)
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>停止成功=>{ResuleModel.Message}");
                            else
                                AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>停止失败=>{ResuleModel.Message}");

                            AddQuartzLog(msg.TaskCode, "消息处理完毕，任务已停止");
                            break;
                        }
                    //当前日志
                    case JobConsts.TASK_OPERATE_LOG_CURRENT:
                        {
                            AddQuartzLog(msg.TaskCode, "收到消息，获取当前日志");
                            var m_Logs = new List<string>();
                            if (m_ClientQuartzLogs.ContainsKey(msg.TaskCode))
                                m_Logs = m_ClientQuartzLogs[msg.TaskCode];
                            var flag = RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, new TaskCallbackMsg
                            {
                                MsgId = msg.MsgId,
                                Content = string.Join("<br />", m_Logs)
                            });
                            AddQuartzLog(msg.TaskCode, "当前日志上报完毕:" + flag);
                            break;
                        }
                    //历史日志
                    case JobConsts.TASK_OPERATE_LOG_HISTORY:
                        {
                            //client.SendLog("收到消息，获取历史日志");
                            //RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, new TaskCallbackMsg
                            //{
                            //    MsgId = msg.MsgId,
                            //    Content = client.GetLogFiles()
                            //});
                            //client.SendLog("历史日志上报完毕");

                            AddQuartzLog(msg.TaskCode, "收到消息，获取历史日志");
                            var list = new List<string>();
                            try
                            {
                                System.IO.DirectoryInfo directory = new DirectoryInfo($"{AppDomain.CurrentDomain.BaseDirectory}Logs");
                                directory.GetFiles()
                                     .ToList()
                                     .OrderBy(o => o.CreationTime)
                                     .ToList()
                                     .ForEach(fname =>
                                     {
                                         list.Add(fname.Name);
                                     });
                            }
                            catch (Exception ex)
                            {
                                AddQuartzLog(msg.TaskCode, $"获取日志文件列表失败：{ex.Message}");
                            }

                            RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, new TaskCallbackMsg
                            {
                                MsgId = msg.MsgId,
                                Content = list
                            });
                            AddQuartzLog(msg.TaskCode, "历史日志上报完毕");

                            break;
                        }
                    //历史日志文件下载
                    case JobConsts.TASK_OPERATE_LOG_HISTORY_FILE:
                        {
                            AddQuartzLog(msg.TaskCode, $"收到消息，获取历史日志 {msg.Args}");
                            var m_Logs = new List<string>();
                            if (m_ClientQuartzLogs.ContainsKey(msg.TaskCode))
                                m_Logs = m_ClientQuartzLogs[msg.TaskCode];

                            var sb = new StringBuilder();
                            //string fname = $"{AppDomain.CurrentDomain.BaseDirectory}Logs{System.IO.Path.DirectorySeparatorChar}{msg.Args}";
                            //if (File.Exists(fname))
                            //{
                            //    var logstart = $"[{taskItem.JobName}] ";
                            //    using (StreamReader sr = new StreamReader(fname, System.Text.Encoding.UTF8))
                            //    {
                            //        var content = sr.ReadLine();
                            //        if (content.StartsWith(logstart))
                            //        {
                            //            sb.AppendLine(content.Substring(logstart.Length + 1));
                            //        }
                            //    }
                            //}
                            m_Logs.ForEach(l => sb.AppendLine(l));
                            RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_CALLBACK, new TaskCallbackMsg
                            {
                                MsgId = msg.MsgId,
                                Content = sb.ToString()
                            });
                            AddQuartzLog(msg.TaskCode, $"历史日志 {msg.Args} 上报完毕");
                            break;
                        }

                    ////初始化租户任务
                    //case JobConsts.TASK_OPERATE_INIT_TENANR_TASK:
                    //    {
                    //         Logger.WriteLog("收到消息，停止任务");
                    //        //client.Disabled();
                    //        //OprateTask(JobConsts.TASK_RUN_STATE_DISABLED, msg.TaskId);

                    //        //var ResuleModel = await _schedulerCenter.StopScheduleJobAsync(qz);
                    //        //if (ResuleModel.Success)
                    //        //    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>停止成功=>{ResuleModel.Message}");
                    //        //else
                    //        //    AddQuartzLog(msg.TaskCode, $"{msg.TaskCode}=>停止失败=>{ResuleModel.Message}");

                    //        AddQuartzLog(msg.TaskCode, "消息处理完毕，任务已停止");
                    //        break;
                    //    }
                    default:
                        Logger.WriteLog($"[Task]消息处理异常,操作指令不存在：{msg.Oprate}");
                        break;
                }
            }
            Logger.WriteLog($"[Task]消息处理完毕：{msg.MsgId}");
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"[Task]消息处理异常：{msg.MsgId} {ex.ToString()}");
        }
    }
    #endregion

    //#region 处理Task消息回传(WebApi使用)
    ///// <summary>
    ///// 直接发送消息不等待消息回传
    ///// </summary>
    ///// <param name="msg"></param>
    ///// <returns></returns>

    public static void PostMsg(TaskMsg msg)
    {
        lock (m_MsgClients)
        {
            m_MsgClients.Add(msg.MsgId);
        }
        switch (msg.TaskType)
        {
            case JobConsts.TASK_TYPE_JOB:
                {
                    RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_JOB, msg);
                    break;
                }
            default:
                Logger.WriteLog($"发送Task消息失败，不支持的任务类型 {msg.TaskType}");
                return;
        }
        Logger.WriteLog($"发送Task消息成功 {msg.ToString()}");
    }
    /// <summary>
    /// 发送消息并等待消息回传
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>

    public static (bool, object) SendMsg(TaskMsg msg)
    {
        bool suc = false;
        //发送消息
        PostMsg(msg);

        #region  等待消息回传
        int timeout = 5;
        Logger.WriteLog($"等待Task回传消息,默认等待时长 {timeout} s");

        //每次休息 100ms
        int sum = 10 * timeout;
        object o = "操作超时，请稍后再试";
        while (true)
        {
            lock (m_MsgClients)
            {
                if (m_TaskMsg.ContainsKey(msg.MsgId))
                {
                    o = m_TaskMsg[msg.MsgId];
                    suc = true;
                    break;
                }
            }

            Thread.Sleep(100);

            if (sum-- <= 0)
            {
                //超时
                lock (m_MsgTimeoutClients)
                {
                    if (!m_MsgTimeoutClients.Contains(msg.MsgId))
                    {
                        m_MsgTimeoutClients.Add(msg.MsgId);
                    }
                }
                Logger.WriteLog($"操作超时，请稍后再试\"");
                break;
            }
        }

        //移除客户端
        lock (m_MsgClients)
        {
            if (m_TaskMsg.ContainsKey(msg.MsgId))
                m_TaskMsg.Remove(msg.MsgId);

            if (m_MsgClients.Contains(msg.MsgId))
                m_MsgClients.Remove(msg.MsgId);
        }
        #endregion

        return (suc, o);
    }

    private static List<Guid> m_MsgClients = new List<Guid>();
    private static Dictionary<Guid, object> m_TaskMsg = new Dictionary<Guid, object>();
    private static List<Guid> m_MsgTimeoutClients = new List<Guid>();
    private static Dictionary<string, object> m_TimeoutTaskMsg = new Dictionary<string, object>();
    /// <summary>
    /// 监听Task回传消息
    /// </summary>
    /// <param name="o"></param>
    public static void TaskCallback(object o)
    {
        var msg = o as TaskCallbackMsg;
        try
        {
            //查看是否是超时的消息，直接丢弃
            lock (m_MsgTimeoutClients)
            {
                if (m_MsgTimeoutClients.Contains(msg.MsgId))
                {
                    m_MsgTimeoutClients.Remove(msg.MsgId);
                    Logger.WriteLog($"接收到Task回传消息成功，识别[MsgId] {msg.MsgId} 超时，直接丢弃");
                    return;
                }
            }

            lock (m_MsgClients)
            {
                if (m_MsgClients.Contains(msg.MsgId))
                {
                    if (!m_TaskMsg.ContainsKey(msg.MsgId))
                        m_TaskMsg.Add(msg.MsgId, msg.Content);
                    else
                        m_TaskMsg[msg.MsgId] = msg.Content;
                    Logger.WriteLog($"接收到Task回传消息成功，识别[MsgId] {msg.MsgId} 成功");
                }
                else
                    Logger.WriteLog($"接收到Task回传消息成功，识别[MsgId] {msg.MsgId} 失败，丢弃");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"接收到Task回传消息成功，消息处理异常{ex}");
        }
    }

    public static void AddQuartzLog(string code, string msg, bool writeLog = true)
    {
        var m_Logs = new List<string>();

        if (m_ClientQuartzLogs.ContainsKey(code))
            m_Logs = m_ClientQuartzLogs[code];

        m_Logs.Add($"[{Utility.GetSysDate().ToString("yyyy-MM-dd HH:mm:ss")}] " + msg);

        if (m_Logs.Count > 200)
            m_Logs.RemoveRange(0, 50);
        lock (m_ClientQuartzLogs)
        {
            if (m_ClientQuartzLogs.ContainsKey(code))
                m_ClientQuartzLogs[code] = m_Logs;
            else
                m_ClientQuartzLogs.Add(code, m_Logs);
        }
        if (writeLog)
            Logger.WriteLog($"任务【{code}】{msg}");
    }

    public static async Task<List<SmQuartzJob>> GetSmQuartzJobsAsync()
    {
        string sql = @"SELECT A.ID,
                               A.CreatedBy,
                               A.CreatedTime,
                               A.UpdateBy,
                               A.UpdateTime,
                               A.ImportDataId,
                               A.ModificationNum,
                               A.Tag,
                               A.GroupId,
                               A.CompanyId,
                               A.JobCode,
                               A.JobName,
                               A.ClassName,
                               A.ScheduleRule,
                               A.Status,
                               A.LastExecuteTime,
                               A.LastCost,
                               A.NextExecuteTime,
                               A.IsUpdate,
                               A.AuditStatus,
                               A.Remark,
                               A.IsActive,
                               A.IsDeleted,
                               A.CurrentNode
                        FROM SmQuartzJob A
                        WHERE A.IsDeleted = 'false' AND A.IsActive = 'true'";
        return await DBHelper.QueryListAsync<SmQuartzJob>(sql);
    }
    public static async Task<SmQuartzJob> GetSmQuartzJob(Guid taskId)
    {
        var item = m_SmQuartzJob.Where(o => o.ID == taskId).FirstOrDefault();

        if (item == null)
            item = (await GetSmQuartzJobsAsync()).Where(o => o.ID == taskId).FirstOrDefault();
        return item;
    }

    /// <summary>
    /// 禁用任务/启用任务
    /// </summary>
    private static void OprateTask(string status, Guid taskId)
    {
        using var _context = ContextFactory.CreateContext();
        var task = _context.SmQuartzJob.Where(x => x.ID == taskId).FirstOrDefault();
        if (task != null)
        {
            task.Status = status;
            _context.SmQuartzJob.Update(task);
            _context.SaveChanges();
            Logger.WriteLog("消息处理完毕，状态已修改");
        }
        else
            Logger.WriteLog("任务id不存在！");
    }
    public static void EditAgrs(TaskMsg msg, Guid taskId)
    {
        using var _context = ContextFactory.CreateContext();
        var task = _context.SmQuartzJob.Where(x => x.ID == taskId).FirstOrDefault();
        if (task != null)
        {
            task.ScheduleRule = msg.Args;
            var curTime = DateTime.UtcNow.AddHours(8);
            var expression = new CronExpression(msg.Args);
            expression.TimeZone = TimeZoneInfo.Utc;
            var m_NextTime = expression?.GetNextValidTimeAfter(curTime).Value.DateTime;
            task.NextExecuteTime = m_NextTime;
            _context.SmQuartzJob.Update(task);
            _context.SaveChanges();
            AddQuartzLog(msg.TaskCode, $"消息处理完毕，参数已修改，下次执行时间：{DateTimeHelper.ConvertToSecondString(m_NextTime)}");
        }
        else
            AddQuartzLog(msg.TaskCode, "无效的任务id");

    }
}

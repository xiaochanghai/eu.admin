using EU.Core.Common.Extensions;
using EU.Core.Common.Helper;

namespace EU.Core.Common.LogHelper;

/// <summary>
/// 通过内置队列异步定时写日志
/// </summary>
public static class Logger
{

    public static string CurrentPath { get; private set; } = null;
    public static readonly object _logger = new object();
    private static DateTime lastClearFileDT = DateTime.Now.AddDays(-1);
    private static string _loggerPath
    {
        get
        {
            return CurrentPath + "\\Log\\Queue\\";
        }
    }
    private static string _logPath { get { return CurrentPath + "\\Log\\"; } }
    static Logger()
    {
        var baseDirectory = AppContext.BaseDirectory;
        CurrentPath = Path.Combine(baseDirectory, "").ReplacePath();
        Task.Run(() => { Start(); });
    }

    //public static void Info(string message)
    //{
    //    Info(LoggerType.Info, message);
    //}
    //public static void Info(LoggerType loggerType, string message = null)
    //{
    //    Info(loggerType, message, null, null);
    //}
    //public static void Info(LoggerType loggerType, string requestParam, string resposeParam, string ex = null)
    //{
    //    Add(loggerType, requestParam, resposeParam, ex, LoggerStatus.Info);
    //}

    //public static void OK(string message)
    //{
    //    OK(LoggerType.Success, message);
    //}
    //public static void OK(LoggerType loggerType, string message = null)
    //{
    //    OK(loggerType, message, null, null);
    //}
    //public static void OK(LoggerType loggerType, string requestParam, string resposeParam, string ex = null)
    //{
    //    Add(loggerType, requestParam, resposeParam, ex, LoggerStatus.Success);
    //}
    //public static void Error(string message)
    //{
    //    Error(LoggerType.Error, message);
    //}
    //public static void Error(LoggerType loggerType, string message)
    //{
    //    Error(loggerType, message, null, null);
    //}
    //public static void Error(LoggerType loggerType, string requestParam, string resposeParam, string ex = null)
    //{
    //    Add(loggerType, requestParam, resposeParam, ex, LoggerStatus.Error);
    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="requestParameter">请求参数</param>
    /// <param name="responseParameter">响应参数</param>
    /// <param name="success">响应结果1、成功,2、异常，0、其他</param>
    /// <param name="userInfo">用户数据</param>
    //private static void Add(LoggerType loggerType, string requestParameter, string responseParameter, string ex, LoggerStatus status)
    //{
    //    Sys_Log log = null;
    //    try
    //    {
    //        HttpContext context = Utilities.HttpContext.Current;
    //        if (context.Request.Method == "OPTIONS") return;
    //        ActionObserver cctionObserver = (context.RequestServices.GetService(typeof(ActionObserver)) as ActionObserver);

    //        //如果当前请求已经写过日志就不再写日志
    //        //if (cctionObserver.IsWrite) return;
    //        //cctionObserver.IsWrite = true;


    //        if (context == null)
    //        {
    //            WriteText($"未获取到httpcontext信息,type:{loggerType.ToString()},reqParam:{requestParameter},respParam:{responseParameter},ex:{ex},success:{status.ToString()}");
    //            return;
    //        }

    //        UserInfo userInfo = UserContext.Current.UserInfo;

    //        log = new Sys_Log()
    //        {
    //            BeginDate = cctionObserver.RequestDate,
    //            EndDate = DateTime.Now,
    //            User_Id = userInfo.User_Id,
    //            UserName = userInfo.UserTrueName,
    //            Role_Id = userInfo.Role_Id,
    //            LogType = loggerType.ToString(),
    //            ExceptionInfo = ex,
    //            RequestParameter = requestParameter,
    //            ResponseParameter = responseParameter,
    //            Success = (int)status
    //        };
    //        SetServicesInfo(log, context);
    //    }
    //    catch (Exception exception)
    //    {
    //        log = log ?? new Sys_Log();
    //        log.ExceptionInfo = exception.Message;
    //    }
    //    if (log == null) return;
    //    lock (_logger)
    //    {
    //        loggerQueueData.Enqueue(log);
    //    }
    //}

    private static void Start()
    {
        //DataTable queueTable = CreateEmptyTable();
        while (true)
        {
            try
            {
                //if (loggerQueueData.Count() > 0 && queueTable.Rows.Count < 500)
                //{
                //    DequeueToTable(queueTable); continue;
                //}
                //每5秒写一次数据
                Thread.Sleep(5000);
                //if (queueTable.Rows.Count == 0) { continue; }

                //DBServerProvider.SqlDapper.BulkInsert(queueTable, "Sys_Log", SqlBulkCopyOptions.KeepIdentity, null, _loggerPath);

                //queueTable.Clear();

                if ((DateTime.Now - lastClearFileDT).TotalDays > 1)
                {
                    FileHelper.DeleteFolder(_loggerPath);
                    lastClearFileDT = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志批量写入数据时出错:{ex.Message}");
                WriteText(ex.Message + ex.StackTrace + ex.Source);
                //queueTable.Clear();
            }

        }

    }

    private static void WriteText(string message)
    {
        try
        {
            FileHelper.WriteFile(_loggerPath + "WriteError\\", $"{DateTime.Now.ToString("yyyyMMdd")}.txt", message + "\r\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志写入文件时出错:{ex.Message}");
        }
    }

    //public static void SetServicesInfo(Sys_Log log, HttpContext context)
    //{
    //    string result = String.Empty;
    //    log.Url = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase +
    //        context.Request.Path;

    //    log.UserIP = context.GetUserIp()?.Replace("::ffff:","");
    //    log.ServiceIP = context.Connection.LocalIpAddress.MapToIPv4().ToString() + ":" + context.Connection.LocalPort;

    //    log.BrowserType = context.Request.Headers["User-Agent"];
    //    if (string.IsNullOrEmpty(log.RequestParameter))
    //    {
    //        try
    //        {
    //            log.RequestParameter = context.GetRequestParameters();
    //            if (log.RequestParameter != null)
    //            {
    //                log.RequestParameter = HttpUtility.UrlDecode(log.RequestParameter, Encoding.UTF8);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            log.ExceptionInfo += $"日志读取参数出错:{ex.Message}";
    //            Console.WriteLine($"日志读取参数出错:{ex.Message}");
    //        }
    //    }
    //}

    #region 记录文件日志
    /// <summary>
    /// 写错误日志(可以指定文件名称，如果不指定，则默认为“日志.txt”)
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="fileName">文件名称,如果不指定，则默认为“日志.txt”</param>
    private static void WriteFileLog(string folder, string fileName, string message, bool isForce)
    {
        try
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {folder} {fileName} {message}");

            string isFileLog = "N";
            isFileLog = "Y";
            //isFileLog = EU.Core.Utilities.ConfigCache.GetValue("IsFileLog");
            if (string.IsNullOrEmpty(isFileLog))
            {
                isFileLog = "N";
            }
            if (isFileLog == "Y")
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName += DateTime.Now.ToString("yyyyMMdd") + ".txt";
                }
                if (isForce == true)
                {
                    FileHelper.WriteFile(_logPath + folder + "\\", fileName, message + "\r\n", true);
                }
                else
                {
                    //判断设置是否需要记录日志
                    FileHelper.WriteFile(_logPath + folder + "\\", fileName, message + "\r\n", true);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 按日期记录文本日志，每天一个文件，如：20171128.txt
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="fileName"></param>
    /// <param name="message"></param>
    /// <param name="isForce">是否强制写日志，不受配置参数影响</param>
    public static void WriteLog(string folder, string fileName, string message, bool isForce)
    {
        try
        {
            WriteFileLog(folder, fileName, message, isForce);
        }
        catch (Exception)
        {
        }
    }


    /// <summary>
    /// 按日期记录文本日志，每天一个文件，如：20171128.txt
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="fileName"></param>
    /// <param name="message"></param>
    public static void WriteLog(string folder, string fileName, string message)
    {
        try
        {
            WriteFileLog(folder, fileName, message, false);
        }
        catch (Exception)
        {

        }
    }
    /// <summary>
    /// 按日期记录文本日志，每天一个文件，如：20171128.txt
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="message"></param>
    public static void WriteLog(string folder, string message)
    {
        try
        {
            WriteFileLog(folder, null, message, false);
        }
        catch (Exception)
        {
        }
    }
    /// <summary>
    /// 按日期记录文本日志，每天一个文件，如：20171128.txt，默认文件夹:Default
    /// </summary>
    /// <param name="message"></param>
    public static void WriteLog(string message)
    {
        try
        {
            WriteFileLog(null, null, message, false);
        }
        catch (Exception)
        {

        }
    }
    #endregion
}

public enum LoggerStatus
{
    Success = 1,
    Error = 2,
    Info = 3
}
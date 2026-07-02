using EU.Core.Common.Extensions;
using EU.Core.Common.Helper;
using EU.Core.Model;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Text;

namespace EU.Core.Common.LogHelper;

public class LogLock : IDisposable
{
    private static readonly ReaderWriterLockSlim _logWriteLock = new();
    private static int _writedCount = 0;
    private static int _failedCount = 0;
    private static string _contentRoot = string.Empty;
    private static bool _disposed = false;

    // 常量定义
    private const string LogFolderName = "Logs";
    private const string DateFormat = "yyyyMMdd";
    private const string LogSeparator = "--------------------------------\r\n";
    private const int MaxLogEntries = 100;
    private const int MaxWeekApis = 5;
    private const int MaxDateEntries = 7;
    private const int MaxHourEntries = 24;

    // 日志配置映射
    private static readonly Dictionary<string, (string Node, string Name)> LogSettingsMap = new()
    {
        ["AOPLog"] = ("AppSettings", "LogAOP"),
        ["AOPLogEx"] = ("AppSettings", "LogAOP"),
        ["RequestIpInfoLog"] = ("Middleware", "IPLog"),
        ["RecordAccessLogs"] = ("Middleware", "RecordAccessLogs"),
        ["SqlLog"] = ("AppSettings", "SqlAOP"),
        ["RequestResponseLog"] = ("Middleware", "RequestResponseLog")
    };

    public LogLock(string contentPath)
    {
        _contentRoot = contentPath;
    }

    public static void OutLogAOP(string prefix, string traceId, string[] dataParas, bool IsHeader = true)
    {
        if (!LogSettingsMap.TryGetValue(prefix, out var settings))
        {
            return;
        }

        var (nodeName, settingName) = settings;

        if (!AppSettings.app(new[] { nodeName, settingName, "Enabled" }).ObjToBool())
        {
            return;
        }

        if (AppSettings.app(new[] { nodeName, settingName, "LogToDB", "Enabled" }).ObjToBool())
        {
            OutSql2LogToDB(prefix, traceId, dataParas, IsHeader);
        }

        if (AppSettings.app(new[] { nodeName, settingName, "LogToFile", "Enabled" }).ObjToBool())
        {
            OutSql2LogToFile(prefix, traceId, dataParas, IsHeader);
        }
    }

    public static void OutSql2LogToFile(string prefix, string traceId, string[] dataParas, bool IsHeader = true, bool isWrt = false)
    {
        try
        {
            _logWriteLock.EnterWriteLock();

            var folderPath = Path.Combine(_contentRoot, LogFolderName, DateTime.Now.ToString(DateFormat));
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var logFilePath = FileHelper.GetAvailableFileWithPrefixOrderSize(folderPath, prefix);

            // 格式化日志内容
            var logContent = FormatLogContent(prefix, dataParas, IsHeader);

            if (isWrt)
            {
                File.WriteAllText(logFilePath, logContent);
            }
            else
            {
                File.AppendAllText(logFilePath, logContent);
            }

            Interlocked.Increment(ref _writedCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志写入失败: {ex.Message}");
            Interlocked.Increment(ref _failedCount);
        }
        finally
        {
            _logWriteLock.ExitWriteLock();
        }
    }

    private static string FormatLogContent(string prefix, string[] dataParas, bool IsHeader)
    {
        string content = string.Empty;

        switch (prefix)
        {
            case "AOPLog":
                if (dataParas.Length > 1)
                {
                    var apiLogAopInfo = JsonConvert.DeserializeObject<AOPLogInfo>(dataParas[1]);
                    content = FormatAOPLogInfo(apiLogAopInfo);
                }
                break;

            case "AOPLogEx":
                if (dataParas.Length > 1)
                {
                    var apiLogAopExInfo = JsonConvert.DeserializeObject<AOPLogExInfo>(dataParas[1]);
                    content = FormatAOPLogExInfo(apiLogAopExInfo);
                }
                break;

            default:
                content = string.Join("\r\n", dataParas);
                break;
        }

        if (IsHeader)
        {
            return $"{LogSeparator}{DateTime.Now}|\r\n{content}\r\n";
        }
        else
        {
            return dataParas.Length > 1 ? $"{dataParas[1]},\r\n" : string.Empty;
        }
    }

    private static string FormatAOPLogInfo(AOPLogInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"【操作时间】：{info.RequestTime}");
        sb.AppendLine($"【当前操作用户】：{info.OpUserName}");
        sb.AppendLine($"【当前执行方法】：{info.RequestMethodName}");
        sb.AppendLine($"【携带的参数有】： {info.RequestParamsName}");
        sb.AppendLine($"【携带的参数JSON】： {info.RequestParamsData}");
        sb.AppendLine($"【响应时间】：{info.ResponseIntervalTime}");
        sb.AppendLine($"【执行完成时间】：{info.ResponseTime}");
        sb.Append($"【执行完成结果】：{info.ResponseJsonData}");
        return sb.ToString();
    }

    private static string FormatAOPLogExInfo(AOPLogExInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"【操作时间】：{info.ApiLogAopInfo.RequestTime}");
        sb.AppendLine($"【当前操作用户】：{info.ApiLogAopInfo.OpUserName}");
        sb.AppendLine($"【当前执行方法】：{info.ApiLogAopInfo.RequestMethodName}");
        sb.AppendLine($"【携带的参数有】： {info.ApiLogAopInfo.RequestParamsName}");
        sb.AppendLine($"【携带的参数JSON】： {info.ApiLogAopInfo.RequestParamsData}");
        sb.AppendLine($"【响应时间】：{info.ApiLogAopInfo.ResponseIntervalTime}");
        sb.AppendLine($"【执行完成时间】：{info.ApiLogAopInfo.ResponseTime}");
        sb.AppendLine($"【执行完成结果】：{info.ApiLogAopInfo.ResponseJsonData}");
        sb.AppendLine($"【执行完成异常信息】：方法中出现异常：{info.ExMessage}");
        sb.Append($"【执行完成异常】：方法中出现异常：{info.InnerException}");
        return sb.ToString();
    }

    public static void OutSql2LogToDB(string prefix, string traceId, string[] dataParas, bool IsHeader = true)
    {
        dataParas = dataParas.Skip(1).ToArray();
        string logContent = string.Join("", dataParas);

        switch (prefix)
        {
            case "AOPLog":
                Log.Information(logContent);
                break;

            case "AOPLogEx":
                Log.Error(logContent);
                break;

            case "RequestIpInfoLog":
                Log.Information(logContent);
                break;

            case "RecordAccessLogs":
                Task.Run(() => ProcessAccessLog(logContent));
                break;

            case "SqlLog":
                Log.Information(logContent);
                break;

            case "RequestResponseLog":
                var requestInfo = JsonHelper.JsonToObj<UserAccessModel>(logContent);
                if (requestInfo.Path.IsNullOrEmpty() || (requestInfo.Path.IsNotEmptyOrNull() && !requestInfo.Path.Contains("/Upload")))
                    Log.Information(logContent);
                break;
        }
    }

    private static void ProcessAccessLog(string logContent)
    {
        try
        {
            var ip = HttpContextExtension.GetUserIp(App.HttpContext);
            var requestInfo = JsonHelper.JsonToObj<UserAccessModel>(logContent);

            if (requestInfo?.RequestData == null)
            {
                return;
            }

            // 过滤特定的API
            if (requestInfo.API == "/api/Authorize/Login" ||
                requestInfo.API.Contains("SM_SYSTEM_API_LOG_MNG") ||
                requestInfo.API.Contains("/Upload") ||
                requestInfo.RequestData.Contains("SM_SYSTEM_LOGIN_LOG_MNG"))
            {
                return;
            }

            Log.Information(logContent);


            if (requestInfo.Filter.IsNotEmptyOrNull())
            {
                requestInfo.Filter = WebUtility.UrlDecode(requestInfo.Filter);
            }

            var dbInsert = new DbInsert("SmApiLog")
            {
                // Avoid resolving scoped services in a background task.
                IsInitDefaultValue = false
            };
            if (Guid.TryParse(requestInfo.User, out var userId))
                dbInsert.Values("CreatedBy", userId);
            dbInsert.Values("CreatedTime", Utility.GetSysDateString());
            dbInsert.Values("Tag", 1);
            dbInsert.Values("IsDeleted", 0);
            dbInsert.Values("IsActive", 1);
            dbInsert.Values("UserId", requestInfo.User);
            dbInsert.Values("IP", ip == "::1" ? "localhost" : ip);
            dbInsert.Values("Path", requestInfo.API);
            dbInsert.Values("Method", requestInfo.RequestMethod);
            dbInsert.Values("RequestData", requestInfo.RequestData + requestInfo.Filter);
            dbInsert.Values("BeginTime", requestInfo.BeginTime);
            dbInsert.Values("OPTime", requestInfo.OPTime.Replace("ms", string.Empty));
            dbInsert.Values("Agent", requestInfo.Agent);

            DBHelper.ExecuteNonQuery(dbInsert.GetSql());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "处理访问日志时发生错误");
        }
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <param name="fileName">文件名</param>
    /// <param name="encode">编码</param>
    /// <param name="readType">读取类型(0:精准,1:前缀模糊)</param>
    /// <param name="takeOnlyTop">仅取前N个文件</param>
    /// <returns></returns>
    public static string ReadLog(string folderPath, string fileName, Encoding encode, ReadType readType = ReadType.Accurate, int takeOnlyTop = -1)
    {
        try
        {
            _logWriteLock.EnterReadLock();

            return readType switch
            {
                ReadType.Accurate => ReadSingleFile(folderPath, fileName, encode),
                ReadType.Prefix => ReadPrefixFiles(folderPath, fileName, encode, takeOnlyTop),
                ReadType.PrefixLatest => ReadLatestPrefixFile(folderPath, fileName, encode),
                _ => string.Empty
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"读取日志文件失败: {fileName}");
            Interlocked.Increment(ref _failedCount);
            return string.Empty;
        }
        finally
        {
            _logWriteLock.ExitReadLock();
        }
    }

    private static string ReadSingleFile(string folderPath, string fileName, Encoding encode)
    {
        var filePath = Path.Combine(folderPath, fileName);
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        using var reader = new StreamReader(filePath, encode);
        return reader.ReadToEnd();
    }

    private static string ReadPrefixFiles(string folderPath, string fileName, Encoding encode, int takeOnlyTop)
    {
        if (!Directory.Exists(folderPath))
        {
            return string.Empty;
        }

        var allFiles = new DirectoryInfo(folderPath);
        IEnumerable<FileInfo> selectFiles = allFiles.GetFiles()
            .Where(fi => fi.Name.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Name);

        if (takeOnlyTop > 0)
        {
            selectFiles = selectFiles.Take(takeOnlyTop);
        }

        var sb = new StringBuilder();
        foreach (var file in selectFiles)
        {
            if (File.Exists(file.FullName))
            {
                using var reader = new StreamReader(file.FullName, encode);
                sb.Append(reader.ReadToEnd());
            }
        }

        return sb.ToString();
    }

    private static string ReadLatestPrefixFile(string folderPath, string fileName, Encoding encode)
    {
        if (!Directory.Exists(folderPath))
        {
            return string.Empty;
        }

        var allFiles = new DirectoryInfo(folderPath);
        var latestFile = allFiles.GetFiles()
            .Where(fi => fi.Name.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Name)
            .FirstOrDefault();

        if (latestFile == null || !File.Exists(latestFile.FullName))
        {
            return string.Empty;
        }

        using var reader = new StreamReader(latestFile.FullName, encode);
        return reader.ReadToEnd();
    }

    private static List<RequestInfo> GetRequestInfo(ReadType readType)
    {
        var requestInfos = new List<RequestInfo>();
        var accessLogs = ReadLog(Path.Combine(_contentRoot, "Log"), "RequestIpInfoLog_", Encoding.UTF8, readType).ObjToString();

        if (string.IsNullOrEmpty(accessLogs))
        {
            return requestInfos;
        }

        try
        {
            return JsonConvert.DeserializeObject<List<RequestInfo>>("[" + accessLogs + "]") ?? new List<RequestInfo>();
        }
        catch
        {
            var accLogArr = accessLogs.Split("\r\n");
            foreach (var item in accLogArr)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                try
                {
                    var accItem = JsonConvert.DeserializeObject<RequestInfo>(item.TrimEnd(','));
                    if (accItem != null)
                    {
                        requestInfos.Add(accItem);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"解析请求信息失败: {item}");
                }
            }
        }

        return requestInfos;
    }

    public static List<LogInfo> GetLogData()
    {
        var allLogs = new List<LogInfo>();

        // 读取 AOP 日志
        allLogs.AddRange(ReadAOPLogs());

        // 读取异常日志
        allLogs.AddRange(ReadExceptionLogs());

        // 读取 SQL 日志
        allLogs.AddRange(ReadSqlLogs());

        // 读取请求响应日志
        allLogs.AddRange(ReadRequestResponseLogs());

        return allLogs
            .OrderByDescending(d => d.Import)
            .ThenByDescending(d => d.Datetime)
            .Take(MaxLogEntries)
            .ToList();
    }

    private static List<LogInfo> ReadAOPLogs()
    {
        try
        {
            var aoplogContent = ReadLog(Path.Combine(_contentRoot, "Log"), "AOPLog_", Encoding.UTF8, ReadType.Prefix);

            if (string.IsNullOrEmpty(aoplogContent))
            {
                return new List<LogInfo>();
            }

            return aoplogContent.Split(LogSeparator)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => new LogInfo
                {
                    Datetime = d.Split("|")[0].ObjToDate(),
                    Content = d.Split("|")[1]?.Replace("\r\n", "<br>"),
                    LogColor = "AOP",
                }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "读取AOP日志失败");
            return new List<LogInfo>();
        }
    }

    private static List<LogInfo> ReadExceptionLogs()
    {
        try
        {
            var exclogContent = ReadLog(
                Path.Combine(_contentRoot, "Log"),
                $"GlobalExceptionLogs_{DateTime.Now:yyyMMdd}.log",
                Encoding.UTF8);

            if (string.IsNullOrEmpty(exclogContent))
            {
                return new List<LogInfo>();
            }

            return exclogContent.Split(LogSeparator)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => new LogInfo
                {
                    Datetime = d.Split("|")[0].Split(',')[0].ObjToDate(),
                    Content = d.Split("|")[1]?.Replace("\r\n", "<br>"),
                    LogColor = "EXC",
                    Import = 9,
                }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "读取异常日志失败");
            return new List<LogInfo>();
        }
    }

    private static List<LogInfo> ReadSqlLogs()
    {
        try
        {
            var sqllogContent = ReadLog(Path.Combine(_contentRoot, "Log"), "SqlLog_", Encoding.UTF8, ReadType.PrefixLatest);

            if (string.IsNullOrEmpty(sqllogContent))
            {
                return new List<LogInfo>();
            }

            return sqllogContent.Split(LogSeparator)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => new LogInfo
                {
                    Datetime = d.Split("|")[0].ObjToDate(),
                    Content = d.Split("|")[1]?.Replace("\r\n", "<br>"),
                    LogColor = "SQL",
                }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "读取SQL日志失败");
            return new List<LogInfo>();
        }
    }

    private static List<LogInfo> ReadRequestResponseLogs()
    {
        try
        {
            var logs = GetRequestInfo(ReadType.PrefixLatest);
            logs = logs.Where(d => d.Datetime.ObjToDate() >= DateTime.Today).ToList();

            return logs.Select(d => new LogInfo
            {
                Datetime = d.Datetime.ObjToDate(),
                Content = $"IP:{d.Ip}<br>{d.Url}",
                LogColor = "ReqRes",
            }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "读取请求响应日志失败");
            return new List<LogInfo>();
        }
    }

    public static RequestApiWeekView RequestApiinfoByWeek()
    {
        var columns = new List<string> { "日期" };
        var jsonBuilder = new StringBuilder();
        jsonBuilder.Append('[');

        try
        {
            var logs = GetRequestInfo(ReadType.Prefix);
            var apiWeeks = (from n in logs
                            group n by new { n.Week, n.Url } into g
                            select new ApiWeek
                            {
                                week = g.Key.Week,
                                url = g.Key.Url,
                                count = g.Count(),
                            }).ToList();

            var weeks = apiWeeks.GroupBy(x => x.week).Select(s => s.First()).ToList();
            var isFirstWeek = true;

            foreach (var week in weeks)
            {
                if (!isFirstWeek)
                {
                    jsonBuilder.Append(',');
                }
                isFirstWeek = false;

                var apiweeksCurrentWeek = apiWeeks
                    .Where(d => d.week == week.week)
                    .OrderByDescending(d => d.count)
                    .Take(MaxWeekApis)
                    .ToList();

                jsonBuilder.Append('{');
                jsonBuilder.Append($"\"日期\":\"{week.week}\"");

                foreach (var item in apiweeksCurrentWeek)
                {
                    columns.Add(item.url);
                    jsonBuilder.Append($",\"{item.url}\":\"{item.count}\"");
                }

                jsonBuilder.Append('}');
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取每周API信息失败");
        }

        jsonBuilder.Append(']');

        return new RequestApiWeekView
        {
            columns = columns.Distinct().ToList(),
            rows = jsonBuilder.ToString(),
        };
    }

    public static AccessApiDateView AccessApiByDate()
    {
        try
        {
            var logs = GetRequestInfo(ReadType.Prefix);
            var apiDates = (from n in logs
                            group n by n.Date into g
                            select new ApiDate
                            {
                                date = g.Key,
                                count = g.Count(),
                            })
                           .OrderByDescending(d => d.date)
                           .Take(MaxDateEntries)
                           .OrderBy(d => d.date)
                           .ToList();

            return new AccessApiDateView
            {
                columns = new[] { "date", "count" },
                rows = apiDates,
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取每日API访问数据失败");
            return new AccessApiDateView
            {
                columns = new[] { "date", "count" },
                rows = new List<ApiDate>(),
            };
        }
    }

    public static AccessApiDateView AccessApiByHour()
    {
        try
        {
            var logs = GetRequestInfo(ReadType.Prefix);
            var apiDates = (from n in logs
                            where n.Datetime.ObjToDate() >= DateTime.Today
                            group n by n.Datetime.ObjToDate().Hour into g
                            select new ApiDate
                            {
                                date = g.Key.ToString("00"),
                                count = g.Count(),
                            })
                           .OrderBy(d => d.date)
                           .Take(MaxHourEntries)
                           .ToList();

            return new AccessApiDateView
            {
                columns = new[] { "date", "count" },
                rows = apiDates,
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取每小时API访问数据失败");
            return new AccessApiDateView
            {
                columns = new[] { "date", "count" },
                rows = new List<ApiDate>(),
            };
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _logWriteLock?.Dispose();
        }

        _disposed = true;
    }
}

public enum ReadType
{
    /// <summary>
    /// 精确查找一个
    /// </summary>
    Accurate,

    /// <summary>
    /// 指定前缀，模糊查找全部
    /// </summary>
    Prefix,

    /// <summary>
    /// 指定前缀，最新一个文件
    /// </summary>
    PrefixLatest
}

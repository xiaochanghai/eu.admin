using EU.Core.Common.Extensions;
using EU.Core.Common.Helper;
using Serilog;

namespace EU.Core.Common.LogHelper;

/// <summary>
/// 通过内置队列异步定时写日志
/// </summary>
public static class Logger
{
    private static readonly object _lockObject = new();
    private static DateTime _lastClearFileDT = DateTime.Now.AddDays(-1);
    private static CancellationTokenSource _cancellationTokenSource;

    public static string CurrentPath { get; private set; }

    private static string LoggerPath => Path.Combine(CurrentPath, "Logs", "Queue");
    private static string LogPath => Path.Combine(CurrentPath, "Logs");

    static Logger()
    {
        var baseDirectory = AppContext.BaseDirectory;
        CurrentPath = Path.Combine(baseDirectory, "").ReplacePath();

        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => Start(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 停止后台日志写入任务
    /// </summary>
    public static void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    private static async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 每5秒检查一次
                await Task.Delay(5000, cancellationToken);

                // 每天清理一次旧文件
                if ((DateTime.Now - _lastClearFileDT).TotalDays > 1)
                {
                    try
                    {
                        FileHelper.DeleteFolder(LoggerPath);
                        _lastClearFileDT = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "清理日志文件夹失败");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，退出循环
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "日志后台任务执行出错");
                WriteErrorToFile(ex.Message + ex.StackTrace + ex.Source);
            }
        }
    }

    private static void WriteErrorToFile(string message)
    {
        try
        {
            var errorPath = Path.Combine(LoggerPath, "WriteError");
            var fileName = $"{DateTime.Now:yyyyMMdd}.txt";
            FileHelper.WriteFile(errorPath, fileName, message + "\r\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志写入文件时出错: {ex.Message}");
        }
    }

    #region 记录文件日志

    /// <summary>
    /// 写日志到文件
    /// </summary>
    /// <param name="folder">文件夹名称</param>
    /// <param name="fileName">文件名（为空时使用当前日期）</param>
    /// <param name="message">日志消息</param>
    /// <param name="isForce">是否强制写入（不受配置影响）</param>
    private static void WriteFileLog(string folder, string fileName, string message, bool isForce)
    {
        try
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {folder} {fileName} {message}");

            // 检查是否启用文件日志（强制写入时跳过检查）
            if (!isForce)
            {
                var isFileLog = "Y"; // 默认启用，可以从配置读取
                if (isFileLog != "Y")
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"{DateTime.Now:yyyyMMdd}.txt";
            }

            var logFolder = string.IsNullOrEmpty(folder) ? LogPath : Path.Combine(LogPath, folder);
            FileHelper.WriteFile(logFolder, fileName, message + "\r\n", true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "写入文件日志失败");
        }
    }

    /// <summary>
    /// 按日期记录文本日志（每天一个文件）
    /// </summary>
    /// <param name="folder">文件夹名称</param>
    /// <param name="fileName">文件名</param>
    /// <param name="message">日志消息</param>
    /// <param name="isForce">是否强制写入</param>
    public static void WriteLog(string folder, string fileName, string message, bool isForce)
    {
        WriteFileLog(folder, fileName, message, isForce);
    }

    /// <summary>
    /// 按日期记录文本日志（每天一个文件）
    /// </summary>
    /// <param name="folder">文件夹名称</param>
    /// <param name="fileName">文件名</param>
    /// <param name="message">日志消息</param>
    public static void WriteLog(string folder, string fileName, string message)
    {
        WriteFileLog(folder, fileName, message, false);
    }

    /// <summary>
    /// 按日期记录文本日志（每天一个文件）
    /// </summary>
    /// <param name="folder">文件夹名称</param>
    /// <param name="message">日志消息</param>
    public static void WriteLog(string folder, string message)
    {
        WriteFileLog(folder, null, message, false);
    }

    /// <summary>
    /// 按日期记录文本日志（每天一个文件，使用默认文件夹）
    /// </summary>
    /// <param name="message">日志消息</param>
    public static void WriteLog(string message)
    {
        WriteFileLog(null, null, message, false);
    }

    #endregion
}

/// <summary>
/// 日志状态枚举
/// </summary>
public enum LoggerStatus
{
    /// <summary>
    /// 成功
    /// </summary>
    Success = 1,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 2,

    /// <summary>
    /// 信息
    /// </summary>
    Info = 3
}

namespace EU.Core.Common.LogHelper;

/// <summary>
/// 日志上下文静态配置
/// </summary>
public static class LogContextStatic
{
    private const string DateFormat = "yyyyMMdd";

    static LogContextStatic()
    {
        if (!Directory.Exists(BaseLogs))
        {
            Directory.CreateDirectory(BaseLogs);
        }
    }

    /// <summary>
    /// 基础日志文件夹名称
    /// </summary>
    public const string BaseLogs = "Logs";

    /// <summary>
    /// 基于当前日期的日志路径
    /// </summary>
    public static string BasePathLogs => DateTime.Now.ToString(DateFormat);

    // 日志上下文属性键
    public const string LogSource = "LogSource";
    public const string AopSql = "AopSql";
    public const string SqlOutToConsole = "OutToConsole";
    public const string SqlOutToFile = "SqlOutToFile";
    public const string OutToDb = "OutToDb";
    public const string SugarActionType = "SugarActionType";

    /// <summary>
    /// 文件日志消息模板
    /// </summary>
    public static readonly string FileMessageTemplate =
        $"{{NewLine}}Date：{{Timestamp:yyyy-MM-dd HH:mm:ss.fff}}{{NewLine}}" +
        $"LogLevel：{{Level}}{{NewLine}}" +
        $"Message：{{Message}}{{NewLine}}" +
        $"{{Exception}}{new string('-', 100)}";

    /// <summary>
    /// 组合日志路径
    /// </summary>
    /// <param name="paths">路径片段</param>
    /// <returns>组合后的完整路径</returns>
    public static string Combine(params string[] paths)
    {
        if (paths == null || paths.Length == 0)
        {
            return BaseLogs;
        }

        var allPaths = new string[paths.Length + 1];
        allPaths[0] = BaseLogs;
        Array.Copy(paths, 0, allPaths, 1, paths.Length);

        return Path.Combine(allPaths);
    }
}

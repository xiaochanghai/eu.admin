using Serilog.Context;
using SqlSugar;

namespace EU.Core.Common.LogHelper;

/// <summary>
/// 日志上下文扩展，用于管理 Serilog 日志上下文的生命周期
/// </summary>
public class LogContextExtension : IDisposable
{
    private readonly Stack<IDisposable> _disposableStack = new();

    public static LogContextExtension Create => new();

    /// <summary>
    /// 添加需要释放的资源到栈中
    /// </summary>
    public LogContextExtension AddDisposable(IDisposable disposable)
    {
        if (disposable != null)
        {
            _disposableStack.Push(disposable);
        }
        return this;
    }

    /// <summary>
    /// 为 SQL AOP 推送日志属性
    /// </summary>
    public LogContextExtension SqlAopPushProperty(ISqlSugarClient db)
    {
        if (db == null)
        {
            throw new ArgumentNullException(nameof(db));
        }

        AddDisposable(LogContext.PushProperty(LogContextStatic.LogSource, LogContextStatic.AopSql));
        AddDisposable(LogContext.PushProperty(LogContextStatic.SqlOutToConsole,
            AppSettings.app(new[] { "AppSettings", "SqlAOP", "LogToConsole", "Enabled" }).ObjToBool()));
        AddDisposable(LogContext.PushProperty(LogContextStatic.SqlOutToFile,
            AppSettings.app(new[] { "AppSettings", "SqlAOP", "LogToFile", "Enabled" }).ObjToBool()));
        AddDisposable(LogContext.PushProperty(LogContextStatic.OutToDb,
            AppSettings.app(new[] { "AppSettings", "SqlAOP", "LogToDB", "Enabled" }).ObjToBool()));
        AddDisposable(LogContext.PushProperty(LogContextStatic.SugarActionType, db.SugarActionType));

        return this;
    }

    public void Dispose()
    {
        while (_disposableStack.Count > 0)
        {
            try
            {
                _disposableStack.Pop()?.Dispose();
            }
            catch
            {
                // 忽略释放时的异常，避免影响其他资源的释放
            }
        }
    }
}

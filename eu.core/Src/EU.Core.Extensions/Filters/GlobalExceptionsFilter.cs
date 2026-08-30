using EU.Core.Common.Helper;
using EU.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EU.Core.Extensions.Filters;

/// <summary>
/// 全局异常附加处理器。
/// </summary>
public interface IGlobalExceptionObserver
{
    Task OnExceptionAsync(
        ExceptionContext context,
        ServiceResult<string> result,
        CancellationToken cancellationToken);
}

/// <summary>
/// 将 Controller 异常统一转换为 ServiceResult 响应。
/// </summary>
public sealed class GlobalExceptionsFilter(
    IHostEnvironment environment,
    ILogger<GlobalExceptionsFilter> logger,
    IEnumerable<IGlobalExceptionObserver> observers) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Exception is OperationCanceledException &&
            context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        var result = new ServiceResult<string>
        {
            Status = 500,
            Message = NormalizeMessage(context.Exception.Message)
        };
        if (environment.IsDevelopment())
        {
            result.MessageDev = context.Exception.StackTrace;
        }

        context.Result = new ContentResult
        {
            Content = JsonHelper.GetJSON<ServiceResult<string>>(result),
            ContentType = "application/json; charset=utf-8"
        };
        context.ExceptionHandled = true;

        logger.LogError(
            context.Exception,
            "Unhandled Controller exception. TraceId: {TraceId}",
            context.HttpContext.TraceIdentifier);

        foreach (IGlobalExceptionObserver observer in observers)
        {
            try
            {
                await observer.OnExceptionAsync(
                    context,
                    result,
                    context.HttpContext.RequestAborted);
            }
            catch (OperationCanceledException) when (
                context.HttpContext.RequestAborted.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Global exception observer {ObserverType} failed. TraceId: {TraceId}",
                    observer.GetType().FullName,
                    context.HttpContext.TraceIdentifier);
            }
        }
    }

    private static string NormalizeMessage(string message)
    {
        const string errorAudit = "Unable to resolve service for";
        return !string.IsNullOrEmpty(message) && message.Contains(errorAudit, StringComparison.Ordinal)
            ? message.Replace(
                errorAudit,
                $"（若新添加服务，需要重新编译项目）{errorAudit}",
                StringComparison.Ordinal)
            : message;
    }
}

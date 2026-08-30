using EU.Core.Extensions.Filters;
using EU.Core.Hubs;
using EU.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Profiling;

namespace EU.Core.Filter;

/// <summary>
/// EU.Core.Api 宿主专属的异常附加处理。
/// </summary>
public sealed class EuCoreApiGlobalExceptionObserver(
    IHubContext<ChatHub> hubContext) : IGlobalExceptionObserver
{
    public async Task OnExceptionAsync(
        ExceptionContext context,
        ServiceResult<string> result,
        CancellationToken cancellationToken)
    {
        MiniProfiler.Current.CustomTiming("Errors：", result.Message);
        if (AppSettings.app(["Middleware", "SignalRSendLog", "Enabled"]).ObjToBool())
        {
            await hubContext.Clients.All.SendAsync(
                "ReceiveUpdate",
                LogLock.GetLogData(),
                cancellationToken);
        }
    }
}

public class InternalServerErrorObjectResult : ObjectResult
{
    public InternalServerErrorObjectResult(object value) : base(value)
    {
        StatusCode = StatusCodes.Status500InternalServerError;
    }
}

//返回错误信息
public class JsonErrorResponse
{
    /// <summary>
    /// 生产环境的消息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 开发环境的消息
    /// </summary>
    public string DevelopmentMessage { get; set; }
}

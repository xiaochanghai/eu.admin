using EU.Core.Common;
using EU.Core.Common.Extensions;
using EU.Core.Common.LogHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;

namespace EU.Core.Extensions.Middlewares;

/// <summary>
/// 中间件
/// 记录请求和响应数据
/// </summary>
public class RequRespLogMiddleware
{
    /// <summary>
    /// 
    /// </summary>
    private readonly RequestDelegate _next;

    private readonly ILogger<RequRespLogMiddleware> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    public RequRespLogMiddleware(RequestDelegate next, ILogger<RequRespLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }


    public async Task InvokeAsync(HttpContext context)
    {
        if (AppSettings.app("Middleware", "RequestResponseLog", "Enabled").ObjToBool())
        {
            // 过滤，只有接口
            if (context.Request.Path.Value.Contains("api"))
            {
                context.Request.EnableBuffering();

                // 存储请求数据
                await RequestDataLog(context);

                await _next(context);

                // 存储响应数据
                ResponseDataLog(context.Response);
            }
            else
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task RequestDataLog(HttpContext context)
    {
        var request = context.Request;

        var headers = request.Headers;

        QueryFilter queryFilter;

        if (!headers.ContainsKey("filter"))
        {
            queryFilter = QueryFilter.Default;
        }

        string filter = headers["filter"];
        if (filter.IsNotEmptyOrNull())
            filter = WebUtility.UrlDecode(filter);

        var bodyData = IsMultipartRequest(request) ? "[multipart/form-data skipped]" : await ReadRequestBodyAsync(request);

        RequestLogInfo requestResponse = new RequestLogInfo()
        {
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            BodyData = bodyData,
            filter = filter
        };
        var content = JsonConvert.SerializeObject(requestResponse);
        //var content = $" QueryData:{request.Path + request.QueryString}\r\n BodyData:{await sr.ReadToEndAsync()}";

        if (!string.IsNullOrEmpty(content))
        {
            WriteLogSafely(context.TraceIdentifier,
                new string[] { "Request Data -  RequestJsonDataType:" + requestResponse.GetType().ToString(), content });
            //SerilogServer.WriteLog("RequestResponseLog", new string[] { "Request Data:", content });

            request.Body.Position = 0;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var sr = new StreamReader(request.Body, leaveOpen: true);
        return await sr.ReadToEndAsync();
    }

    private static bool IsMultipartRequest(HttpRequest request) =>
        request.ContentType?.IndexOf("multipart/form-data", StringComparison.OrdinalIgnoreCase) >= 0;

    private void ResponseDataLog(HttpResponse response)
    {
        var responseBody = response.GetResponseBody();

        // 去除 Html
        var reg = "<[^>]+>";

        if (!string.IsNullOrEmpty(responseBody))
        {
            var isHtml = Regex.IsMatch(responseBody, reg);
            if (response.ContentType?.Contains("image/", StringComparison.OrdinalIgnoreCase) != true)
            {
                WriteLogSafely(response.HttpContext.TraceIdentifier,
                    new string[] { "Response Data -  ResponseJsonDataType:" + responseBody.GetType().ToString(), responseBody });
            }
            //SerilogServer.WriteLog("RequestResponseLog", new string[] { "Response Data:", responseBody });
        }
    }

    private void ResponseDataLog(HttpResponse response, MemoryStream ms)
    {
        ms.Position = 0;
        var responseBody = new StreamReader(ms).ReadToEnd();

        // 去除 Html
        var reg = "<[^>]+>";
        var isHtml = Regex.IsMatch(responseBody, reg);

        if (!string.IsNullOrEmpty(responseBody))
        {
            WriteLogSafely(response.HttpContext.TraceIdentifier,
                new string[] { "Response Data -  ResponseJsonDataType:" + responseBody.GetType().ToString(), responseBody });
            //SerilogServer.WriteLog("RequestResponseLog", new string[] { "Response Data:", responseBody });
        }
    }

    private void WriteLogSafely(string traceId, string[] data)
    {
        try
        {
            LogLock.OutLogAOP("RequestResponseLog", traceId, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write request/response log. TraceId: {TraceId}", traceId);
        }
    }
}

using EU.Core.Common.Https;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;

namespace EU.Core.Extensions.Middlewares;

public static class FluentResponseBodyMiddleware
{
    /// <summary>
    /// 跳过响应体拦截的路径（大文件下载/流式接口）
    /// </summary>
    private static readonly string[] SkipPaths =
    {
        "/api/file/download/",
        "/api/file/img/",
        "/efs/download/",
        "/efs/img/",
        "/stream",
    };

    public static IApplicationBuilder UseResponseBodyRead(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // 大文件下载：跳过响应体拦截，避免 MemoryStream 全量缓存导致 CPU/内存飙升
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            if (SkipPaths.Any(p => path.Contains(p)))
            {
                await next(context);
                return;
            }

            await using var swapStream = new FluentHttpResponseStream(context!.Features!.Get<IHttpResponseBodyFeature>()!,
                context!.Features!.Get<IHttpBodyControlFeature>()!);
            context.Response.Body = swapStream;
            await next(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
        });
    }
}
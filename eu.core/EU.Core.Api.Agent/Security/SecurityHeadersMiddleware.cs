namespace EU.Core.Api.Agent.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var response = (HttpResponse)state;
            response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            response.Headers.TryAdd("X-Frame-Options", "DENY");
            response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            response.Headers.TryAdd("Permissions-Policy",
                "camera=(), microphone=(), geolocation=(), payment=(), usb=()");
            response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
            response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
            response.Headers.TryAdd("Content-Security-Policy",
                "default-src 'self'; base-uri 'none'; frame-ancestors 'none'; object-src 'none'; " +
                "script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; " +
                "form-action 'self'");
            if (response.HttpContext.Request.Path.StartsWithSegments("/api")
                || response.HttpContext.Request.Path.StartsWithSegments("/metrics"))
                response.Headers.TryAdd("Cache-Control", "no-store");
            return Task.CompletedTask;
        }, context.Response);
        await next(context);
    }
}

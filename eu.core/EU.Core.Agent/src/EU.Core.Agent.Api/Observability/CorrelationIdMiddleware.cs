using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EU.Core.Agent.Api.Observability;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly ILogger<CorrelationIdMiddleware> logger = loggerFactory.CreateLogger<CorrelationIdMiddleware>();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdentifierPattern();

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = SelectCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string SelectCorrelationId(HttpContext context)
    {
        string? incoming = context.Request.Headers[HeaderName].ToString();
        return !string.IsNullOrWhiteSpace(incoming) && SafeIdentifierPattern().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");
    }
}

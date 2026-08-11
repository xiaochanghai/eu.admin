using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;

namespace EU.Core.Api.Agent.Errors;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    private readonly ILogger<ProblemDetailsMiddleware> logger = loggerFactory.CreateLogger<ProblemDetailsMiddleware>();

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (
            context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected. The request cancellation is expected,
            // so do not log it as a server failure or attempt a response body.
            context.Items[AgentOperationAuditMiddleware.CancelledItemKey] = true;
        }
        catch (RequestBodyTooLargeException) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status413PayloadTooLarge,
                "The request body exceeds the supported size.",
                "REQUEST_BODY_TOO_LARGE");
        }
        catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
        {
            int status = exception.StatusCode is StatusCodes.Status413PayloadTooLarge
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;
            await WriteProblemAsync(
                context,
                status,
                status == StatusCodes.Status413PayloadTooLarge
                    ? "The request body exceeds the supported size."
                    : "The request body is invalid.",
                status == StatusCodes.Status413PayloadTooLarge
                    ? "REQUEST_BODY_TOO_LARGE"
                    : "REQUEST_INVALID");
        }
        catch (JsonException) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "The request body is invalid.",
                "REQUEST_INVALID");
        }
        catch (Exception) when (!context.Response.HasStarted)
        {
            string traceId = ResolveTraceId(context);
            logger.LogError("Unhandled request failed. TraceId: {TraceId}", traceId);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "UNEXPECTED_ERROR");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string title,
        string? errorCode = null)
    {
        string traceId = ResolveTraceId(context);
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.Headers[CorrelationIdMiddleware.HeaderName] = traceId;
        var problem = new Dictionary<string, object?>
        {
            ["type"] = "about:blank",
            ["title"] = title,
            ["status"] = status,
            ["traceId"] = traceId
        };
        if (errorCode is not null)
        {
            problem["errorCode"] = errorCode;
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, problem);
    }

    private static string ResolveTraceId(HttpContext context)
    {
        return context.TraceIdentifier ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}

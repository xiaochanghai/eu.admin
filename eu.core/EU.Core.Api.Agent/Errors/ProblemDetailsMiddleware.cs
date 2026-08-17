using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
            await AgentApiErrorResponseWriter.WriteAsync(
                context,
                "REQUEST_BODY_TOO_LARGE",
                "The request body exceeds the supported size.",
                clearResponse: true,
                cancellationToken: context.RequestAborted);
        }
        catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
        {
            int status = exception.StatusCode is StatusCodes.Status413PayloadTooLarge
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;
            await AgentApiErrorResponseWriter.WriteAsync(
                context,
                status == StatusCodes.Status413PayloadTooLarge
                    ? "REQUEST_BODY_TOO_LARGE"
                    : "REQUEST_INVALID",
                status == StatusCodes.Status413PayloadTooLarge
                    ? "The request body exceeds the supported size."
                    : "The request body is invalid.",
                httpStatus: status,
                clearResponse: true,
                cancellationToken: context.RequestAborted);
        }
        catch (JsonException) when (!context.Response.HasStarted)
        {
            await AgentApiErrorResponseWriter.WriteAsync(
                context,
                "REQUEST_INVALID",
                "The request body is invalid.",
                clearResponse: true,
                cancellationToken: context.RequestAborted);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            string traceId = context.TraceIdentifier;
            logger.LogError(exception, "Unhandled request failed. TraceId: {TraceId}", traceId);

            await AgentApiErrorResponseWriter.WriteAsync(
                context,
                "UNEXPECTED_ERROR",
                "An unexpected error occurred.",
                clearResponse: true,
                cancellationToken: context.RequestAborted);
        }
    }
}

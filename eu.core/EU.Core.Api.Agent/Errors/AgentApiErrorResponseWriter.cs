using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Errors;

public static class AgentApiErrorResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        string errorCode,
        string message,
        int? httpStatus = null,
        bool clearResponse = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(context, errorCode);
        int status = httpStatus
            ?? descriptor.HttpStatus
            ?? StatusCodes.Status500InternalServerError;
        string traceId = string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : context.TraceIdentifier;

        if (clearResponse)
            context.Response.Clear();

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers[CorrelationIdMiddleware.HeaderName] = traceId;
        var jsonOptions = context.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .JsonSerializerOptions;
        return context.Response.WriteAsJsonAsync(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, traceId)),
            jsonOptions,
            cancellationToken);
    }
}

using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Observability;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EU.Core.Api.Agent.Errors;

internal sealed class AgentApiValidationResultFilter : IAlwaysRunResultFilter
{
    public static IActionResult InvalidModelState(ActionContext context)
    {
        bool unsupportedMediaType = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Any(error => error.Exception is UnsupportedContentTypeException);
        return Create(
            context.HttpContext,
            unsupportedMediaType
                ? StatusCodes.Status415UnsupportedMediaType
                : StatusCodes.Status400BadRequest,
            unsupportedMediaType
                ? "REQUEST_UNSUPPORTED_MEDIA_TYPE"
                : "REQUEST_INVALID",
            unsupportedMediaType
                ? "The request media type is not supported."
                : "The request body is invalid.");
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is UnsupportedMediaTypeResult)
        {
            context.Result = Create(
                context.HttpContext,
                StatusCodes.Status415UnsupportedMediaType,
                "REQUEST_UNSUPPORTED_MEDIA_TYPE",
                "The request media type is not supported.");
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static JsonResult Create(
        HttpContext context,
        int httpStatus,
        string errorCode,
        string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(context, errorCode);
        context.Response.Headers[CorrelationIdMiddleware.HeaderName] = context.TraceIdentifier;
        return new JsonResult(ServiceResult<AgentApiErrorData>.Failure(
            descriptor.Status,
            message,
            new AgentApiErrorData(errorCode, context.TraceIdentifier)))
        {
            StatusCode = httpStatus
        };
    }
}

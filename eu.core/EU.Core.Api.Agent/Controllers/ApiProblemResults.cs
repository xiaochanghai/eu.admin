using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

internal static class ApiProblemResults
{
    public static ObjectResult Create(
        HttpContext context,
        int status,
        string errorCode,
        string title,
        string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = "about:blank"
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static IActionResult InvalidModelState(ActionContext context) =>
        Create(
            context.HttpContext,
            StatusCodes.Status400BadRequest,
            "REQUEST_INVALID",
            "The request body is invalid.");
}

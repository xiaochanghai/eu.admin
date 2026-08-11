using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Security;

public sealed class AgentAuthorizationResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        bool forbidden = authorizeResult.Forbidden;
        if (forbidden)
        {
            await context.ForbidAsync();
        }
        else
        {
            await context.ChallengeAsync();
        }

        int status = forbidden
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = forbidden
                ? "The caller is not authorized for this operation."
                : "Authentication is required.",
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = forbidden
            ? "AUTHORIZATION_DENIED"
            : "AUTHENTICATION_REQUIRED";
        problem.Extensions["correlationId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem,
            SerializerOptions,
            context.RequestAborted);
    }
}

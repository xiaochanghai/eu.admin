using EU.Core.Api.Agent.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication;

namespace EU.Core.Api.Agent.Security;

public sealed class AgentAuthorizationResultHandler
    : IAuthorizationMiddlewareResultHandler
{
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
        string errorCode = forbidden
            ? "AUTHORIZATION_DENIED"
            : "AUTHENTICATION_REQUIRED";
        await AgentApiErrorResponseWriter.WriteAsync(
            context,
            errorCode,
            forbidden
                ? "The caller is not authorized for this operation."
                : "Authentication is required.",
            httpStatus: status,
            cancellationToken: context.RequestAborted);
    }
}

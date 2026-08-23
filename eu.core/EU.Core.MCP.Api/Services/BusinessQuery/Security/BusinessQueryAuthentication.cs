using System.Security.Cryptography;
using System.Text;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed class BusinessQueryAuthenticationMiddleware(
    RequestDelegate next,
    BusinessQueryServiceTokenResolver serviceTokenResolver,
    IBusinessQueryAuditRepository auditRepository,
    TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/mcp/business-query"))
        {
            await next(context);
            return;
        }

        string expected = serviceTokenResolver.Resolve();
        string authorization = context.Request.Headers.Authorization.ToString();
        string provided = authorization.StartsWith("Bearer ", StringComparison.Ordinal)
            ? authorization[7..]
            : string.Empty;
        if (expected.Length < 32
            || provided.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided),
                Encoding.UTF8.GetBytes(expected)))
        {
            try
            {
                await auditRepository.WriteSecurityRejectionAsync(
                    new BusinessQuerySecurityAuditRecord(
                        Guid.NewGuid(),
                        "mcp-authentication",
                        "rejected",
                        "AUTHENTICATION_REQUIRED",
                        timeProvider.GetUtcNow()),
                    CancellationToken.None);
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "AUDIT_UNAVAILABLE" });
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "AUTHENTICATION_REQUIRED" });
            return;
        }

        await next(context);
    }
}

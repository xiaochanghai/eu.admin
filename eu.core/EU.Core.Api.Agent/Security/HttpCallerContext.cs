using EU.Core.Api.Agent.Configuration;
using EU.Core.Agent.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Security;

public sealed class HttpCallerContext : ICallerContext
{
    public HttpCallerContext(
        IHttpContextAccessor accessor,
        IOptions<AgentAuthenticationOptions> options)
    {
        HttpContext context = accessor.HttpContext ?? throw InvalidContext();
        if (context.User.Identity?.IsAuthenticated != true)
        {
            throw InvalidContext();
        }

        AgentAuthenticationOptions configuration = options.Value;
        UserId = RequiredClaim(context, configuration.UserIdClaimType);
        string[] tenantClaims = context.User
            .FindAll(configuration.TenantClaimType)
            .Select(claim => claim.Value)
            .ToArray();
        TenantId = tenantClaims.Length == 1 && string.Equals(
            tenantClaims[0],
            configuration.TenantId,
            StringComparison.Ordinal)
                ? configuration.TenantId
                : throw InvalidContext();
        Permissions = context.User.FindAll(configuration.PermissionClaimType)
            .Select(claim => claim.Value.Trim())
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        CorrelationId = string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? throw InvalidContext()
            : context.TraceIdentifier;
    }

    public string UserId { get; }

    public string TenantId { get; }

    public IReadOnlySet<string> Permissions { get; }

    public string CorrelationId { get; }

    private static string RequiredClaim(HttpContext context, string claimType)
    {
        string? value = context.User.FindFirst(claimType)?.Value;
        return string.IsNullOrWhiteSpace(value)
            ? throw InvalidContext()
            : value.Trim();
    }

    private static InvalidOperationException InvalidContext() =>
        new("A complete trusted caller context is required.");
}

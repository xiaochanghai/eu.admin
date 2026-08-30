using EU.Core.IServices.Abstractions.Security;

namespace EU.Core.Api.Agent.Security;

public sealed class HttpCallerContext : ICallerContext
{
    public HttpCallerContext(IHttpContextAccessor accessor)
    {
        HttpContext context = accessor.HttpContext ?? throw InvalidContext();
        if (context.User.Identity?.IsAuthenticated != true)
        {
            throw InvalidContext();
        }

        UserId = RequiredClaim(context, AgentIdentityClaims.UserId);
        TenantId = AgentIdentityClaims.GetTenantId(context.User)
            ?? throw InvalidContext();
        Permissions = context.User.FindAll(AgentIdentityClaims.Permission)
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

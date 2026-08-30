using System.Security.Claims;

namespace EU.Core.Api.Agent.Security;

internal static class AgentIdentityClaims
{
    public const string UserId = ClaimTypes.Name;
    public const string Tenant = "TenantId";
    public const string Permission = "permission";
    public const string DefaultTenantId = "0";

    public static string? GetTenantId(ClaimsPrincipal principal)
    {
        string[] values = principal.FindAll(Tenant)
            .Select(claim => claim.Value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
        return values.Length == 1 ? values[0] : null;
    }
}

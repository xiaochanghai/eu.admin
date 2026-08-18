using System.Security.Claims;
using EU.Core.Api.Agent.Configuration;

namespace EU.Core.Api.Agent.Security;

internal static class AgentSharedTokenClaimsNormalizer
{
    public static void Normalize(
        ClaimsPrincipal principal,
        AgentAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(options);

        ClaimsIdentity? identity = principal.Identities
            .FirstOrDefault(candidate => candidate.IsAuthenticated);
        if (identity is null)
            return;

        CopySingleClaimWhenMissing(
            identity,
            options.SharedTokenUserIdClaimType,
            options.UserIdClaimType);

        if (!identity.HasClaim(claim =>
                string.Equals(
                    claim.Type,
                    options.TenantClaimType,
                    StringComparison.Ordinal)))
        {
            string[] sharedTenants = identity
                .FindAll(options.SharedTokenTenantClaimType)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (sharedTenants.Length == 1 && string.Equals(
                    sharedTenants[0],
                    options.SharedTokenTenantId,
                    StringComparison.Ordinal))
            {
                identity.AddClaim(new Claim(
                    options.TenantClaimType,
                    options.TenantId));
            }
        }

        if (!HasSingleClaim(identity, options.UserIdClaimType) ||
            !HasSingleClaim(identity, options.TenantClaimType, options.TenantId))
        {
            return;
        }

        foreach (string permission in options.SharedTokenPermissions
                     .Distinct(StringComparer.Ordinal))
        {
            if (!identity.HasClaim(options.PermissionClaimType, permission))
            {
                identity.AddClaim(new Claim(
                    options.PermissionClaimType,
                    permission));
            }
        }
    }

    private static void CopySingleClaimWhenMissing(
        ClaimsIdentity identity,
        string sourceClaimType,
        string targetClaimType)
    {
        if (identity.HasClaim(claim => string.Equals(
                claim.Type,
                targetClaimType,
                StringComparison.Ordinal)))
        {
            return;
        }

        string[] values = identity.FindAll(sourceClaimType)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (values.Length == 1)
            identity.AddClaim(new Claim(targetClaimType, values[0]));
    }

    private static bool HasSingleClaim(
        ClaimsIdentity identity,
        string claimType,
        string? expectedValue = null)
    {
        Claim[] claims = identity.FindAll(claimType).ToArray();
        return claims.Length == 1 &&
               !string.IsNullOrWhiteSpace(claims[0].Value) &&
               (expectedValue is null || string.Equals(
                   claims[0].Value,
                   expectedValue,
                   StringComparison.Ordinal));
    }
}

#nullable enable

using System.Security.Claims;
using System.Text;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Security;
using EU.Core.AuthHelper;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgentSharedTokenClaimsNormalizer_Should
{
    [Fact]
    public void Not_require_permission_claim_when_enforcement_is_disabled()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Bearer"));
        var options = new AgentAuthenticationOptions
        {
            EnforcePermissionClaims = false
        };

        bool allowed = AgentApiSecurityServiceCollectionExtensions.HasRequiredPermission(
            principal,
            options,
            AgentAuthorizationPolicies.ChatPermission);

        Assert.True(allowed);
    }

    [Fact]
    public void Require_permission_claim_when_enforcement_is_enabled()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Bearer"));
        var options = new AgentAuthenticationOptions
        {
            EnforcePermissionClaims = true
        };

        bool allowed = AgentApiSecurityServiceCollectionExtensions.HasRequiredPermission(
            principal,
            options,
            AgentAuthorizationPolicies.ChatPermission);

        Assert.False(allowed);
    }

    [Fact]
    public void Accept_admin_permission_for_named_policy_when_enforcement_is_enabled()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", AgentAuthorizationPolicies.AdminPermission)],
            "Bearer"));
        var options = new AgentAuthenticationOptions
        {
            EnforcePermissionClaims = true
        };

        bool allowed = AgentApiSecurityServiceCollectionExtensions.HasRequiredPermission(
            principal,
            options,
            AgentAuthorizationPolicies.ChatPermission);

        Assert.True(allowed);
    }

    [Fact]
    public void Accept_token_generated_by_eu_core_api()
    {
        const string issuer = "EU.Core";
        const string audience = "wr";
        var signingKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes("agent-shared-token-contract-test-key-32-bytes"));
        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);
        var requirement = new PermissionRequirement(
            "/api/denied",
            [],
            ClaimTypes.Role,
            issuer,
            audience,
            signingCredentials,
            TimeSpan.FromMinutes(5));
        string token = JwtToken.BuildJwtToken(
            [
                new Claim(ClaimTypes.Name, "user-1"),
                new Claim("TenantId", "0")
            ],
            requirement).token;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };
        ClaimsPrincipal principal = handler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            },
            out _);
        var options = new AgentAuthenticationOptions
        {
            TenantId = "development",
            SharedTokenPermissions = [AgentAuthorizationPolicies.AdminPermission]
        };

        AgentSharedTokenClaimsNormalizer.Normalize(principal, options);

        Assert.Equal("user-1", principal.FindFirst("sub")?.Value);
        Assert.Equal("development", principal.FindFirst("tenant_id")?.Value);
        Assert.Equal(
            AgentAuthorizationPolicies.AdminPermission,
            principal.FindFirst("permission")?.Value);
    }

    [Fact]
    public void Map_shared_api_identity_tenant_and_configured_permission()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "user-1"),
                new Claim("TenantId", "0")
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var options = new AgentAuthenticationOptions
        {
            TenantId = "development",
            SharedTokenPermissions = [AgentAuthorizationPolicies.AdminPermission]
        };

        AgentSharedTokenClaimsNormalizer.Normalize(principal, options);

        Assert.Equal("user-1", principal.FindFirst("sub")?.Value);
        Assert.Equal("development", principal.FindFirst("tenant_id")?.Value);
        Assert.Equal(
            AgentAuthorizationPolicies.AdminPermission,
            principal.FindFirst("permission")?.Value);
    }

    [Fact]
    public void Not_map_tenant_or_permission_for_untrusted_values()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "user-1"),
                new Claim("TenantId", "other")
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var options = new AgentAuthenticationOptions
        {
            TenantId = "development",
            SharedTokenPermissions = [AgentAuthorizationPolicies.AdminPermission]
        };

        AgentSharedTokenClaimsNormalizer.Normalize(principal, options);

        Assert.Equal("user-1", principal.FindFirst("sub")?.Value);
        Assert.Null(principal.FindFirst("tenant_id"));
        Assert.Null(principal.FindFirst("permission"));
    }

    [Fact]
    public void Not_grant_agent_access_to_legacy_token_without_tenant()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim("jti", "user-1"),
                new Claim("SessionId", "session-1")
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var options = new AgentAuthenticationOptions
        {
            TenantId = "development",
            SharedTokenPermissions = [AgentAuthorizationPolicies.AdminPermission]
        };

        AgentSharedTokenClaimsNormalizer.Normalize(principal, options);

        Assert.Null(principal.FindFirst("sub"));
        Assert.Null(principal.FindFirst("tenant_id"));
        Assert.Null(principal.FindFirst("permission"));
    }

    [Fact]
    public void Preserve_native_agent_claims_without_duplicates()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "native-user"),
                new Claim("tenant_id", "development"),
                new Claim("permission", AgentAuthorizationPolicies.AdminPermission),
                new Claim(ClaimTypes.Name, "shared-user"),
                new Claim("TenantId", "0")
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var options = new AgentAuthenticationOptions
        {
            TenantId = "development",
            SharedTokenPermissions = [AgentAuthorizationPolicies.AdminPermission]
        };

        AgentSharedTokenClaimsNormalizer.Normalize(principal, options);

        Assert.Equal("native-user", principal.FindFirst("sub")?.Value);
        Assert.Single(principal.FindAll("tenant_id"));
        Assert.Single(principal.FindAll("permission"));
    }
}

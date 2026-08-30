#nullable enable

using System.Security.Claims;
using System.Text;
using EU.Core.Api.Agent.Security;
using EU.Core.AuthHelper;
using EU.Core.IServices.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgentIdentityClaims_Should
{
    [Fact]
    public void Build_caller_context_from_eu_core_api_claims()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "user-1"),
                    new Claim("TenantId", "tenant-1")
                ],
                "Bearer"))
        };

        var caller = new HttpCallerContext(new HttpContextAccessor
        {
            HttpContext = context
        });

        Assert.Equal("user-1", caller.UserId);
        Assert.Equal("tenant-1", caller.TenantId);
        Assert.Empty(caller.Permissions);
        Assert.Equal("trace-1", caller.CorrelationId);

        var identity = new AgentExecutionIdentity(
            caller.UserId,
            caller.TenantId,
            caller.Permissions,
            caller.CorrelationId);
        Assert.Empty(identity.Permissions);
    }

    [Fact]
    public void Reject_caller_with_multiple_tenant_claims()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "user-1"),
                    new Claim("TenantId", "0"),
                    new Claim("TenantId", "1")
                ],
                "Bearer"))
        };

        Assert.Throws<InvalidOperationException>(() =>
            new HttpCallerContext(new HttpContextAccessor
            {
                HttpContext = context
            }));
    }

    [Fact]
    public void Use_eu_core_api_token_claims_without_mapping()
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

        Assert.Equal("user-1", principal.FindFirst(AgentIdentityClaims.UserId)?.Value);
        Assert.Equal("0", principal.FindFirst(AgentIdentityClaims.Tenant)?.Value);
        Assert.Null(principal.FindFirst("sub"));
        Assert.Null(principal.FindFirst("tenant_id"));
        Assert.Null(principal.FindFirst(AgentIdentityClaims.Permission));
    }
}

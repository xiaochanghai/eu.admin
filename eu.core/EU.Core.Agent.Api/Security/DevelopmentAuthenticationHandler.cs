using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Options;
using EU.Core.Agent.Api.Configuration;

namespace EU.Core.Agent.Api.Security;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AgentAuthenticationOptions> authenticationOptions,
    IServer server)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AgentDevelopment";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!IsAllowedClient(Context, server))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "Development authentication is restricted to loopback clients."));
        }

        AgentAuthenticationOptions configuration = authenticationOptions.Value;
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, "development-operator"),
            new(configuration.UserIdClaimType, "development-operator"),
            new(configuration.TenantClaimType, configuration.TenantId),
            new(configuration.PermissionClaimType, AgentAuthorizationPolicies.AdminPermission),
            new(configuration.PermissionClaimType, AgentAuthorizationPolicies.DebugPermission),
            new(configuration.PermissionClaimType, AgentAuthorizationPolicies.ChatPermission),
            new(configuration.PermissionClaimType, AgentAuthorizationPolicies.AuditReadPermission),
            new(configuration.PermissionClaimType, AgentAuthorizationPolicies.BusinessDataReadPermission),
            new(configuration.PermissionClaimType, "business.sales.read"),
            new(configuration.PermissionClaimType, "business.sales.profit.read"),
            new(configuration.PermissionClaimType, "business.sales.customer.read")
        ];
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }

    private static bool IsAllowedClient(HttpContext context, IServer server)
    {
        if (context.Request.Headers.ContainsKey("Forwarded") ||
            context.Request.Headers.ContainsKey("X-Forwarded-For") ||
            context.Request.Headers.ContainsKey("X-Forwarded-Host") ||
            context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            return false;
        }

        IPAddress? remoteAddress = context.Connection.RemoteIpAddress;
        if (remoteAddress is not null)
        {
            IPAddress? localAddress = context.Connection.LocalIpAddress;
            return IPAddress.IsLoopback(remoteAddress) &&
                (localAddress is null || IPAddress.IsLoopback(localAddress));
        }

        return string.Equals(
            server.GetType().Assembly.GetName().Name,
            "Microsoft.AspNetCore.TestHost",
            StringComparison.Ordinal);
    }
}

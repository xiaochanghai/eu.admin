using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.Agent.Security;

internal static class AgentApiSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddAgentApiHttpSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        AgentAuthenticationOptions authentication = configuration
            .GetSection(AgentAuthenticationOptions.SectionName)
            .Get<AgentAuthenticationOptions>() ?? new AgentAuthenticationOptions();
        AgentRateLimitOptions rateLimit = configuration
            .GetSection(AgentRateLimitOptions.SectionName)
            .Get<AgentRateLimitOptions>() ?? new AgentRateLimitOptions();
        AgentHttpSecurityOptions httpSecurity = configuration
            .GetSection(AgentHttpSecurityOptions.SectionName)
            .Get<AgentHttpSecurityOptions>() ?? new AgentHttpSecurityOptions();

        services.AddSingleton<ExpensiveRequestAdmissionGate>();
        services.AddCors(options => options.AddPolicy(
            AgentHttpSecurityOptions.CorsPolicyName,
            policy =>
            {
                if (httpSecurity.AllowedOrigins.Count > 0)
                    policy.WithOrigins(httpSecurity.AllowedOrigins.ToArray());
                policy.WithMethods("GET", "POST", "PUT", "DELETE")
                    .WithHeaders(
                        "Authorization",
                        "Content-Type",
                        "Accept",
                        "X-Correlation-ID",
                        HttpIdempotencyMiddleware.HeaderName)
                    .WithExposedHeaders(
                        "X-Correlation-ID",
                        "Retry-After",
                        "Location",
                        ChatRunsController.RunIdHeaderName,
                        ChatRunsController.ConversationIdHeaderName,
                        HttpIdempotencyMiddleware.ReplayedHeaderName)
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }));
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!rateLimit.Enabled || !context.Request.Path.StartsWithSegments("/api"))
                    return RateLimitPartition.GetNoLimiter("not-limited");

                string userId = context.User.FindFirst(authentication.UserIdClaimType)?.Value
                    ?.Trim() ?? "anonymous";
                string workload = AgentWorkloadClassifier.IsExpensive(context.Request)
                    ? "expensive"
                    : "general";
                string partition = $"{workload}:{StablePartition(userId)}";
                int permitLimit = workload == "expensive"
                    ? rateLimit.ExpensivePermitLimit
                    : rateLimit.GeneralPermitLimit;
                return RateLimitPartition.GetFixedWindowLimiter(partition, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.RequestServices.GetRequiredService<AgentMetrics>()
                    .RecordResilience(AgentResilienceEvent.RateLimitRejected);
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";
                context.HttpContext.Response.Headers.RetryAfter =
                    rateLimit.WindowSeconds.ToString(CultureInfo.InvariantCulture);
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Too many requests.",
                    status = StatusCodes.Status429TooManyRequests,
                    errorCode = "AGENT_RATE_LIMIT_EXCEEDED",
                    traceId = context.HttpContext.TraceIdentifier,
                    code = "AGENT_RATE_LIMIT_EXCEEDED",
                    detail = "The request rate limit was exceeded. Retry after the indicated interval.",
                    correlationId = context.HttpContext.TraceIdentifier
                }, cancellationToken);
            };
        });

        string authenticationScheme =
            environment.IsDevelopment() && authentication.DevelopmentBypassEnabled
                ? DevelopmentAuthenticationHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme;
        AuthenticationBuilder authenticationBuilder = services
            .AddAuthentication(authenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authentication.Authority;
                options.Audience = authentication.Audience;
                options.RequireHttpsMetadata = authentication.RequireHttpsMetadata;
                options.MapInboundClaims = false;
            });
        if (string.Equals(
                authenticationScheme,
                DevelopmentAuthenticationHandler.SchemeName,
                StringComparison.Ordinal))
        {
            authenticationBuilder.AddScheme<AuthenticationSchemeOptions,
                DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationHandler.SchemeName,
                _ => { });
        }

        services.AddAuthorization(options =>
        {
            AuthorizationPolicy authenticated = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(authentication.UserIdClaimType)
                .RequireAssertion(context => HasFixedTenant(context, authentication))
                .Build();
            options.FallbackPolicy = authenticated;
            AddPermissionPolicy(options, AgentAuthorizationPolicies.Admin,
                AgentAuthorizationPolicies.AdminPermission, authentication);
            AddPermissionPolicy(options, AgentAuthorizationPolicies.Debug,
                AgentAuthorizationPolicies.DebugPermission, authentication);
            AddPermissionPolicy(options, AgentAuthorizationPolicies.Chat,
                AgentAuthorizationPolicies.ChatPermission, authentication);
            AddPermissionPolicy(options, AgentAuthorizationPolicies.AuditRead,
                AgentAuthorizationPolicies.AuditReadPermission, authentication);
            AddPermissionPolicy(options, AgentAuthorizationPolicies.ApprovalRead,
                AgentAuthorizationPolicies.ApprovalReadPermission, authentication);
            AddPermissionPolicy(options, AgentAuthorizationPolicies.ApprovalDecide,
                AgentAuthorizationPolicies.ApprovalDecidePermission, authentication);
            AddPermissionPolicy(options, AgentAuthorizationPolicies.ApprovalDecideHighRisk,
                AgentAuthorizationPolicies.ApprovalDecideHighRiskPermission, authentication);
            options.AddPolicy(
                AgentAuthorizationPolicies.HistoryRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(authentication.UserIdClaimType)
                    .RequireAssertion(context =>
                        HasFixedTenant(context, authentication)
                        && (HasPermission(
                                context.User,
                                authentication.PermissionClaimType,
                                AgentAuthorizationPolicies.ChatPermission)
                            || HasPermission(
                                context.User,
                                authentication.PermissionClaimType,
                                AgentAuthorizationPolicies.AuditReadPermission))));
        });

        return services;
    }

    private static string StablePartition(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash.AsSpan(0, 12));
    }

    private static void AddPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        string permission,
        AgentAuthenticationOptions authentication)
    {
        options.AddPolicy(policyName, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(authentication.UserIdClaimType)
            .RequireAssertion(context => HasFixedTenant(context, authentication))
            .RequireAssertion(context => context.User.Claims.Any(claim =>
                string.Equals(
                    claim.Type,
                    authentication.PermissionClaimType,
                    StringComparison.Ordinal) &&
                (string.Equals(claim.Value, permission, StringComparison.Ordinal) ||
                 string.Equals(
                     claim.Value,
                     AgentAuthorizationPolicies.AdminPermission,
                     StringComparison.Ordinal)))));
    }

    private static bool HasFixedTenant(
        AuthorizationHandlerContext context,
        AgentAuthenticationOptions authentication)
    {
        string[] tenantClaims = context.User.FindAll(authentication.TenantClaimType)
            .Select(claim => claim.Value)
            .ToArray();
        return tenantClaims.Length == 1 && string.Equals(
            tenantClaims[0],
            authentication.TenantId,
            StringComparison.Ordinal);
    }

    private static bool HasPermission(
        ClaimsPrincipal user,
        string claimType,
        string permission) =>
        user.Claims.Any(claim =>
            string.Equals(claim.Type, claimType, StringComparison.Ordinal)
            && (string.Equals(claim.Value, permission, StringComparison.Ordinal)
                || string.Equals(
                    claim.Value,
                    AgentAuthorizationPolicies.AdminPermission,
                    StringComparison.Ordinal)));
}

using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Observability;
using EU.Core.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

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
        services.AddPartitionedFixedWindowRateLimit(
            context =>
            {
                if (!rateLimit.Enabled || !context.Request.Path.StartsWithSegments("/api"))
                    return null;

                string userId = context.User.FindFirst(authentication.UserIdClaimType)?.Value
                    ?.Trim() ?? "anonymous";
                string workload = AgentWorkloadClassifier.IsExpensive(context.Request)
                    ? "expensive"
                    : "general";
                string partition = $"{workload}:{StablePartition(userId)}";
                int permitLimit = workload == "expensive"
                    ? rateLimit.ExpensivePermitLimit
                    : rateLimit.GeneralPermitLimit;
                return new FixedWindowRateLimitPartition(
                    partition,
                    permitLimit,
                    TimeSpan.FromSeconds(rateLimit.WindowSeconds));
            },
            async (context, cancellationToken) =>
            {
                context.HttpContext.RequestServices.GetRequiredService<AgentMetrics>()
                    .RecordResilience(AgentResilienceEvent.RateLimitRejected);
                context.HttpContext.Response.Headers.RetryAfter =
                    rateLimit.WindowSeconds.ToString(CultureInfo.InvariantCulture);
                await AgentApiErrorResponseWriter.WriteAsync(
                    context.HttpContext,
                    "AGENT_RATE_LIMIT_EXCEEDED",
                    "The request rate limit was exceeded. Retry after the indicated interval.",
                    cancellationToken: cancellationToken);
            });

        string authenticationScheme =
            environment.IsDevelopment() && authentication.DevelopmentBypassEnabled
                ? DevelopmentAuthenticationHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme;
        if (string.Equals(
                authenticationScheme,
                DevelopmentAuthenticationHandler.SchemeName,
                StringComparison.Ordinal))
        {
            services.AddAuthentication(authenticationScheme)
                .AddScheme<AuthenticationSchemeOptions,
                    DevelopmentAuthenticationHandler>(
                    DevelopmentAuthenticationHandler.SchemeName,
                    _ => { });
        }
        else
        {
            services.AddAuthenticationSetup(
                new JwtBearerAuthenticationSchemes(authenticationScheme));
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    JwtBearerEvents events = options.Events ?? new JwtBearerEvents();
                    Func<TokenValidatedContext, Task> previous = events.OnTokenValidated;
                    events.OnTokenValidated = async context =>
                    {
                        await previous(context);
                        if (context.Result?.Failure is null && context.Principal is not null)
                        {
                            AgentSharedTokenClaimsNormalizer.Normalize(
                                context.Principal,
                                authentication);
                        }
                    };
                    options.Events = events;
                });
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
                        && (!authentication.EnforcePermissionClaims
                            || HasPermission(
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
            .RequireAssertion(context =>
                HasRequiredPermission(context.User, authentication, permission)));
    }

    internal static bool HasRequiredPermission(
        ClaimsPrincipal user,
        AgentAuthenticationOptions authentication,
        string permission) =>
        !authentication.EnforcePermissionClaims ||
        HasPermission(user, authentication.PermissionClaimType, permission);

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

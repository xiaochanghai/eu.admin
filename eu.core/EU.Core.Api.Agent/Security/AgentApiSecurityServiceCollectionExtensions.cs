using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Observability;
using EU.Core.Common.HttpContextUser;
using EU.Core.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.Agent.Security;

internal static class AgentApiSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddAgentApiHttpSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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

                string userId = context.RequestServices.GetService<IUser>()?.ID
                    ?.ToString("D") ?? "anonymous";
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

        services.AddAuthenticationAndAuthorizationSetup(
            new JwtBearerAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));

        services.AddAuthorization(options =>
        {
            AuthorizationPolicy authenticated = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.FallbackPolicy = authenticated;
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.Admin);
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.Debug);
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.Chat);
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.AuditRead);
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.ApprovalRead);
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.ApprovalDecide);
            AddAuthenticatedPolicy(options, AgentAuthorizationPolicies.ApprovalDecideHighRisk);
            options.AddPolicy(
                AgentAuthorizationPolicies.HistoryRead,
                policy => policy
                    .RequireAuthenticatedUser());
        });

        return services;
    }

    private static string StablePartition(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash.AsSpan(0, 12));
    }

    private static void AddAuthenticatedPolicy(
        AuthorizationOptions options,
        string policyName)
    {
        options.AddPolicy(policyName, policy => policy
            .RequireAuthenticatedUser());
    }
}

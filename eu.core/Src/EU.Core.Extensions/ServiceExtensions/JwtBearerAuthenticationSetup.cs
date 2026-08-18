#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.Extensions;

/// <summary>
/// JWT Bearer 认证方案注册配置。
/// </summary>
public sealed record JwtBearerAuthenticationSchemes
{
    public JwtBearerAuthenticationSchemes(
        string authenticateScheme,
        string? challengeScheme = null,
        string? forbidScheme = null)
    {
        if (string.IsNullOrWhiteSpace(authenticateScheme))
            throw new ArgumentException("The authentication scheme is required.", nameof(authenticateScheme));

        AuthenticateScheme = authenticateScheme;
        ChallengeScheme = string.IsNullOrWhiteSpace(challengeScheme)
            ? authenticateScheme
            : challengeScheme;
        ForbidScheme = string.IsNullOrWhiteSpace(forbidScheme)
            ? ChallengeScheme
            : forbidScheme;
    }

    public string AuthenticateScheme { get; }

    public string ChallengeScheme { get; }

    public string ForbidScheme { get; }
}

/// <summary>
/// 统一注册 JWT Bearer 认证，同时允许宿主保留自己的验证参数和失败响应方案。
/// </summary>
public static class JwtBearerAuthenticationSetup
{
    public static AuthenticationBuilder AddJwtBearerAuthentication(
        this IServiceCollection services,
        JwtBearerAuthenticationSchemes schemes,
        Action<JwtBearerOptions> configureJwtBearer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(schemes);
        ArgumentNullException.ThrowIfNull(configureJwtBearer);

        return services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = schemes.AuthenticateScheme;
                options.DefaultAuthenticateScheme = schemes.AuthenticateScheme;
                options.DefaultChallengeScheme = schemes.ChallengeScheme;
                options.DefaultForbidScheme = schemes.ForbidScheme;
            })
            .AddJwtBearer(
                schemes.AuthenticateScheme,
                configureJwtBearer);
    }
}

using EU.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;

namespace EU.Core.Extensions;

/// <summary>
/// 统一注册认证与授权服务。
/// </summary>
public static class AuthenticationSetup
{
    public static void AddAuthenticationAndAuthorizationSetup(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddAuthorizationSetup();
        services.AddAuthenticationSetup();
    }

    public static void AddAuthenticationSetup(
        this IServiceCollection services,
        JwtBearerAuthenticationSchemes schemes = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        Permissions.IsUseIds4 = AppSettings.app(["Startup", "IdentityServer4", "Enabled"]).ObjToBool();
        Permissions.IsUseAuthing = AppSettings.app(["Startup", "Authing", "Enabled"]).ObjToBool();

        if (Permissions.IsUseIds4 && Permissions.IsUseAuthing)
        {
            throw new InvalidOperationException(
                "IdentityServer4 and Authing cannot be enabled at the same time. Configure only one authentication provider.");
        }

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        if (Permissions.IsUseIds4)
        {
            services.AddAuthentication_Ids4Setup(schemes);
        }
        else if (Permissions.IsUseAuthing)
        {
            services.AddAuthentication_AuthingSetup(schemes);
        }
        else
        {
            services.AddAuthentication_JWTSetup(schemes);
        }
    }

}

using EU.Core.Common.Caches;
using EU.Core.Common.Helper;
using EU.Core.Model.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EU.Core.AuthHelper;

/// <summary>
/// JWTToken生成类
/// </summary>
public class JwtToken
{
    private static RedisCacheService Redis = new();

    /// <summary>
    /// 获取基于JWT的Token
    /// </summary>
    /// <param name="claims">需要在登陆的时候配置</param>
    /// <param name="permissionRequirement">在startup中定义的参数</param>
    /// <returns></returns>
    public static TokenInfoViewModel BuildJwtToken(Claim[] claims, PermissionRequirement permissionRequirement, string sessionId = null)
    {
        var now = DateTime.Now;
        // 实例化JwtSecurityToken
        var jwt = new JwtSecurityToken(
            issuer: permissionRequirement.Issuer,
            audience: permissionRequirement.Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(permissionRequirement.Expiration),
            signingCredentials: permissionRequirement.SigningCredentials
        );
        // 生成 Token
        var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

        //打包返回前台
        var responseJson = new TokenInfoViewModel
        {
            success = true,
            token = encodedJwt,
            expires_in = permissionRequirement.Expiration.TotalSeconds,
            token_type = "Bearer"
        };
        //var keys = Redis.Exists1(userId.ObjToString());
        #region 写入Redis
        if (sessionId != null)
            Redis.Add(sessionId, responseJson.token, permissionRequirement.Expiration);
        #endregion

        return responseJson;
    }
}

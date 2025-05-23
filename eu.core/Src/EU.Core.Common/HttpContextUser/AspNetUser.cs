using EU.Core.Common.Swagger;
using EU.Core.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EU.Core.Common.HttpContextUser;

public class AspNetUser : IUser
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<AspNetUser> _logger;

    public AspNetUser(IHttpContextAccessor accessor, ILogger<AspNetUser> logger)
    {
        _accessor = accessor;
        _logger = logger;
    }

    public string Name => GetName();

    private string GetName()
    {
        if (IsAuthenticated() && _accessor.HttpContext.User.Identity.Name.IsNotEmptyOrNull())
        {
            return _accessor.HttpContext.User.Identity.Name;
        }
        else
        {
            if (!string.IsNullOrEmpty(GetToken()))
            {
                var getNameType = Permissions.IsUseIds4
                    ? "name"
                    : "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
                return GetUserInfoFromToken(getNameType).FirstOrDefault().ObjToString();
            }
        }

        return "";
    }

    public Guid? ID => GetClaimValueByType("jti").FirstOrDefault().ObjToGuid();
    public long TenantId => GetClaimValueByType("TenantId").FirstOrDefault().ObjToLong();

    public bool IsAuthenticated()
    {
        return _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }


    public string GetToken()
    {
        var token = _accessor.HttpContext?.Request?.Headers["Authorization"].ObjToString().Replace("Bearer ", "");
        if (!token.IsNullOrEmpty())
        {
            return token;
        }

        if (_accessor.HttpContext?.IsSuccessSwagger() == true)
        {
            token = _accessor.HttpContext.GetSuccessSwaggerJwt();
            if (token.IsNotEmptyOrNull())
            {
                if (_accessor.HttpContext.User.Claims.Any(s => s.Type == JwtRegisteredClaimNames.Jti))
                {
                    return token;
                }

                var claims = new ClaimsIdentity(GetClaimsIdentity(token));
                _accessor.HttpContext.User.AddIdentity(claims);
                return token;
            }
        }

        return token;
    }

    public string GetPlatform()
    {
        var platform = _accessor.HttpContext?.Request?.Headers["Platform"].ObjToString();
        if (!platform.IsNullOrEmpty())
            return platform;

        return null;
    }

    public List<string> GetUserInfoFromToken(string ClaimType)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var token = "";

        token = GetToken();
        // token校验
        if (token.IsNotEmptyOrNull() && jwtHandler.CanReadToken(token))
        {
            JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(token);

            return (from item in jwtToken.Claims
                    where item.Type == ClaimType
                    select item.Value).ToList();
        }

        return new List<string>() { };
    }

    public ServiceResult<string> MessageModel { get; set; }

    public IEnumerable<Claim> GetClaimsIdentity()
    {
        if (_accessor.HttpContext == null) return ArraySegment<Claim>.Empty;

        if (!IsAuthenticated()) return GetClaimsIdentity(GetToken());

        if (_accessor.HttpContext == null) return ArraySegment<Claim>.Empty;
        var claims = _accessor.HttpContext.User.Claims.ToList();
        var headers = _accessor.HttpContext.Request.Headers;
        foreach (var header in headers)
        {
            claims.Add(new Claim(header.Key, header.Value));
        }

        return claims;
    }

    public IEnumerable<Claim> GetClaimsIdentity(string token)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        // token校验
        if (token.IsNotEmptyOrNull() && jwtHandler.CanReadToken(token))
        {
            var jwtToken = jwtHandler.ReadJwtToken(token);

            return jwtToken.Claims;
        }

        return new List<Claim>();
    }

    public List<string> GetClaimValueByType(string ClaimType)
    {
        return (from item in GetClaimsIdentity()
                where item.Type == ClaimType
                select item.Value).ToList();
    }
}
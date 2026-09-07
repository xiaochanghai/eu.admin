using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EU.Core.Extensions;

/// <summary>从现有本地 JWT 配置派生用途隔离的签名材料，不读取另一套凭据。</summary>
public static class JwtPurposeSigningKey
{
    /// <summary>派生指定用途的 256 位密钥；调用方负责清零返回值。</summary>
    /// <param name="options">现有 Bearer 认证方案配置。</param>
    /// <param name="purpose">双方约定的固定协议用途，不能来自请求输入。</param>
    /// <returns>用途隔离的签名材料。</returns>
    public static byte[] Derive(IOptionsMonitor<JwtBearerOptions> options, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (options.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.IssuerSigningKey
            is not SymmetricSecurityKey key || key.KeySize < 256)
        {
            throw new InvalidOperationException("A local JWT signing key of at least 256 bits is required.");
        }

        // key.Key belongs to the shared JWT configuration: do not clear it.
        return HMACSHA256.HashData(key.Key, Encoding.UTF8.GetBytes(purpose));
    }
}

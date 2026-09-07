using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>复用项目 JWT 配置；保留类型名兼容宿主注册，不再区分开发环境。</summary>
public sealed class DevelopmentBusinessQuerySigningKeyResolver(
    IOptionsMonitor<JwtBearerOptions> jwtOptions) : IBusinessQuerySigningKeyResolver
{

    public ValueTask<byte[]> ResolveAsync(
        string alias,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(JwtPurposeSigningKey.Derive(jwtOptions, "EU.Core.BusinessQuery.ExecutionContext.v1"));
    }
}

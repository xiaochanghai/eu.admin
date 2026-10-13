using EU.Core.Agent.Runtime;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>将单例 Agent 运行时与按作用域解析的标准模型配置 Service 连接，不直接访问数据库。</summary>
public sealed class AgModelProfileResolver(IServiceScopeFactory scopeFactory, ILogger<AgModelProfileResolver>? logger = null) : IAgentModelProfileResolver
{
    #region 解析运行配置（ResolveAsync）
    /// <summary>每次运行重新解析数据库配置；禁用、删除或解密失败时禁止旧配置回退。</summary>
    /// <param name="profileCode">运行快照引用的模型标识。</param>
    /// <param name="cancellationToken">取消本次解析的令牌。</param>
    /// <returns>本次调用使用的模型配置，凭据仅保留在服务端内存。</returns>
    public async Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IAgModelConfigServices>().ResolveRuntimeProfileAsync(profileCode, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // 不记录异常正文、内部异常或配置值，避免连接串及凭据进入日志。
            logger?.LogWarning("Model configuration resolution failed. ErrorCode={ErrorCode}; ExceptionType={ExceptionType}",
                AgentRunErrorCodes.ModelConfigurationUnavailable, exception.GetType().Name);
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelConfigurationUnavailable,
                "模型配置不可用，请检查启用状态、配置字段及 Redis 主密钥。");
        }
    }
    #endregion
}

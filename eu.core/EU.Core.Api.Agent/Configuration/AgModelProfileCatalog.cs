using EU.Core.Agent.Runtime;
using EU.Core.IServices;
using EU.Core.IServices.Agents;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>将单例 Agent 运行时与按作用域解析的标准模型配置 Service 连接，不直接访问数据库。</summary>
public sealed class AgModelProfileCatalog(IServiceScopeFactory scopeFactory) : IPublicModelProfileCatalog, IAgentModelProfileResolver
{
    #region 获取模型目录（ListAsync）
    /// <summary>读取当前已启用的公开模型标识，不缓存凭据或服务实例。</summary>
    /// <param name="cancellationToken">取消本次读取的令牌。</param>
    /// <returns>可公开选择的模型标识集合。</returns>
    public async Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAgModelConfigServices>().ListAvailableProfilesAsync(cancellationToken);
    }
    #endregion

    #region 校验引用（ExistsAsync）
    /// <summary>按区分大小写的标识检查模型是否仍允许选择和发布。</summary>
    /// <param name="modelProfileId">待检查的公开模型标识。</param>
    /// <param name="cancellationToken">取消本次检查的令牌。</param>
    /// <returns>当前目录包含该标识时返回 true。</returns>
    public async Task<bool> ExistsAsync(string modelProfileId, CancellationToken cancellationToken = default) =>
        (await ListAsync(cancellationToken)).Contains(modelProfileId, StringComparer.Ordinal);
    #endregion

    #region 解析运行配置（ResolveAsync）
    /// <summary>每次运行重新解析数据库配置；禁用、删除或解密失败时禁止旧配置回退。</summary>
    /// <param name="profileCode">运行快照引用的模型标识。</param>
    /// <param name="cancellationToken">取消本次解析的令牌。</param>
    /// <returns>本次调用使用的模型配置，凭据仅保留在服务端内存。</returns>
    public async Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAgModelConfigServices>().ResolveRuntimeProfileAsync(profileCode, cancellationToken);
    }
    #endregion
}

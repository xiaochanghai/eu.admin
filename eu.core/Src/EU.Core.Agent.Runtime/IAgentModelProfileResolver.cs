using EU.Core.IServices;

namespace EU.Core.Agent.Runtime;

/// <summary>运行时模型配置解析边界；持久化仍由现有业务 Service 负责。</summary>
public interface IAgentModelProfileResolver
{
    /// <summary>解析一次运行所需的模型配置。</summary>
    /// <param name="profileCode">Agent 快照中的公开配置标识。</param>
    /// <param name="cancellationToken">取消本次解析的令牌。</param>
    /// <returns>当前有效的模型配置，禁止将凭据写入快照或事件。</returns>
    Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default);
}

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgModelConfig.cs
*
* 功 能： N / A
* 类 名： AgModelConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/9/9 23:28:44  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 平台共享模型配置，API 密钥仅以密文持久化。(自定义服务接口)
/// </summary>	
public interface IAgModelConfigServices : IBaseServices<AgModelConfig, AgModelConfigDto, InsertAgModelConfigInput, EditAgModelConfigInput>
{
    /// <summary>获取允许 Agent 使用的公开模型标识（ListAvailableProfilesAsync）。</summary>
    /// <param name="cancellationToken">取消当前查询的令牌。</param>
    /// <returns>已启用且未删除的合法模型标识；不包含凭据。</returns>
    Task<IReadOnlyList<string>> ListAvailableProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>读取当前模型调用配置（ResolveRuntimeProfileAsync），仅供服务端使用。</summary>
    /// <param name="profileCode">Agent 保存的区分大小写的公开模型标识。</param>
    /// <param name="cancellationToken">取消当前查询的令牌。</param>
    /// <returns>校验并解密后的单次调用配置；无效、禁用或缺失时抛出安全异常。</returns>
    Task<AgentModelRuntimeProfile> ResolveRuntimeProfileAsync(string profileCode, CancellationToken cancellationToken = default);
}

/// <summary>仅在服务端内存中使用的模型调用配置，不作为管理 API 的 DTO。</summary>
public sealed class AgentModelRuntimeProfile
{
    /// <summary>模型服务的 HTTPS 基础地址。</summary>
    public Uri Endpoint { get; init; }
    /// <summary>提供商实际模型名，而不是 Agent 引用标识。</summary>
    public string ModelName { get; init; }
    /// <summary>单次调用超时。</summary>
    public TimeSpan Timeout { get; init; }
    /// <summary>Qwen 思考开关；null 保持供应商默认。</summary>
    public bool? EnableThinking { get; init; }
    /// <summary>单次调用明文凭据，禁止序列化或日志输出。</summary>
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public string ApiKey { get; init; }
    /// <summary>日志只显示类型名，不展开调用配置。</summary>
    public override string ToString() => nameof(AgentModelRuntimeProfile);
}

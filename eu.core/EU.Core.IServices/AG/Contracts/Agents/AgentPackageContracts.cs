#nullable enable

using System.Text.Json.Serialization;

namespace EU.Core.IServices.Agents;

/// <summary>
/// Agent 导入导出包的第一版根契约。
/// </summary>
/// <param name="Format">包格式标识。</param>
/// <param name="Version">包格式版本。</param>
/// <param name="Agent">包中包含的 Agent 定义。</param>
public sealed record AgentPackageV1(
    [property: JsonPropertyOrder(0)] string Format,
    [property: JsonPropertyOrder(1)] string Version,
    [property: JsonPropertyOrder(2)] AgentPackageAgentV1 Agent);

/// <summary>
/// Agent 包中的定义信息。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="RuntimeStatus">Agent 的运行状态。</param>
/// <param name="Draft">Agent 草稿配置。</param>
/// <param name="Deployment">目标部署信息。</param>
/// <param name="Skills">绑定的技能版本引用集合。</param>
/// <param name="Tools">绑定的工具版本引用集合。</param>
/// <param name="KnowledgeBases">绑定的知识库引用集合。</param>
public sealed record AgentPackageAgentV1(
    [property: JsonPropertyOrder(0)] string Code,
    [property: JsonPropertyOrder(1)] string Name,
    [property: JsonPropertyOrder(2)] string Description,
    [property: JsonPropertyOrder(3)] string RuntimeStatus,
    [property: JsonPropertyOrder(4)] AgentPackageDraftV1 Draft,
    [property: JsonPropertyOrder(5)] AgentPackageDeploymentV1 Deployment,
    [property: JsonPropertyOrder(6)] IReadOnlyList<string> Skills,
    [property: JsonPropertyOrder(7)] IReadOnlyList<string> Tools,
    [property: JsonPropertyOrder(8)]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? KnowledgeBases = null)
{
    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    /// <summary>
    /// 绑定的子 Agent 版本集合。
    /// </summary>
    public IReadOnlyList<AgentPackageChildBindingV1>? ChildAgents { get; init; }

    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    /// <summary>
    /// 绑定的编排版本集合。
    /// </summary>
    public IReadOnlyList<AgentPackageOrchestrationBindingV1>? Orchestrations { get; init; }
}

/// <summary>
/// Agent 包中的子 Agent 绑定。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
public sealed record AgentPackageChildBindingV1(
    string AgentId,
    string AgentVersionId);

/// <summary>
/// Agent 包中的编排绑定。
/// </summary>
/// <param name="OrchestrationId">编排定义标识。</param>
/// <param name="OrchestrationVersionId">编排版本标识。</param>
public sealed record AgentPackageOrchestrationBindingV1(
    string OrchestrationId,
    string OrchestrationVersionId);

/// <summary>
/// Agent 包中的草稿配置。
/// </summary>
/// <param name="Instructions">Agent 的系统指令。</param>
/// <param name="ModelProfileId">模型配置标识。</param>
/// <param name="OutputMode">输出内容模式。</param>
/// <param name="OutputJsonSchema">约束结构化输出的 JSON Schema。</param>
public sealed record AgentPackageDraftV1(
    [property: JsonPropertyOrder(0)] string Instructions,
    [property: JsonPropertyOrder(1)] string ModelProfileId,
    [property: JsonPropertyOrder(2)] string OutputMode,
    [property: JsonPropertyOrder(3)] string? OutputJsonSchema);

/// <summary>
/// Agent 包的目标部署信息。
/// </summary>
/// <param name="Target">部署目标。</param>
/// <param name="Host">目标宿主。</param>
public sealed record AgentPackageDeploymentV1(
    [property: JsonPropertyOrder(0)] string Target,
    [property: JsonPropertyOrder(1)] string Host);

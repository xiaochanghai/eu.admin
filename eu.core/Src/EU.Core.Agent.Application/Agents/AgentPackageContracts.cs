using System.Text.Json.Serialization;

namespace EU.Core.Agent.Application.Agents;

public sealed record AgentPackageV1(
    [property: JsonPropertyOrder(0)] string Format,
    [property: JsonPropertyOrder(1)] string Version,
    [property: JsonPropertyOrder(2)] AgentPackageAgentV1 Agent);

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
    public IReadOnlyList<AgentPackageChildBindingV1>? ChildAgents { get; init; }

    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<AgentPackageOrchestrationBindingV1>? Orchestrations { get; init; }
}

public sealed record AgentPackageChildBindingV1(
    string AgentId,
    string AgentVersionId);

public sealed record AgentPackageOrchestrationBindingV1(
    string OrchestrationId,
    string OrchestrationVersionId);

public sealed record AgentPackageDraftV1(
    [property: JsonPropertyOrder(0)] string Instructions,
    [property: JsonPropertyOrder(1)] string ModelProfileId,
    [property: JsonPropertyOrder(2)] string OutputMode,
    [property: JsonPropertyOrder(3)] string? OutputJsonSchema);

public sealed record AgentPackageDeploymentV1(
    [property: JsonPropertyOrder(0)] string Target,
    [property: JsonPropertyOrder(1)] string Host);

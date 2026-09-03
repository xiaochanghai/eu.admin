#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.Model.ViewModels.Extend;

public enum AgentOutputMode
{
    Text,
    Structured
}

public enum AgentRuntimeStatus
{
    Enabled,
    Disabled,
    Archived
}

public sealed record AgentSkillBindingSnapshot(Guid SkillVersionId);

public sealed record AgentToolBindingSnapshot(Guid ToolVersionId);

public sealed record AgentKnowledgeBindingSnapshot(Guid KnowledgeBaseId, long LogicalRevision);

public sealed record AgentChildBindingSnapshot(Guid AgentId, Guid AgentVersionId)
{
    public string AgentCode { get; init; } = string.Empty;

    public string? AgentName { get; init; }

    public string? AgentDescription { get; init; }
}

public static class AgentDelegationPolicy
{
    public const int MaximumChildAgentBindings = 8;
}

public sealed record AgentOrchestrationBindingSnapshot(Guid OrchestrationId, Guid OrchestrationVersionId);

public sealed record AgentVersionSnapshot(
    Guid VersionId,
    string AgentCode,
    string Instructions,
    string ModelProfileId,
    AgentOutputMode OutputMode,
    string? OutputJsonSchema,
    IReadOnlyList<AgentSkillBindingSnapshot> Skills,
    IReadOnlyList<AgentToolBindingSnapshot> Tools)
{
    public string? AgentName { get; init; }

    public string? AgentDescription { get; init; }

    public IReadOnlyList<AgentKnowledgeBindingSnapshot> KnowledgeBases { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<AgentKnowledgeBindingSnapshot>());

    public IReadOnlyList<AgentChildBindingSnapshot> ChildAgents { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<AgentChildBindingSnapshot>());

    public IReadOnlyList<AgentOrchestrationBindingSnapshot> Orchestrations { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<AgentOrchestrationBindingSnapshot>());
}

public sealed record AgentVersion(
    Guid Id,
    string Label,
    bool IsDraft,
    string Instructions,
    string ModelProfileId,
    AgentOutputMode OutputMode,
    string? OutputJsonSchema,
    string? OutputSchemaSha256,
    AgentVersionSnapshot? Snapshot)
{
    public IReadOnlyList<Guid> SkillVersionIds { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<Guid>());

    public IReadOnlyList<Guid> ToolVersionIds { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<Guid>());

    public IReadOnlyList<Guid> KnowledgeBaseIds { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<Guid>());

    public IReadOnlyList<Guid> ChildAgentIds { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<Guid>());

    public IReadOnlyList<Guid> OrchestrationIds { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<Guid>());

    public IReadOnlyList<AgentChildBindingSnapshot> ChildAgentPins { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<AgentChildBindingSnapshot>());

    public IReadOnlyList<AgentOrchestrationBindingSnapshot> OrchestrationPins { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<AgentOrchestrationBindingSnapshot>());
}

public sealed record AgentDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    AgentRuntimeStatus RuntimeStatus,
    long LogicalRevision,
    AgentVersion Draft,
    IReadOnlyList<AgentVersion> PublishedVersions)
{
    public const string ServerDeploymentTarget = "Server";
    public const string ApiHost = "EU.Core.Api.Agent";
    public const string LegacyApiHost = "EU.Core.Agent.Api";

    public string DeploymentTarget => ServerDeploymentTarget;

    public string Host => ApiHost;
}

public sealed record CreateAgentCommand(string Code, string Name = "", string Description = "");

public sealed record SaveAgentDraftCommand(
    Guid AgentId,
    long ExpectedLogicalRevision,
    string Instructions,
    string ModelProfileId,
    AgentOutputMode OutputMode,
    string? OutputJsonSchema,
    string? Name = null,
    string? Description = null,
    IReadOnlyList<Guid>? SkillVersionIds = null,
    IReadOnlyList<Guid>? ToolVersionIds = null,
    IReadOnlyList<Guid>? KnowledgeBaseIds = null)
{
    public IReadOnlyList<Guid>? ChildAgentIds { get; init; }
    public IReadOnlyList<Guid>? OrchestrationIds { get; init; }
    public IReadOnlyList<AgentChildBindingSnapshot>? ChildAgentPins { get; init; }
    public IReadOnlyList<AgentOrchestrationBindingSnapshot>? OrchestrationPins { get; init; }
}

public sealed record PublishAgentCommand(Guid AgentId, long ExpectedLogicalRevision);

public sealed record SetAgentRuntimeStatusCommand(
    Guid AgentId,
    long ExpectedLogicalRevision,
    AgentRuntimeStatus RuntimeStatus);

public sealed record ImportAgentCommand(
    string Code,
    string Name,
    string Description,
    AgentRuntimeStatus RuntimeStatus,
    string Instructions,
    string ModelProfileId,
    AgentOutputMode OutputMode,
    string? OutputJsonSchema,
    IReadOnlyList<Guid>? SkillVersionIds = null,
    IReadOnlyList<Guid>? ToolVersionIds = null,
    IReadOnlyList<Guid>? KnowledgeBaseIds = null)
{
    public IReadOnlyList<Guid>? ChildAgentIds { get; init; }
    public IReadOnlyList<Guid>? OrchestrationIds { get; init; }
    public IReadOnlyList<AgentChildBindingSnapshot>? ChildAgentPins { get; init; }
    public IReadOnlyList<AgentOrchestrationBindingSnapshot>? OrchestrationPins { get; init; }
}

public sealed record AgentDefinitionQuery(string? Search = null, AgentRuntimeStatus? RuntimeStatus = null);

public sealed record AgentListItem(
    Guid Id,
    string Code,
    string Name,
    string Description,
    AgentRuntimeStatus RuntimeStatus,
    long LogicalRevision,
    string DraftLabel,
    string DraftModelProfileId,
    string? CurrentPublishedLabel);

public interface IAgentDefinitionCatalog
{
    Task<AgentDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default);
}

public static class AgentContractCloner
{
    public static AgentDefinition Clone(AgentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition with
        {
            Draft = Clone(definition.Draft),
            PublishedVersions = ReadOnly(definition.PublishedVersions.Select(Clone))
        };
    }

    public static AgentVersion Clone(AgentVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version with
        {
            Snapshot = version.Snapshot is null ? null : Clone(version.Snapshot),
            SkillVersionIds = ReadOnly(version.SkillVersionIds),
            ToolVersionIds = ReadOnly(version.ToolVersionIds),
            KnowledgeBaseIds = ReadOnly(version.KnowledgeBaseIds),
            ChildAgentIds = ReadOnly(version.ChildAgentIds),
            OrchestrationIds = ReadOnly(version.OrchestrationIds),
            ChildAgentPins = ReadOnly(version.ChildAgentPins.Select(value => value with { })),
            OrchestrationPins = ReadOnly(version.OrchestrationPins.Select(value => value with { }))
        };
    }

    public static AgentVersionSnapshot Clone(AgentVersionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot with
        {
            Skills = ReadOnly(snapshot.Skills.Select(skill => skill with { })),
            Tools = ReadOnly(snapshot.Tools.Select(tool => tool with { })),
            KnowledgeBases = ReadOnly(snapshot.KnowledgeBases.Select(value => value with { })),
            ChildAgents = ReadOnly(snapshot.ChildAgents.Select(value => value with { })),
            Orchestrations = ReadOnly(snapshot.Orchestrations.Select(value => value with { }))
        };
    }

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

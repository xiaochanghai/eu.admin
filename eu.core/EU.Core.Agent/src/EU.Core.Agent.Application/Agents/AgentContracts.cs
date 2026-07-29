using System.Collections.ObjectModel;

namespace EU.Core.Agent.Application.Agents;

public enum AgentOutputMode
{
    Text,
    Structured
}

public enum AgentRuntimeStatus
{
    Enabled,
    Disabled
}

public sealed record AgentSkillBindingSnapshot(Guid SkillVersionId);

public sealed record AgentToolBindingSnapshot(Guid ToolVersionId);

public sealed record AgentKnowledgeBindingSnapshot(Guid KnowledgeBaseId, long LogicalRevision);

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
    public IReadOnlyList<AgentKnowledgeBindingSnapshot> KnowledgeBases { get; init; } =
        AgentContractCloner.ReadOnly(Array.Empty<AgentKnowledgeBindingSnapshot>());
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
    public const string ApiHost = "EU.Core.Agent.Api";

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
    IReadOnlyList<Guid>? KnowledgeBaseIds = null);

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
    IReadOnlyList<Guid>? KnowledgeBaseIds = null);

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

public sealed record AgentError(string Code, string Message);

public static class AgentErrorCodes
{
    public const string CodeConflict = "AGENT_CODE_CONFLICT";
    public const string RowVersionConflict = "AGENT_ROW_VERSION_CONFLICT";
    public const string VersionNotPublishable = "AGENT_VERSION_NOT_PUBLISHABLE";
    public const string OutputSchemaInvalid = "OUTPUT_SCHEMA_INVALID";
    public const string CodeInvalid = "AGENT_CODE_INVALID";
    public const string NotFound = "AGENT_NOT_FOUND";
    public const string RuntimeStatusInvalid = "AGENT_RUNTIME_STATUS_INVALID";
    public const string PackageInvalid = "AGENT_PACKAGE_INVALID";
    public const string PackageVersionUnsupported = "AGENT_PACKAGE_VERSION_UNSUPPORTED";
    public const string ReferenceMissing = "AGENT_REFERENCE_MISSING";
    public const string SkillVersionNotPublished = "SKILL_VERSION_NOT_PUBLISHED";
    public const string ToolVersionNotAvailable = "MCP_TOOL_VERSION_NOT_AVAILABLE";
    public const string KnowledgeBaseUnavailable = "KNOWLEDGE_BASE_UNAVAILABLE";
}

public sealed record AgentOperationResult<T>(T? Value, AgentError? Error)
{
    public bool Succeeded => Error is null;

    public static AgentOperationResult<T> Success(T value) => new(value, null);

    public static AgentOperationResult<T> Failure(string code, string message) => new(default, new AgentError(code, message));
}

public interface IAgentRepository
{
    Task<AgentDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AgentDefinition?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentDefinition>> ListAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(AgentDefinition definition, CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(AgentDefinition definition, long expectedLogicalRevision, CancellationToken cancellationToken = default);
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
            KnowledgeBaseIds = ReadOnly(version.KnowledgeBaseIds)
        };
    }

    public static AgentVersionSnapshot Clone(AgentVersionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot with
        {
            Skills = ReadOnly(snapshot.Skills.Select(skill => skill with { })),
            Tools = ReadOnly(snapshot.Tools.Select(tool => tool with { })),
            KnowledgeBases = ReadOnly(snapshot.KnowledgeBases.Select(value => value with { }))
        };
    }

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

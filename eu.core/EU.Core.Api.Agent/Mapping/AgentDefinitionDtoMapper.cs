using EU.Core.Agent.Application.Agents;
using EU.Core.Model.Entity;
using EU.Core.Model.Models;

namespace EU.Core.Api.Agent;

internal static class AgentDefinitionDtoMapper
{
    private const string VersionScope = "Version";
    private const string SnapshotScope = "Snapshot";
    private static readonly string[] BindingTypes =
        ["Skill", "Tool", "KnowledgeBase", "ChildAgent", "Orchestration"];

    public static AgentDefinition Map(AgAgentDefinitionDetailDto source)
    {
        ArgumentNullException.ThrowIfNull(source);
        AgAgentDefinition definition = source.Definition
            ?? throw new InvalidDataException("The Agent detail does not contain a definition.");
        AgAgentVersionDetailDto draft = source.Versions.SingleOrDefault(value => value.Version.IsDraft == true)
            ?? throw new InvalidDataException($"Agent '{definition.Code}' does not have exactly one Draft version.");
        AgentVersion[] published = source.Versions
            .Where(value => value.Version.IsDraft != true)
            .OrderBy(value => value.Version.Ordinal)
            .Select(MapVersion)
            .ToArray();

        return new AgentDefinition(
            definition.ID,
            definition.Code,
            definition.Name,
            definition.Description,
            ParseEnum<AgentRuntimeStatus>(definition.RuntimeStatus, "RuntimeStatus"),
            definition.LogicalRevision ?? throw new InvalidDataException("Agent LogicalRevision is required."),
            MapVersion(draft),
            published);
    }

    private static AgentVersion MapVersion(AgAgentVersionDetailDto source)
    {
        AgAgentVersion version = source.Version;
        ValidateBindings(source);
        AgAgentVersionBinding[] versionBindings = source.Bindings
            .Where(value => value.Scope == VersionScope)
            .OrderBy(value => value.Ordinal)
            .ToArray();

        return new AgentVersion(
            version.ID,
            version.Label,
            version.IsDraft == true,
            version.Instructions,
            version.ModelProfileId,
            ParseEnum<AgentOutputMode>(version.OutputMode, "OutputMode"),
            version.OutputJsonSchema,
            version.OutputSchemaSha256,
            source.Snapshot is null ? null : MapSnapshot(source))
        {
            SkillVersionIds = References(versionBindings, "Skill"),
            ToolVersionIds = References(versionBindings, "Tool"),
            KnowledgeBaseIds = References(versionBindings, "KnowledgeBase"),
            ChildAgentIds = References(versionBindings, "ChildAgent"),
            OrchestrationIds = References(versionBindings, "Orchestration"),
            ChildAgentPins = versionBindings
                .Where(value => value.BindingType == "ChildAgent" && value.ReferenceVersionId.HasValue)
                .Select(value => new AgentChildBindingSnapshot(
                    Required(value.ReferenceId, "ChildAgent.ReferenceId"),
                    value.ReferenceVersionId!.Value)
                {
                    AgentCode = value.ReferenceCode ?? string.Empty,
                    AgentName = value.ReferenceName,
                    AgentDescription = value.ReferenceDescription
                })
                .ToArray(),
            OrchestrationPins = versionBindings
                .Where(value => value.BindingType == "Orchestration" && value.ReferenceVersionId.HasValue)
                .Select(value => new AgentOrchestrationBindingSnapshot(
                    Required(value.ReferenceId, "Orchestration.ReferenceId"),
                    value.ReferenceVersionId!.Value))
                .ToArray()
        };
    }

    private static AgentVersionSnapshot MapSnapshot(AgAgentVersionDetailDto source)
    {
        AgAgentVersionSnapshot snapshot = source.Snapshot;
        AgAgentVersionBinding[] bindings = source.Bindings
            .Where(value => value.Scope == SnapshotScope)
            .OrderBy(value => value.Ordinal)
            .ToArray();
        return new AgentVersionSnapshot(
            Required(snapshot.SnapshotVersionId, "SnapshotVersionId"),
            snapshot.AgentCode,
            snapshot.Instructions,
            snapshot.ModelProfileId,
            ParseEnum<AgentOutputMode>(snapshot.OutputMode, "Snapshot.OutputMode"),
            snapshot.OutputJsonSchema,
            bindings.Where(value => value.BindingType == "Skill")
                .Select(value => new AgentSkillBindingSnapshot(Required(value.ReferenceId, "Skill.ReferenceId")))
                .ToArray(),
            bindings.Where(value => value.BindingType == "Tool")
                .Select(value => new AgentToolBindingSnapshot(Required(value.ReferenceId, "Tool.ReferenceId")))
                .ToArray())
        {
            AgentName = snapshot.AgentName,
            AgentDescription = snapshot.AgentDescription,
            KnowledgeBases = bindings.Where(value => value.BindingType == "KnowledgeBase")
                .Select(value => new AgentKnowledgeBindingSnapshot(
                    Required(value.ReferenceId, "KnowledgeBase.ReferenceId"),
                    value.LogicalRevision ?? throw new InvalidDataException(
                        "A snapshot knowledge binding requires a revision.")))
                .ToArray(),
            ChildAgents = bindings.Where(value => value.BindingType == "ChildAgent")
                .Select(value => new AgentChildBindingSnapshot(
                    Required(value.ReferenceId, "ChildAgent.ReferenceId"),
                    Required(value.ReferenceVersionId, "ChildAgent.ReferenceVersionId"))
                {
                    AgentCode = value.ReferenceCode ?? string.Empty,
                    AgentName = value.ReferenceName,
                    AgentDescription = value.ReferenceDescription
                })
                .ToArray(),
            Orchestrations = bindings.Where(value => value.BindingType == "Orchestration")
                .Select(value => new AgentOrchestrationBindingSnapshot(
                    Required(value.ReferenceId, "Orchestration.ReferenceId"),
                    Required(value.ReferenceVersionId, "Orchestration.ReferenceVersionId")))
                .ToArray()
        };
    }

    private static Guid[] References(IEnumerable<AgAgentVersionBinding> bindings, string bindingType) =>
        bindings.Where(value => value.BindingType == bindingType)
            .Select(value => Required(value.ReferenceId, $"{bindingType}.ReferenceId"))
            .ToArray();

    private static void ValidateBindings(AgAgentVersionDetailDto source)
    {
        foreach (AgAgentVersionBinding binding in source.Bindings)
        {
            if (binding.Scope is not (VersionScope or SnapshotScope))
                throw new InvalidDataException($"Unknown Agent binding scope '{binding.Scope}'.");
            if (!BindingTypes.Contains(binding.BindingType, StringComparer.Ordinal))
                throw new InvalidDataException($"Unknown Agent binding type '{binding.BindingType}'.");
            if (binding.Scope == SnapshotScope && source.Snapshot is null)
                throw new InvalidDataException("A snapshot binding references a missing Agent snapshot.");
        }
    }

    private static Guid Required(Guid? value, string name) =>
        value ?? throw new InvalidDataException($"Agent {name} is required.");

    private static T ParseEnum<T>(string value, string name) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out T parsed)
            ? parsed
            : throw new InvalidDataException($"Agent {name} contains unsupported value '{value}'.");
}

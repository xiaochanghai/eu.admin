using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Knowledge;

namespace EU.Core.Agent.Application.Agents;

public sealed class AgentLifecycleService
{
    private readonly IAgentRepository _repository;
    private readonly JsonSchemaValidator _jsonSchemaValidator;
    private readonly IPublishedSkillVersionCatalog? _skillVersions;
    private readonly IPublishedMcpToolCatalog? _toolVersions;
    private readonly IPublishedKnowledgeCatalog? _knowledgeBases;

    public AgentLifecycleService(
        IAgentRepository repository,
        JsonSchemaValidator? jsonSchemaValidator = null,
        IPublishedSkillVersionCatalog? skillVersions = null,
        IPublishedMcpToolCatalog? toolVersions = null,
        IPublishedKnowledgeCatalog? knowledgeBases = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jsonSchemaValidator = jsonSchemaValidator ?? new JsonSchemaValidator();
        _skillVersions = skillVersions;
        _toolVersions = toolVersions;
        _knowledgeBases = knowledgeBases;
    }

    public async Task<AgentOperationResult<AgentDefinition>> CreateAsync(CreateAgentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!TryNormalizeCode(command.Code, out string? normalizedCode))
        {
            return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.CodeInvalid, "Agent code must normalize to lowercase kebab-case.");
        }

        Guid id = Guid.NewGuid();
        var draft = new AgentVersion(Guid.NewGuid(), "0.1.0", true, string.Empty, string.Empty, AgentOutputMode.Text, null, null, null);
        var definition = new AgentDefinition(
            id,
            normalizedCode!,
            command.Name ?? string.Empty,
            command.Description ?? string.Empty,
            AgentRuntimeStatus.Enabled,
            0,
            draft,
            AgentContractCloner.ReadOnly(Array.Empty<AgentVersion>()));
        if (!await _repository.TryCreateAsync(definition, cancellationToken))
        {
            return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.CodeConflict, "An Agent already uses this code.");
        }

        return AgentOperationResult<AgentDefinition>.Success(definition);
    }

    public async Task<AgentOperationResult<AgentDefinition>> CreateImportedAsync(
        ImportAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!TryNormalizeCode(command.Code, out string? normalizedCode) ||
            !string.Equals(command.Code, normalizedCode, StringComparison.Ordinal))
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.CodeInvalid,
                "Imported Agent code must already be lowercase kebab-case.");
        }

        if (!Enum.IsDefined(command.RuntimeStatus))
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.RuntimeStatusInvalid,
                "Runtime status must be Enabled or Disabled.");
        }

        if (command.OutputMode is AgentOutputMode.Text)
        {
            if (command.OutputJsonSchema is not null)
            {
                return AgentOperationResult<AgentDefinition>.Failure(
                    AgentErrorCodes.PackageInvalid,
                    "Text output cannot carry a JSON schema.");
            }
        }
        else if (command.OutputMode is AgentOutputMode.Structured)
        {
            JsonSchemaValidationResult validation = _jsonSchemaValidator.Validate(command.OutputJsonSchema);
            if (!validation.IsValid)
            {
                return AgentOperationResult<AgentDefinition>.Failure(
                    AgentErrorCodes.OutputSchemaInvalid,
                    validation.Error!);
            }
        }
        else
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.PackageInvalid,
                "Output mode is not supported.");
        }

        IReadOnlyList<Guid> importedSkillVersionIds =
            command.SkillVersionIds ?? Array.Empty<Guid>();
        AgentOperationResult<AgentDefinition>? importedSkillError =
            await ValidateSkillVersionsAsync(importedSkillVersionIds, cancellationToken);
        if (importedSkillError is not null)
        {
            return importedSkillError;
        }

        IReadOnlyList<Guid> importedToolVersionIds =
            command.ToolVersionIds ?? Array.Empty<Guid>();
        AgentOperationResult<AgentDefinition>? importedToolError =
            await ValidateToolVersionsAsync(importedToolVersionIds, cancellationToken);
        if (importedToolError is not null)
        {
            return importedToolError;
        }

        IReadOnlyList<Guid> importedKnowledgeBaseIds =
            command.KnowledgeBaseIds ?? Array.Empty<Guid>();
        AgentOperationResult<AgentDefinition>? importedKnowledgeError =
            await ValidateKnowledgeBasesAsync(importedKnowledgeBaseIds, cancellationToken);
        if (importedKnowledgeError is not null)
        {
            return importedKnowledgeError;
        }

        var draft = new AgentVersion(
            Guid.NewGuid(),
            "0.1.0",
            true,
            command.Instructions ?? string.Empty,
            command.ModelProfileId ?? string.Empty,
            command.OutputMode,
            command.OutputJsonSchema,
            null,
            null)
        {
            SkillVersionIds = AgentContractCloner.ReadOnly(
                importedSkillVersionIds),
            ToolVersionIds = AgentContractCloner.ReadOnly(
                importedToolVersionIds),
            KnowledgeBaseIds = AgentContractCloner.ReadOnly(importedKnowledgeBaseIds)
        };
        var definition = new AgentDefinition(
            Guid.NewGuid(),
            normalizedCode!,
            command.Name ?? string.Empty,
            command.Description ?? string.Empty,
            command.RuntimeStatus,
            0,
            draft,
            AgentContractCloner.ReadOnly(Array.Empty<AgentVersion>()));
        if (!await _repository.TryCreateAsync(definition, cancellationToken))
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.CodeConflict,
                "An Agent already uses this code.");
        }

        return AgentOperationResult<AgentDefinition>.Success(definition);
    }

    public async Task<AgentOperationResult<AgentDefinition>> SaveDraftAsync(SaveAgentDraftCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentDefinition? existing = await _repository.GetByIdAsync(command.AgentId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return RowVersionConflict();
        }

        IReadOnlyList<Guid> skillVersionIds = command.SkillVersionIds ??
                                              existing.Draft.SkillVersionIds;
        AgentOperationResult<AgentDefinition>? skillError =
            await ValidateSkillVersionsAsync(skillVersionIds, cancellationToken);
        if (skillError is not null)
        {
            return skillError;
        }

        IReadOnlyList<Guid> toolVersionIds = command.ToolVersionIds ??
                                             existing.Draft.ToolVersionIds;
        AgentOperationResult<AgentDefinition>? toolError =
            await ValidateToolVersionsAsync(toolVersionIds, cancellationToken);
        if (toolError is not null)
        {
            return toolError;
        }

        IReadOnlyList<Guid> knowledgeBaseIds = command.KnowledgeBaseIds ??
                                               existing.Draft.KnowledgeBaseIds;
        AgentOperationResult<AgentDefinition>? knowledgeError =
            await ValidateKnowledgeBasesAsync(knowledgeBaseIds, cancellationToken);
        if (knowledgeError is not null)
        {
            return knowledgeError;
        }

        var draft = existing.Draft with
        {
            Instructions = command.Instructions ?? string.Empty,
            ModelProfileId = command.ModelProfileId ?? string.Empty,
            OutputMode = command.OutputMode,
            OutputJsonSchema = command.OutputJsonSchema,
            OutputSchemaSha256 = null,
            Snapshot = null,
            SkillVersionIds = AgentContractCloner.ReadOnly(skillVersionIds),
            ToolVersionIds = AgentContractCloner.ReadOnly(toolVersionIds),
            KnowledgeBaseIds = AgentContractCloner.ReadOnly(knowledgeBaseIds)
        };
        AgentDefinition updated = existing with { Draft = draft, LogicalRevision = existing.LogicalRevision + 1 };
        updated = updated with
        {
            Name = command.Name ?? existing.Name,
            Description = command.Description ?? existing.Description
        };
        if (!await _repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken))
        {
            return RowVersionConflict();
        }

        return AgentOperationResult<AgentDefinition>.Success(updated);
    }

    public async Task<AgentOperationResult<AgentDefinition>> SetRuntimeStatusAsync(SetAgentRuntimeStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.RuntimeStatus))
        {
            return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.RuntimeStatusInvalid, "Runtime status must be Enabled or Disabled.");
        }

        AgentDefinition? existing = await _repository.GetByIdAsync(command.AgentId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return RowVersionConflict();
        }

        AgentDefinition updated = existing with { RuntimeStatus = command.RuntimeStatus, LogicalRevision = existing.LogicalRevision + 1 };
        if (!await _repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken))
        {
            return RowVersionConflict();
        }

        return AgentOperationResult<AgentDefinition>.Success(updated);
    }

    public async Task<AgentOperationResult<AgentDefinition>> PublishAsync(PublishAgentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentDefinition? existing = await _repository.GetByIdAsync(command.AgentId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return RowVersionConflict();
        }

        AgentVersion draft = existing.Draft;
        if (string.IsNullOrWhiteSpace(draft.Instructions) || string.IsNullOrWhiteSpace(draft.ModelProfileId))
        {
            return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.VersionNotPublishable, "Instructions and ModelProfileId are required before publish.");
        }

        string? canonicalSchema = null;
        string? schemaHash = null;
        if (draft.OutputMode is AgentOutputMode.Text)
        {
            if (draft.OutputJsonSchema is not null)
            {
                return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.OutputSchemaInvalid, "Text output cannot carry a JSON schema.");
            }
        }
        else if (draft.OutputMode is AgentOutputMode.Structured)
        {
            JsonSchemaValidationResult validation = _jsonSchemaValidator.Validate(draft.OutputJsonSchema);
            if (!validation.IsValid)
            {
                return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.OutputSchemaInvalid, validation.Error!);
            }

            canonicalSchema = validation.CanonicalJson;
            schemaHash = validation.Sha256;
        }
        else
        {
            return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.VersionNotPublishable, "Output mode is not supported.");
        }

        string label = $"{existing.PublishedVersions.Count + 1}.0.0";
        Guid versionId = Guid.NewGuid();
        var snapshot = new AgentVersionSnapshot(
            versionId,
            existing.Code,
            draft.Instructions,
            draft.ModelProfileId,
            draft.OutputMode,
            canonicalSchema,
            AgentContractCloner.ReadOnly(draft.SkillVersionIds.Select(
                versionId => new AgentSkillBindingSnapshot(versionId))),
            AgentContractCloner.ReadOnly(draft.ToolVersionIds.Select(
                versionId => new AgentToolBindingSnapshot(versionId))))
        {
            KnowledgeBases = AgentContractCloner.ReadOnly(
                (await GetKnowledgeReferencesAsync(draft.KnowledgeBaseIds, cancellationToken))
                .Select(value => new AgentKnowledgeBindingSnapshot(
                    value.KnowledgeBaseId, value.LogicalRevision)))
        };
        var published = new AgentVersion(versionId, label, false, draft.Instructions, draft.ModelProfileId, draft.OutputMode, canonicalSchema, schemaHash, snapshot);
        AgentDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            PublishedVersions = AgentContractCloner.ReadOnly(existing.PublishedVersions.Append(published))
        };
        if (!await _repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken))
        {
            return RowVersionConflict();
        }

        return AgentOperationResult<AgentDefinition>.Success(updated);
    }

    public async Task<IReadOnlyList<AgentListItem>> ListAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        IReadOnlyList<AgentDefinition> definitions = await _repository.ListAsync(query, cancellationToken);
        return AgentContractCloner.ReadOnly(definitions.Select(definition => new AgentListItem(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.Description,
            definition.RuntimeStatus,
            definition.LogicalRevision,
            definition.Draft.Label,
            definition.Draft.ModelProfileId,
            definition.PublishedVersions.LastOrDefault()?.Label)));
    }

    private static AgentOperationResult<AgentDefinition> NotFound() =>
        AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.NotFound, "The Agent was not found.");

    private static AgentOperationResult<AgentDefinition> RowVersionConflict() =>
        AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.RowVersionConflict, "The Agent changed before this operation completed.");

    private async Task<AgentOperationResult<AgentDefinition>?> ValidateSkillVersionsAsync(
        IReadOnlyList<Guid> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count != versionIds.Distinct().Count())
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.SkillVersionNotPublished,
                "Agent Skill bindings must not contain duplicate versions.");
        }

        foreach (Guid versionId in versionIds)
        {
            if (versionId == Guid.Empty ||
                _skillVersions is null ||
                !await _skillVersions.ExistsAsync(versionId, cancellationToken))
            {
                return AgentOperationResult<AgentDefinition>.Failure(
                    AgentErrorCodes.SkillVersionNotPublished,
                    "Agent Drafts may bind only published Skill versions.");
            }
        }

        return null;
    }

    private async Task<AgentOperationResult<AgentDefinition>?> ValidateToolVersionsAsync(
        IReadOnlyList<Guid> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count > 128 ||
            versionIds.Count != versionIds.Distinct().Count())
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.ToolVersionNotAvailable,
                "Agent MCP tool bindings must contain no more than 128 unique versions.");
        }

        foreach (Guid versionId in versionIds)
        {
            if (versionId == Guid.Empty ||
                _toolVersions is null ||
                !await _toolVersions.ExistsAsync(versionId, cancellationToken))
            {
                return AgentOperationResult<AgentDefinition>.Failure(
                    AgentErrorCodes.ToolVersionNotAvailable,
                    "Agent Drafts may bind only classified MCP tool versions.");
            }
        }

        return null;
    }

    private async Task<AgentOperationResult<AgentDefinition>?> ValidateKnowledgeBasesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count > 32 || ids.Count != ids.Distinct().Count())
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.KnowledgeBaseUnavailable,
                "Agent knowledge bindings must contain no more than 32 unique knowledge bases.");
        }

        IReadOnlySet<Guid> available = (await GetKnowledgeReferencesAsync(ids, cancellationToken))
            .Select(value => value.KnowledgeBaseId)
            .ToHashSet();
        if (ids.Any(id => id == Guid.Empty || !available.Contains(id)))
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.KnowledgeBaseUnavailable,
                "Agent Drafts may bind only enabled and indexed knowledge bases.");
        }

        return null;
    }

    private async Task<IReadOnlyList<PublishedKnowledgeReference>> GetKnowledgeReferencesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<PublishedKnowledgeReference>();
        }

        if (_knowledgeBases is null)
        {
            return Array.Empty<PublishedKnowledgeReference>();
        }

        IReadOnlySet<Guid> selected = ids.ToHashSet();
        return KnowledgeContractCloner.ReadOnly(
            (await _knowledgeBases.ListAsync(cancellationToken))
            .Where(value => selected.Contains(value.KnowledgeBaseId)));
    }

    private static bool TryNormalizeCode(string? value, out string? normalizedCode)
    {
        normalizedCode = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var builder = new System.Text.StringBuilder();
        bool pendingHyphen = false;
        foreach (char character in value.Trim())
        {
            if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingHyphen = false;
            }
            else if (character is '-' or '_' || char.IsWhiteSpace(character))
            {
                pendingHyphen = builder.Length > 0;
            }
            else
            {
                return false;
            }
        }

        normalizedCode = builder.ToString();
        return normalizedCode.Length > 0;
    }
}

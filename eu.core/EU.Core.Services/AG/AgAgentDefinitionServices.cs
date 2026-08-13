using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Validation;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.MainAgent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

#nullable enable

/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentDefinition.cs
*
* 功 能： N / A
* 类 名： AgAgentDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/8/12 0:58:24  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// Agent 定义表 (服务)
/// </summary>
public class AgAgentDefinitionServices : BaseServices<AgAgentDefinition, AgAgentDefinitionDto, InsertAgAgentDefinitionInput, EditAgAgentDefinitionInput>, IAgAgentDefinitionServices
{
    public AgAgentDefinitionServices(IBaseRepository<AgAgentDefinition> dal)
    {
        BaseDal = dal;
        _repository = null!;
        _jsonSchemaValidator = null!;
        _modelProfiles = null!;
    }

    /// <summary>
    /// 查询 Agent 管理列表，并批量加载草稿及最新发布版本摘要。
    /// </summary>
    public async Task<List<AgAgentDefinitionDto>> QueryAgentList(string? search = null, string? runtimeStatus = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? normalizedSearch = search?.Trim().ToLowerInvariant();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var definitions = await Db.Queryable<AgAgentDefinition>()
                .Where(definition => !definition.IsDeleted)
                .WhereIF(
                    runtimeStatus.IsNullOrEmpty(),
                    definition => definition.RuntimeStatus != "Archived")
                .WhereIF(
                    runtimeStatus.IsNotEmptyOrNull(),
                    definition => definition.RuntimeStatus == runtimeStatus)
                .WhereIF(
                    normalizedSearch.IsNotEmptyOrNull(),
                    definition =>
                        SqlFunc.ToLower(definition.Code).Contains(normalizedSearch!) ||
                        SqlFunc.ToLower(definition.Name).Contains(normalizedSearch!) ||
                        SqlFunc.ToLower(definition.Description).Contains(normalizedSearch!))
                .OrderBy(definition => definition.Code)
                .OrderBy(definition => definition.ID)
                .ToListAsync();

            if (definitions.Count == 0)
            {
                await Db.Ado.CommitTranAsync();
                return [];
            }

            cancellationToken.ThrowIfCancellationRequested();
            Guid[] agentIds = definitions.Select(definition => definition.ID).ToArray();
            var versions = await Db.Queryable<AgAgentVersion>()
                .Where(version =>
                    !version.IsDeleted &&
                    version.AgentId.HasValue &&
                    agentIds.Contains(version.AgentId.Value))
                .OrderBy(version => version.AgentId)
                .OrderBy(version => version.IsDraft, OrderByType.Desc)
                .OrderBy(version => version.Ordinal)
                .ToListAsync();

            var versionsByAgent = versions
                .GroupBy(version => version.AgentId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.ToArray());

            List<AgAgentDefinitionDto> result = definitions.Select(definition =>
            {
                if (!versionsByAgent.TryGetValue(definition.ID, out AgAgentVersion[]? agentVersions) ||
                    agentVersions is null)
                    throw new InvalidDataException($"Agent '{definition.Code}' does not have any versions.");

                AgAgentVersion draft = agentVersions.SingleOrDefault(version => version.IsDraft == true)
                    ?? throw new InvalidDataException($"Agent '{definition.Code}' does not have exactly one Draft version.");
                AgAgentVersion? currentPublished = agentVersions
                    .Where(version => version.IsDraft != true)
                    .OrderBy(version => version.Ordinal)
                    .LastOrDefault();

                return new AgAgentDefinitionDto
                {
                    ID = definition.ID,
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description,
                    RuntimeStatus = definition.RuntimeStatus,
                    LogicalRevision = definition.LogicalRevision,
                    DraftLabel = draft.Label,
                    DraftModelProfileId = draft.ModelProfileId,
                    CurrentPublishedLabel = currentPublished?.Label
                };
            }).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// 查询 Agent 明细及其版本、快照和资源绑定。
    /// </summary>
    public async Task<AgAgentDefinitionDetailDto?> QueryAgent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgAgentDefinition? definition = await Db.Queryable<AgAgentDefinition>()
                .Where(value => value.ID == id && !value.IsDeleted)
                .FirstAsync();
            if (definition is null)
            {
                await Db.Ado.CommitTranAsync();
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<AgAgentVersion> versions = await Db.Queryable<AgAgentVersion>()
                .Where(value => value.AgentId == id && !value.IsDeleted)
                .OrderBy(value => value.IsDraft, OrderByType.Desc)
                .OrderBy(value => value.Ordinal)
                .ToListAsync();
            Guid[] versionIds = versions.Select(value => value.ID).ToArray();

            List<AgAgentVersionSnapshot> snapshots = versionIds.Length == 0
                ? []
                : await Db.Queryable<AgAgentVersionSnapshot>()
                    .Where(value =>
                        value.VersionId.HasValue &&
                        versionIds.Contains(value.VersionId.Value) &&
                        !value.IsDeleted)
                    .ToListAsync();
            List<AgAgentVersionBinding> bindings = versionIds.Length == 0
                ? []
                : await Db.Queryable<AgAgentVersionBinding>()
                    .Where(value =>
                        value.VersionId.HasValue &&
                        versionIds.Contains(value.VersionId.Value) &&
                        !value.IsDeleted)
                    .OrderBy(value => value.VersionId)
                    .OrderBy(value => value.Scope)
                    .OrderBy(value => value.BindingType)
                    .OrderBy(value => value.Ordinal)
                    .ToListAsync();

            var snapshotsByVersion = snapshots.ToDictionary(value => value.VersionId.GetValueOrDefault());
            var bindingsByVersion = bindings
                .GroupBy(value => value.VersionId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.ToList());
            var result = new AgAgentDefinitionDetailDto
            {
                Definition = definition,
                Versions = versions.Select(version => new AgAgentVersionDetailDto
                {
                    Version = version,
                    Snapshot = snapshotsByVersion.GetValueOrDefault(version.ID),
                    Bindings = bindingsByVersion.GetValueOrDefault(version.ID) ?? []
                }).ToList()
            };
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private readonly IAgentRepository _repository;
    private readonly JsonSchemaValidator _jsonSchemaValidator;
    private readonly IPublishedSkillVersionCatalog? _skillVersions;
    private readonly IPublishedMcpToolCatalog? _toolVersions;
    private readonly IPublishedKnowledgeCatalog? _knowledgeBases;
    private readonly IPublishedOrchestrationCatalog? _orchestrationCatalog;
    private readonly IOrchestrationRepository? _orchestrations;
    private readonly IMainAgentAssignmentRepository? _mainAgentAssignments;
    private readonly IModelProfileReferenceCatalog _modelProfiles;

    public AgAgentDefinitionServices(
        IBaseRepository<AgAgentDefinition> dal,
        IAgentRepository repository,
        JsonSchemaValidator? jsonSchemaValidator = null,
        IPublishedSkillVersionCatalog? skillVersions = null,
        IPublishedMcpToolCatalog? toolVersions = null,
        IPublishedKnowledgeCatalog? knowledgeBases = null,
        IPublishedOrchestrationCatalog? orchestrationCatalog = null,
        IOrchestrationRepository? orchestrations = null,
        IMainAgentAssignmentRepository? mainAgentAssignments = null,
        IModelProfileReferenceCatalog? modelProfiles = null)
    {
        BaseDal = dal ?? throw new ArgumentNullException(nameof(dal));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jsonSchemaValidator = jsonSchemaValidator ?? new JsonSchemaValidator();
        _skillVersions = skillVersions;
        _toolVersions = toolVersions;
        _knowledgeBases = knowledgeBases;
        _orchestrationCatalog = orchestrationCatalog;
        _orchestrations = orchestrations;
        _mainAgentAssignments = mainAgentAssignments;
        _modelProfiles = modelProfiles ?? throw new ArgumentNullException(nameof(modelProfiles));
    }

    public async Task<AgentOperationResult<AgentDefinition>> CreateAsync(CreateAgentCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
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
        EnsureAgentManagementAvailable();
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
                "Runtime status must be Enabled, Disabled, or Archived.");
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
            KnowledgeBaseIds = AgentContractCloner.ReadOnly(importedKnowledgeBaseIds),
            ChildAgentIds = AgentContractCloner.ReadOnly(command.ChildAgentIds ?? Array.Empty<Guid>()),
            OrchestrationIds = AgentContractCloner.ReadOnly(command.OrchestrationIds ?? Array.Empty<Guid>()),
            ChildAgentPins = AgentContractCloner.ReadOnly((command.ChildAgentPins ?? Array.Empty<AgentChildBindingSnapshot>()).Select(value => value with { })),
            OrchestrationPins = AgentContractCloner.ReadOnly((command.OrchestrationPins ?? Array.Empty<AgentOrchestrationBindingSnapshot>()).Select(value => value with { }))
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
        EnsureAgentManagementAvailable();
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

        if (existing.RuntimeStatus is AgentRuntimeStatus.Archived)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.LifecycleTransitionInvalid,
                "An archived Agent must be restored before its Draft can be edited.");
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
            KnowledgeBaseIds = AgentContractCloner.ReadOnly(knowledgeBaseIds),
            ChildAgentIds = AgentContractCloner.ReadOnly(command.ChildAgentIds ?? existing.Draft.ChildAgentIds),
            OrchestrationIds = AgentContractCloner.ReadOnly(command.OrchestrationIds ?? existing.Draft.OrchestrationIds),
            ChildAgentPins = AgentContractCloner.ReadOnly((command.ChildAgentPins ??
                (command.ChildAgentIds is null ? existing.Draft.ChildAgentPins : Array.Empty<AgentChildBindingSnapshot>())).Select(value => value with { })),
            OrchestrationPins = AgentContractCloner.ReadOnly((command.OrchestrationPins ??
                (command.OrchestrationIds is null ? existing.Draft.OrchestrationPins : Array.Empty<AgentOrchestrationBindingSnapshot>())).Select(value => value with { }))
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
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.RuntimeStatus))
        {
            return AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.RuntimeStatusInvalid, "Runtime status must be Enabled, Disabled, or Archived.");
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

        if (command.RuntimeStatus is AgentRuntimeStatus.Archived &&
            existing.RuntimeStatus is not AgentRuntimeStatus.Disabled)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.LifecycleTransitionInvalid,
                "An Agent must be disabled before it can be archived.");
        }

        if (command.RuntimeStatus is AgentRuntimeStatus.Archived)
        {
            IReadOnlyList<string> blockers = await FindArchiveBlockersAsync(
                existing.Id,
                cancellationToken);
            if (blockers.Count > 0)
            {
                return AgentOperationResult<AgentDefinition>.Failure(
                    AgentErrorCodes.ArchiveBlocked,
                    $"The Agent is still referenced by {string.Join(", ", blockers)}.");
            }
        }

        if (existing.RuntimeStatus is AgentRuntimeStatus.Archived &&
            command.RuntimeStatus is not AgentRuntimeStatus.Disabled)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.LifecycleTransitionInvalid,
                "An archived Agent must be restored to Disabled before it can be enabled.");
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
        EnsureAgentManagementAvailable();
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

        if (existing.RuntimeStatus is AgentRuntimeStatus.Archived)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.LifecycleTransitionInvalid,
                "An archived Agent must be restored before a version can be published.");
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

        AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>> childBindings =
            await ResolveChildAgentBindingsAsync(existing.Id, draft, cancellationToken);
        if (!childBindings.Succeeded)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                childBindings.Error!.Code, childBindings.Error.Message);
        }

        AgentOperationResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>> orchestrationBindings =
            await ResolveOrchestrationBindingsAsync(draft, cancellationToken);
        if (!orchestrationBindings.Succeeded)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                orchestrationBindings.Error!.Code, orchestrationBindings.Error.Message);
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
            AgentName = existing.Name.Trim(),
            AgentDescription = existing.Description.Trim(),
            KnowledgeBases = AgentContractCloner.ReadOnly(
                (await GetKnowledgeReferencesAsync(draft.KnowledgeBaseIds, cancellationToken))
                .Select(value => new AgentKnowledgeBindingSnapshot(
                    value.KnowledgeBaseId, value.LogicalRevision))),
            ChildAgents = childBindings.Value!,
            Orchestrations = orchestrationBindings.Value!
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
        EnsureAgentManagementAvailable();
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

    private void EnsureAgentManagementAvailable()
    {
        if (_repository is null || _jsonSchemaValidator is null || _modelProfiles is null)
        {
            throw new InvalidOperationException(
                "Agent management dependencies are not registered in this Host.");
        }
    }

    private static AgentOperationResult<AgentDefinition> NotFound() =>
        AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.NotFound, "The Agent was not found.");

    private static AgentOperationResult<AgentDefinition> RowVersionConflict() =>
        AgentOperationResult<AgentDefinition>.Failure(AgentErrorCodes.RowVersionConflict, "The Agent changed before this operation completed.");

    private async Task<IReadOnlyList<string>> FindArchiveBlockersAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var blockers = new List<string>();
        IReadOnlyList<AgentDefinition> enabledAgents = await _repository.ListAsync(
            new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Enabled),
            cancellationToken);
        blockers.AddRange(enabledAgents
            .Where(value => value.Id != agentId &&
                value.PublishedVersions.LastOrDefault()?.Snapshot?.ChildAgents
                    .Any(binding => binding.AgentId == agentId) == true)
            .Select(value => $"Agent '{value.Code}'"));

        if (_orchestrations is not null)
        {
            IReadOnlyList<OrchestrationDefinition> definitions =
                await _orchestrations.ListAsync(cancellationToken);
            blockers.AddRange(definitions
                .Where(value => value.Status is OrchestrationStatus.Enabled &&
                    value.PublishedVersions.LastOrDefault()?.Snapshot?.Agents
                        .Any(binding => binding.AgentId == agentId) == true)
                .Select(value => $"orchestration '{value.Code}'"));
        }

        if (_mainAgentAssignments is not null &&
            (await _mainAgentAssignments.GetAsync(cancellationToken))?.AgentId == agentId)
        {
            blockers.Add("the Main Agent assignment");
        }

        return AgentContractCloner.ReadOnly(blockers.Take(8));
    }

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

        if (versionIds.Count == 0)
        {
            return null;
        }

        IReadOnlySet<Guid> available = _skillVersions is null
            ? new HashSet<Guid>()
            : (await _skillVersions.ListAsync(cancellationToken))
                .Select(value => value.VersionId)
                .ToHashSet();
        if (versionIds.Any(versionId =>
                versionId == Guid.Empty || !available.Contains(versionId)))
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.SkillVersionNotPublished,
                "Agent Drafts may bind only published Skill versions.");
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

        if (versionIds.Count == 0)
        {
            return null;
        }

        IReadOnlySet<Guid> available = _toolVersions is null
            ? new HashSet<Guid>()
            : (await _toolVersions.ListAsync(cancellationToken))
                .Select(value => value.ToolVersionId)
                .ToHashSet();
        if (versionIds.Any(versionId =>
                versionId == Guid.Empty || !available.Contains(versionId)))
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.ToolVersionNotAvailable,
                "Agent Drafts may bind only classified MCP tool versions.");
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

    private async Task<AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>> ResolveChildAgentBindingsAsync(
        Guid agentId,
        AgentVersion draft,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> childAgentIds = draft.ChildAgentIds;
        if (childAgentIds.Count > AgentDelegationPolicy.MaximumChildAgentBindings)
        {
            return AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>.Failure(
                AgentErrorCodes.ReferenceMissing,
                $"Main Agent publications may bind no more than {AgentDelegationPolicy.MaximumChildAgentBindings} child Agents.");
        }

        if (childAgentIds.Count != childAgentIds.Distinct().Count() ||
            childAgentIds.Any(id => id == Guid.Empty || id == agentId))
        {
            return AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>.Failure(
                AgentErrorCodes.ReferenceMissing,
                "Child Agent bindings must contain unique published Agent identities other than the Agent itself.");
        }

        if (draft.ChildAgentPins.Count > 0 &&
            (draft.ChildAgentPins.Select(value => value.AgentId).Distinct().Count() != draft.ChildAgentPins.Count ||
             draft.ChildAgentPins.Count != childAgentIds.Count ||
             draft.ChildAgentPins.Select(value => value.AgentId).Except(childAgentIds).Any() ||
             draft.ChildAgentPins.Any(value => value.AgentVersionId == Guid.Empty)))
        {
            return AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>.Failure(
                AgentErrorCodes.ReferenceMissing,
                "Imported child Agent pins must match unique child Agent identities.");
        }
        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot> pins = draft.ChildAgentPins
            .ToDictionary(value => value.AgentId);

        var resolved = new List<AgentChildBindingSnapshot>(childAgentIds.Count);
        foreach (Guid childAgentId in childAgentIds)
        {
            AgentDefinition? child = await _repository.GetByIdAsync(childAgentId, cancellationToken);
            if (child is null ||
                child.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                child.PublishedVersions.Count == 0)
            {
                return AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>.Failure(
                    AgentErrorCodes.ReferenceMissing,
                    "Child Agent bindings must reference enabled published Agents.");
            }

            Guid versionId = pins.TryGetValue(childAgentId, out AgentChildBindingSnapshot? pin)
                ? pin.AgentVersionId
                : child.PublishedVersions[^1].Id;
            AgentVersion? selectedVersion = child.PublishedVersions
                .FirstOrDefault(version => version.Id == versionId);
            if (selectedVersion is null)
            {
                return AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>.Failure(
                    AgentErrorCodes.ReferenceMissing,
                    "The imported child Agent version is no longer available.");
            }
            resolved.Add(new AgentChildBindingSnapshot(childAgentId, versionId)
            {
                AgentCode = selectedVersion.Snapshot?.AgentCode ?? child.Code,
                AgentName = selectedVersion.Snapshot?.AgentName is { } frozenName
                    ? frozenName
                    : child.Name.Trim(),
                AgentDescription = selectedVersion.Snapshot?.AgentDescription
                    is { } frozenDescription
                    ? frozenDescription
                    : child.Description.Trim()
            });
        }

        return AgentOperationResult<IReadOnlyList<AgentChildBindingSnapshot>>.Success(
            AgentContractCloner.ReadOnly(resolved));
    }

    private async Task<AgentOperationResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>>> ResolveOrchestrationBindingsAsync(
        AgentVersion draft,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> orchestrationIds = draft.OrchestrationIds;
        if (orchestrationIds.Count != orchestrationIds.Distinct().Count() ||
            orchestrationIds.Any(id => id == Guid.Empty) ||
            (orchestrationIds.Count > 0 && _orchestrationCatalog is null))
        {
            return AgentOperationResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>>.Failure(
                AgentErrorCodes.ReferenceMissing,
                "Orchestration bindings must contain unique enabled published orchestrations.");
        }

        IReadOnlyList<PublishedOrchestrationReference> values = _orchestrationCatalog is null
            ? []
            : await _orchestrationCatalog.ListPublishedAsync(cancellationToken);
        if (draft.OrchestrationPins.Count > 0 &&
            (draft.OrchestrationPins.Select(value => value.OrchestrationId).Distinct().Count() != draft.OrchestrationPins.Count ||
             draft.OrchestrationPins.Count != orchestrationIds.Count ||
             draft.OrchestrationPins.Select(value => value.OrchestrationId).Except(orchestrationIds).Any() ||
             draft.OrchestrationPins.Any(value => value.OrchestrationVersionId == Guid.Empty)))
        {
            return AgentOperationResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>>.Failure(
                AgentErrorCodes.ReferenceMissing,
                "Imported orchestration pins must match unique orchestration identities.");
        }
        IReadOnlyDictionary<Guid, AgentOrchestrationBindingSnapshot> pins = draft.OrchestrationPins
            .ToDictionary(value => value.OrchestrationId);

        var resolved = new List<AgentOrchestrationBindingSnapshot>(orchestrationIds.Count);
        foreach (Guid orchestrationId in orchestrationIds)
        {
            PublishedOrchestrationReference? selected = pins.TryGetValue(orchestrationId, out AgentOrchestrationBindingSnapshot? pin)
                ? values.SingleOrDefault(value => value.OrchestrationId == orchestrationId && value.OrchestrationVersionId == pin.OrchestrationVersionId)
                : values.LastOrDefault(value => value.OrchestrationId == orchestrationId);
            if (selected is null || !selected.Enabled)
            {
                return AgentOperationResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>>.Failure(
                    AgentErrorCodes.ReferenceMissing,
                    "Orchestration bindings must reference enabled published orchestrations.");
            }
            resolved.Add(new AgentOrchestrationBindingSnapshot(orchestrationId, selected.OrchestrationVersionId));
        }

        return AgentOperationResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>>.Success(
            AgentContractCloner.ReadOnly(resolved));
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

    public const string AgentPackageFormatIdentifier = "eu.core.agent-package";
    public const string AgentPackageCurrentVersion = "1.0.0";

    private const int MaximumPackageUtf8Bytes = 131_072;
    private const int MaximumPackageDepth = 24;
    private const int MaximumPackageNodes = 2_048;

    private static readonly JsonSerializerOptions AgentPackageSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    private static readonly HashSet<string> ForbiddenPackagePropertyNames = new(StringComparer.Ordinal)
    {
        "apikey",
        "connectionstring",
        "credential",
        "credentialalias",
        "endpoint",
        "password",
        "secret",
        "token",
        "accesstoken"
    };

    public async Task<AgentOperationResult<string>> ExportAsync(
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        AgentDefinition? definition = await _repository.GetByIdAsync(agentId, cancellationToken);
        if (definition is null)
        {
            return AgentOperationResult<string>.Failure(
                AgentErrorCodes.NotFound,
                "The Agent was not found.");
        }

        AgentError? bindingError = await ValidateDraftChildReferencesAsync(
            definition.Draft.ChildAgentIds, definition.Draft.ChildAgentPins, cancellationToken);
        if (bindingError is not null)
        {
            return new AgentOperationResult<string>(null, bindingError);
        }

        bindingError = await ValidateDraftOrchestrationReferencesAsync(
            definition.Draft.OrchestrationIds, definition.Draft.OrchestrationPins, cancellationToken);
        if (bindingError is not null)
        {
            return new AgentOperationResult<string>(null, bindingError);
        }

        var package = new AgentPackageV1(
            AgentPackageFormatIdentifier,
            AgentPackageCurrentVersion,
            new AgentPackageAgentV1(
                definition.Code,
                definition.Name,
                definition.Description,
                definition.RuntimeStatus.ToString(),
                new AgentPackageDraftV1(
                    definition.Draft.Instructions,
                    definition.Draft.ModelProfileId,
                    definition.Draft.OutputMode.ToString(),
                    definition.Draft.OutputJsonSchema),
                new AgentPackageDeploymentV1(
                    AgentDefinition.ServerDeploymentTarget,
                    AgentDefinition.ApiHost),
                AgentContractCloner.ReadOnly(definition.Draft.SkillVersionIds.Select(
                    id => id.ToString("D"))),
                AgentContractCloner.ReadOnly(definition.Draft.ToolVersionIds.Select(
                    id => id.ToString("D"))),
                definition.Draft.KnowledgeBaseIds.Count == 0
                    ? null
                    : AgentContractCloner.ReadOnly(definition.Draft.KnowledgeBaseIds.Select(
                        id => id.ToString("D"))))
            {
                ChildAgents = await ExportChildBindingsAsync(
                    definition.Draft.ChildAgentIds,
                    definition.Draft.ChildAgentPins,
                    cancellationToken),
                Orchestrations = await ExportOrchestrationBindingsAsync(
                    definition.Draft.OrchestrationIds,
                    definition.Draft.OrchestrationPins,
                    cancellationToken)
            });

        string json = JsonSerializer.Serialize(package, AgentPackageSerializerOptions);
        if (!TryReadPackage(json, out AgentPackageV1? verifiedPackage, out AgentError? safetyError))
        {
            return new AgentOperationResult<string>(null, safetyError);
        }

        if (!TryValidatePackage(verifiedPackage!, out _, out _, out AgentError? contractError))
        {
            return new AgentOperationResult<string>(null, contractError);
        }

        AgentError? referenceError = await ValidatePackageReferencesAsync(
            verifiedPackage!, cancellationToken);
        return referenceError is null
            ? AgentOperationResult<string>.Success(json)
            : new AgentOperationResult<string>(null, referenceError);
    }

    public async Task<AgentOperationResult<AgentDefinition>> ImportAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        if (!TryReadPackage(json, out AgentPackageV1? package, out AgentError? error))
        {
            return new AgentOperationResult<AgentDefinition>(null, error);
        }

        if (!TryValidatePackage(
                package!,
                out AgentRuntimeStatus runtimeStatus,
                out AgentOutputMode outputMode,
                out error))
        {
            return new AgentOperationResult<AgentDefinition>(null, error);
        }

        AgentError? referenceError = await ValidatePackageReferencesAsync(
            package!, cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<AgentDefinition>(null, referenceError);
        }

        AgentOperationResult<AgentDefinition> result = await CreateImportedAsync(
            new ImportAgentCommand(
                package!.Agent.Code,
                package.Agent.Name,
                package.Agent.Description,
                runtimeStatus,
                package.Agent.Draft.Instructions,
                package.Agent.Draft.ModelProfileId,
                outputMode,
                package.Agent.Draft.OutputJsonSchema,
                AgentContractCloner.ReadOnly(package.Agent.Skills.Select(Guid.Parse)),
                AgentContractCloner.ReadOnly(package.Agent.Tools.Select(Guid.Parse)),
                AgentContractCloner.ReadOnly(
                    (package.Agent.KnowledgeBases ?? []).Select(Guid.Parse)))
            {
                ChildAgentIds = AgentContractCloner.ReadOnly(
                    (package.Agent.ChildAgents ?? []).Select(value => Guid.Parse(value.AgentId))),
                OrchestrationIds = AgentContractCloner.ReadOnly(
                    (package.Agent.Orchestrations ?? []).Select(value => Guid.Parse(value.OrchestrationId))),
                ChildAgentPins = AgentContractCloner.ReadOnly(
                    (package.Agent.ChildAgents ?? []).Select(value =>
                        new AgentChildBindingSnapshot(
                            Guid.Parse(value.AgentId),
                            Guid.Parse(value.AgentVersionId)))),
                OrchestrationPins = AgentContractCloner.ReadOnly(
                    (package.Agent.Orchestrations ?? []).Select(value =>
                        new AgentOrchestrationBindingSnapshot(
                            Guid.Parse(value.OrchestrationId),
                            Guid.Parse(value.OrchestrationVersionId))))
            },
            cancellationToken);

        if (result.Error?.Code is AgentErrorCodes.CodeInvalid or AgentErrorCodes.RuntimeStatusInvalid)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.PackageInvalid,
                result.Error.Message);
        }

        return result;
    }

    private async Task<AgentError?> ValidatePackageReferencesAsync(
        AgentPackageV1 package,
        CancellationToken cancellationToken)
    {
        AgentError? error = await ValidateModelReferenceAsync(
            package.Agent.Draft.ModelProfileId, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        error = await ValidateToolReferencesAsync(package.Agent.Tools, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        error = await ValidateSkillReferencesAsync(package.Agent.Skills, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        error = await ValidateKnowledgeReferencesAsync(
            package.Agent.KnowledgeBases ?? [], cancellationToken);
        if (error is not null)
        {
            return error;
        }

        error = await ValidateChildBindingReferencesAsync(
            package.Agent.ChildAgents ?? [], cancellationToken);
        return error ?? await ValidateOrchestrationBindingReferencesAsync(
            package.Agent.Orchestrations ?? [], cancellationToken);
    }

    private async Task<AgentError?> ValidateModelReferenceAsync(
        string modelProfileId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(modelProfileId) &&
            !await _modelProfiles.ExistsAsync(modelProfileId, cancellationToken))
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package references a model profile that is not available.");
        }

        return null;
    }

    private async Task<AgentError?> ValidateToolReferencesAsync(
        IReadOnlyList<string> references,
        CancellationToken cancellationToken)
    {
        if (references.Count == 0)
        {
            return null;
        }

        IReadOnlySet<Guid> available = _toolVersions is null
            ? new HashSet<Guid>()
            : (await _toolVersions.ListAsync(cancellationToken))
                .Select(value => value.ToolVersionId)
                .ToHashSet();
        if (references.Any(reference =>
                !Guid.TryParseExact(reference, "D", out Guid versionId) ||
                !available.Contains(versionId)))
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package references an MCP tool version that is not available.");
        }

        return null;
    }

    private async Task<AgentError?> ValidateSkillReferencesAsync(
        IReadOnlyList<string> references,
        CancellationToken cancellationToken)
    {
        if (references.Count == 0)
        {
            return null;
        }

        IReadOnlySet<Guid> available = _skillVersions is null
            ? new HashSet<Guid>()
            : (await _skillVersions.ListAsync(cancellationToken))
                .Select(value => value.VersionId)
                .ToHashSet();
        if (references.Any(reference =>
                !Guid.TryParseExact(reference, "D", out Guid versionId) ||
                !available.Contains(versionId)))
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package references a Skill version that is not published.");
        }

        return null;
    }

    private async Task<AgentError?> ValidateKnowledgeReferencesAsync(
        IReadOnlyList<string> references,
        CancellationToken cancellationToken)
    {
        IReadOnlySet<Guid> available = _knowledgeBases is null
            ? new HashSet<Guid>()
            : (await _knowledgeBases.ListAsync(cancellationToken))
                .Select(value => value.KnowledgeBaseId)
                .ToHashSet();
        foreach (string reference in references)
        {
            if (!Guid.TryParseExact(reference, "D", out Guid id) || !available.Contains(id))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references a knowledge base that is not enabled and indexed.");
            }
        }

        return null;
    }

    private async Task<AgentError?> ValidateDraftChildReferencesAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentChildBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        if (pins.Count > 0 && pins.Select(value => value.AgentId).Distinct().Count() != pins.Count)
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package child Agent pins contain duplicate identities.");
        }

        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot>? byId = pins.Count == 0
            ? null
            : pins.ToDictionary(value => value.AgentId);
        if (byId is not null && (byId.Count != ids.Count || byId.Keys.Except(ids).Any()))
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package child Agent pins do not match its identities.");
        }

        foreach (Guid id in ids)
        {
            AgentDefinition? agent = await _repository.GetByIdAsync(id, cancellationToken);
            Guid versionId = byId?.TryGetValue(id, out AgentChildBindingSnapshot? pin) is true
                ? pin.AgentVersionId
                : agent?.PublishedVersions.LastOrDefault()?.Id ?? Guid.Empty;
            if (agent is null ||
                agent.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                !agent.PublishedVersions.Any(value => value.Id == versionId))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references an enabled published child Agent that is not available.");
            }
        }

        return null;
    }

    private async Task<AgentError?> ValidateDraftOrchestrationReferencesAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentOrchestrationBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedOrchestrationReference> values = _orchestrationCatalog is null
            ? []
            : await _orchestrationCatalog.ListPublishedAsync(cancellationToken);
        if (pins.Count > 0 &&
            pins.Select(value => value.OrchestrationId).Distinct().Count() != pins.Count)
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package orchestration pins contain duplicate identities.");
        }

        IReadOnlyDictionary<Guid, AgentOrchestrationBindingSnapshot>? byId = pins.Count == 0
            ? null
            : pins.ToDictionary(value => value.OrchestrationId);
        if (byId is not null && (byId.Count != ids.Count || byId.Keys.Except(ids).Any()))
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package orchestration pins do not match its identities.");
        }

        return ids.Any(id =>
            (byId?.TryGetValue(id, out AgentOrchestrationBindingSnapshot? pin) is true
                ? values.SingleOrDefault(value =>
                    value.OrchestrationId == id &&
                    value.OrchestrationVersionId == pin.OrchestrationVersionId)
                : values.LastOrDefault(value => value.OrchestrationId == id)) is not { Enabled: true })
            ? new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package references an enabled published orchestration that is not available.")
            : null;
    }

    private async Task<IReadOnlyList<AgentPackageChildBindingV1>?> ExportChildBindingsAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentChildBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        if (pins.Count > 0)
        {
            return AgentContractCloner.ReadOnly(pins.Select(value =>
                new AgentPackageChildBindingV1(
                    value.AgentId.ToString("D"),
                    value.AgentVersionId.ToString("D"))));
        }

        if (ids.Count == 0)
        {
            return null;
        }

        return AgentContractCloner.ReadOnly(await Task.WhenAll(ids.Select(async id =>
        {
            AgentDefinition agent = (await _repository.GetByIdAsync(id, cancellationToken))!;
            return new AgentPackageChildBindingV1(
                id.ToString("D"),
                agent.PublishedVersions[^1].Id.ToString("D"));
        })));
    }

    private async Task<IReadOnlyList<AgentPackageOrchestrationBindingV1>?> ExportOrchestrationBindingsAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentOrchestrationBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        if (pins.Count > 0)
        {
            return AgentContractCloner.ReadOnly(pins.Select(value =>
                new AgentPackageOrchestrationBindingV1(
                    value.OrchestrationId.ToString("D"),
                    value.OrchestrationVersionId.ToString("D"))));
        }

        if (ids.Count == 0)
        {
            return null;
        }

        IReadOnlyDictionary<Guid, PublishedOrchestrationReference> values =
            (await _orchestrationCatalog!.ListPublishedAsync(cancellationToken))
            .GroupBy(value => value.OrchestrationId)
            .ToDictionary(group => group.Key, group => group.Last());
        return AgentContractCloner.ReadOnly(ids.Select(id =>
            new AgentPackageOrchestrationBindingV1(
                id.ToString("D"),
                values[id].OrchestrationVersionId.ToString("D"))));
    }

    private async Task<AgentError?> ValidateChildBindingReferencesAsync(
        IReadOnlyList<AgentPackageChildBindingV1> references,
        CancellationToken cancellationToken)
    {
        foreach (AgentPackageChildBindingV1 reference in references)
        {
            if (!Guid.TryParseExact(reference.AgentId, "D", out Guid id) ||
                !Guid.TryParseExact(reference.AgentVersionId, "D", out Guid versionId))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references an invalid child Agent version.");
            }

            AgentDefinition? agent = await _repository.GetByIdAsync(id, cancellationToken);
            if (agent is null ||
                agent.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                !agent.PublishedVersions.Any(value => value.Id == versionId))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references a child Agent version that is not available.");
            }
        }

        return null;
    }

    private async Task<AgentError?> ValidateOrchestrationBindingReferencesAsync(
        IReadOnlyList<AgentPackageOrchestrationBindingV1> references,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedOrchestrationReference> values = _orchestrationCatalog is null
            ? []
            : await _orchestrationCatalog.ListPublishedAsync(cancellationToken);
        foreach (AgentPackageOrchestrationBindingV1 reference in references)
        {
            if (!Guid.TryParseExact(reference.OrchestrationId, "D", out Guid id) ||
                !Guid.TryParseExact(reference.OrchestrationVersionId, "D", out Guid versionId) ||
                !values.Any(value =>
                    value.OrchestrationId == id &&
                    value.OrchestrationVersionId == versionId &&
                    value.Enabled))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references an orchestration version that is not available.");
            }
        }

        return null;
    }

    private static bool TryReadPackage(
        string? json,
        out AgentPackageV1? package,
        out AgentError? error)
    {
        package = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json) ||
            Encoding.UTF8.GetByteCount(json) > MaximumPackageUtf8Bytes)
        {
            error = PackageInvalid("The Agent package is empty or exceeds the supported size.");
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaximumPackageDepth
                });
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                error = PackageInvalid("The Agent package root must be a JSON object.");
                return false;
            }

            int nodeCount = 0;
            if (!ValidateJsonSafety(document.RootElement, ref nodeCount, out string? safetyError))
            {
                error = PackageInvalid(safetyError!);
                return false;
            }

            package = JsonSerializer.Deserialize<AgentPackageV1>(
                json,
                AgentPackageSerializerOptions);
            if (package is null)
            {
                error = PackageInvalid("The Agent package is missing.");
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = PackageInvalid("The Agent package is not valid supported JSON.");
            return false;
        }
        catch (NotSupportedException)
        {
            error = PackageInvalid("The Agent package contains unsupported values.");
            return false;
        }
    }

    private bool TryValidatePackage(
        AgentPackageV1 package,
        out AgentRuntimeStatus runtimeStatus,
        out AgentOutputMode outputMode,
        out AgentError? error)
    {
        runtimeStatus = default;
        outputMode = default;
        error = null;

        if (!string.Equals(
                package.Format,
                AgentPackageFormatIdentifier,
                StringComparison.Ordinal))
        {
            error = PackageInvalid("The Agent package format identifier is not supported.");
            return false;
        }

        if (!TryParseSemanticVersion(package.Version, out int major))
        {
            error = PackageInvalid("The Agent package version is not a semantic version.");
            return false;
        }

        if (major != 1)
        {
            error = new AgentError(
                AgentErrorCodes.PackageVersionUnsupported,
                "The Agent package major version is not supported.");
            return false;
        }

        AgentPackageAgentV1? agent = package.Agent;
        if (agent is null ||
            agent.Draft is null ||
            agent.Deployment is null ||
            agent.Skills is null ||
            agent.Tools is null ||
            agent.Code is null ||
            agent.Name is null ||
            agent.Description is null ||
            agent.RuntimeStatus is null ||
            agent.Draft.Instructions is null ||
            agent.Draft.ModelProfileId is null ||
            agent.Draft.OutputMode is null)
        {
            error = PackageInvalid("The Agent package is missing required fields.");
            return false;
        }

        IReadOnlyList<string> knowledgeBases = agent.KnowledgeBases ?? [];
        if (knowledgeBases.Count > 32 ||
            knowledgeBases.Distinct(StringComparer.Ordinal).Count() != knowledgeBases.Count ||
            knowledgeBases.Any(reference => !Guid.TryParseExact(reference, "D", out _)))
        {
            error = PackageInvalid(
                "Knowledge references must be unique enabled knowledge base IDs.");
            return false;
        }

        IReadOnlyList<AgentPackageChildBindingV1> childAgents = agent.ChildAgents ?? [];
        if (childAgents.Any(value =>
                value is null ||
                !Guid.TryParseExact(value.AgentId, "D", out _) ||
                !Guid.TryParseExact(value.AgentVersionId, "D", out _)) ||
            childAgents.Select(value => Guid.Parse(value.AgentId)).Distinct().Count() !=
                childAgents.Count)
        {
            error = PackageInvalid(
                "Child Agent references must contain unique Agent and published version IDs.");
            return false;
        }

        IReadOnlyList<AgentPackageOrchestrationBindingV1> orchestrations =
            agent.Orchestrations ?? [];
        if (orchestrations.Any(value =>
                value is null ||
                !Guid.TryParseExact(value.OrchestrationId, "D", out _) ||
                !Guid.TryParseExact(value.OrchestrationVersionId, "D", out _)) ||
            orchestrations.Select(value => Guid.Parse(value.OrchestrationId)).Distinct().Count() !=
                orchestrations.Count)
        {
            error = PackageInvalid(
                "Orchestration references must contain unique orchestration and published version IDs.");
            return false;
        }

        if (!IsNormalizedCode(agent.Code))
        {
            error = PackageInvalid("Imported Agent code must be lowercase kebab-case.");
            return false;
        }

        if (string.Equals(
                agent.RuntimeStatus,
                nameof(AgentRuntimeStatus.Enabled),
                StringComparison.Ordinal))
        {
            runtimeStatus = AgentRuntimeStatus.Enabled;
        }
        else if (string.Equals(
                     agent.RuntimeStatus,
                     nameof(AgentRuntimeStatus.Disabled),
                     StringComparison.Ordinal))
        {
            runtimeStatus = AgentRuntimeStatus.Disabled;
        }
        else if (string.Equals(
                     agent.RuntimeStatus,
                     nameof(AgentRuntimeStatus.Archived),
                     StringComparison.Ordinal))
        {
            runtimeStatus = AgentRuntimeStatus.Archived;
        }
        else
        {
            error = PackageInvalid(
                "Runtime status must be Enabled, Disabled, or Archived.");
            return false;
        }

        if (string.Equals(
                agent.Draft.OutputMode,
                nameof(AgentOutputMode.Text),
                StringComparison.Ordinal))
        {
            outputMode = AgentOutputMode.Text;
        }
        else if (string.Equals(
                     agent.Draft.OutputMode,
                     nameof(AgentOutputMode.Structured),
                     StringComparison.Ordinal))
        {
            outputMode = AgentOutputMode.Structured;
        }
        else
        {
            error = PackageInvalid("Output mode must be Text or Structured.");
            return false;
        }

        bool supportedHost =
            string.Equals(
                agent.Deployment.Host,
                AgentDefinition.ApiHost,
                StringComparison.Ordinal) ||
            string.Equals(
                agent.Deployment.Host,
                AgentDefinition.LegacyApiHost,
                StringComparison.Ordinal);
        if (!string.Equals(
                agent.Deployment.Target,
                AgentDefinition.ServerDeploymentTarget,
                StringComparison.Ordinal) ||
            !supportedHost)
        {
            error = PackageInvalid("Deployment must target Server on EU.Core.Api.Agent.");
            return false;
        }

        if (agent.Tools.Count > 128 ||
            agent.Tools.Distinct(StringComparer.Ordinal).Count() != agent.Tools.Count ||
            agent.Tools.Any(reference => !Guid.TryParseExact(reference, "D", out _)))
        {
            error = PackageInvalid(
                "Tool references must be unique available MCP tool version IDs.");
            return false;
        }

        if (agent.Skills.Count > 64 ||
            agent.Skills.Distinct(StringComparer.Ordinal).Count() != agent.Skills.Count ||
            agent.Skills.Any(reference => !Guid.TryParseExact(reference, "D", out _)))
        {
            error = PackageInvalid(
                "Skill references must be unique published Skill version IDs.");
            return false;
        }

        if (outputMode is AgentOutputMode.Text)
        {
            if (agent.Draft.OutputJsonSchema is not null)
            {
                error = PackageInvalid("Text output cannot carry a JSON schema.");
                return false;
            }
        }
        else
        {
            JsonSchemaValidationResult schema =
                _jsonSchemaValidator.Validate(agent.Draft.OutputJsonSchema);
            if (!schema.IsValid)
            {
                error = new AgentError(
                    AgentErrorCodes.OutputSchemaInvalid,
                    schema.Error!);
                return false;
            }
        }

        return true;
    }

    private static bool ValidateJsonSafety(
        JsonElement element,
        ref int nodeCount,
        out string? error)
    {
        if (++nodeCount > MaximumPackageNodes)
        {
            error = "The Agent package exceeds the supported complexity.";
            return false;
        }

        if (element.ValueKind is JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    error = "The Agent package cannot contain duplicate property names.";
                    return false;
                }

                string normalizedName = new(property.Name
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray());
                if (ForbiddenPackagePropertyNames.Contains(normalizedName))
                {
                    error =
                        "The Agent package cannot contain credential, endpoint, or connection properties.";
                    return false;
                }

                if (!ValidateJsonSafety(property.Value, ref nodeCount, out error))
                {
                    return false;
                }
            }
        }
        else if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (!ValidateJsonSafety(item, ref nodeCount, out error))
                {
                    return false;
                }
            }
        }
        else if (element.ValueKind is JsonValueKind.String)
        {
            string value = element.GetString() ?? string.Empty;
            if (LooksLikeAbsolutePath(value) || LooksLikeSecretReference(value))
            {
                error =
                    "The Agent package cannot contain secret-shaped references or absolute paths.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool IsNormalizedCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        bool previousHyphen = false;
        foreach (char character in value)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                previousHyphen = false;
            }
            else if (character == '-' && !previousHyphen)
            {
                previousHyphen = true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseSemanticVersion(string? value, out int major)
    {
        major = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split('.');
        return parts.Length == 3 &&
               TryParseCanonicalNumericIdentifier(parts[0], out major) &&
               TryParseCanonicalNumericIdentifier(parts[1], out _) &&
               TryParseCanonicalNumericIdentifier(parts[2], out _);
    }

    private static bool TryParseCanonicalNumericIdentifier(string value, out int number)
    {
        number = 0;
        return value.Length > 0 &&
               (value.Length == 1 || value[0] != '0') &&
               value.All(char.IsAsciiDigit) &&
               int.TryParse(value, out number);
    }

    private static bool LooksLikeAbsolutePath(string value)
    {
        if (value.StartsWith("/", StringComparison.Ordinal) ||
            value.Contains(" /", StringComparison.Ordinal) ||
            value.Contains("\\\\", StringComparison.Ordinal) ||
            value.Contains("file:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        for (int index = 0; index + 2 < value.Length; index++)
        {
            if (char.IsAsciiLetter(value[index]) &&
                value[index + 1] == ':' &&
                (value[index + 2] == '\\' || value[index + 2] == '/'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeSecretReference(string value) =>
        value.Contains("alias:", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("sk-", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("password=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("api key=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("apikey=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("connection string=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("data source=", StringComparison.OrdinalIgnoreCase);

    private static AgentError PackageInvalid(string message) =>
        new(AgentErrorCodes.PackageInvalid, message);
}

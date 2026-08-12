using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.MainAgent;

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
    }

    /// <summary>
    /// 查询 Agent 管理列表，并批量加载草稿及最新发布版本摘要。
    /// </summary>
    public async Task<List<AgAgentDefinitionDto>> QueryAgentList(string search = null, string runtimeStatus = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string normalizedSearch = search?.Trim().ToLowerInvariant();
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
                        SqlFunc.ToLower(definition.Code).Contains(normalizedSearch) ||
                        SqlFunc.ToLower(definition.Name).Contains(normalizedSearch) ||
                        SqlFunc.ToLower(definition.Description).Contains(normalizedSearch))
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
                .GroupBy(version => version.AgentId.Value)
                .ToDictionary(group => group.Key, group => group.ToArray());

            List<AgAgentDefinitionDto> result = definitions.Select(definition =>
            {
                if (!versionsByAgent.TryGetValue(definition.ID, out AgAgentVersion[] agentVersions))
                    throw new InvalidDataException($"Agent '{definition.Code}' does not have any versions.");

                AgAgentVersion draft = agentVersions.SingleOrDefault(version => version.IsDraft == true)
                    ?? throw new InvalidDataException($"Agent '{definition.Code}' does not have exactly one Draft version.");
                AgAgentVersion currentPublished = agentVersions
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
    public async Task<AgAgentDefinitionDetailDto> QueryAgent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgAgentDefinition definition = await Db.Queryable<AgAgentDefinition>()
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

            var snapshotsByVersion = snapshots.ToDictionary(value => value.VersionId.Value);
            var bindingsByVersion = bindings
                .GroupBy(value => value.VersionId.Value)
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

#nullable enable

    private readonly IAgentRepository _repository;
    private readonly JsonSchemaValidator _jsonSchemaValidator;
    private readonly IPublishedSkillVersionCatalog? _skillVersions;
    private readonly IPublishedMcpToolCatalog? _toolVersions;
    private readonly IPublishedKnowledgeCatalog? _knowledgeBases;
    private readonly IPublishedOrchestrationCatalog? _orchestrationCatalog;
    private readonly IOrchestrationRepository? _orchestrations;
    private readonly IMainAgentAssignmentRepository? _mainAgentAssignments;

    public AgAgentDefinitionServices(
        IBaseRepository<AgAgentDefinition> dal,
        IAgentRepository repository,
        JsonSchemaValidator? jsonSchemaValidator = null,
        IPublishedSkillVersionCatalog? skillVersions = null,
        IPublishedMcpToolCatalog? toolVersions = null,
        IPublishedKnowledgeCatalog? knowledgeBases = null,
        IPublishedOrchestrationCatalog? orchestrationCatalog = null,
        IOrchestrationRepository? orchestrations = null,
        IMainAgentAssignmentRepository? mainAgentAssignments = null)
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
}

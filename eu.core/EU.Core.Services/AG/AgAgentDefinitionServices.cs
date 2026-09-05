using EU.Core.IServices.Agents;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Skills;
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
public class AgAgentDefinitionServices : BaseServices<AgAgentDefinition, AgAgentDefinitionDto, InsertAgAgentDefinitionInput, EditAgAgentDefinitionInput>, IAgAgentDefinitionServices, IAgentDefinitionCatalog
{
    private const string MainAgentAssignmentKey = "platform-main-agent";
    private readonly JsonSchemaValidator _jsonSchemaValidator;
    private readonly IPublishedSkillVersionCatalog? _skillVersions;
    private readonly IPublishedMcpToolCatalog? _toolVersions;
    private readonly IAgKnowledgeBaseDefinitionServices? _knowledgeBases;
    private readonly IPublishedOrchestrationCatalog? _orchestrationCatalog;
    private readonly IOrchestrationRepository? _orchestrations;
    private readonly IMainAgentAssignmentRepository? _mainAgentAssignments;
    private readonly IModelProfileReferenceCatalog? _modelProfiles;

    #region 构造

    public AgAgentDefinitionServices(
        IBaseRepository<AgAgentDefinition> dal,
        JsonSchemaValidator? jsonSchemaValidator = null,
        IPublishedSkillVersionCatalog? skillVersions = null,
        IPublishedMcpToolCatalog? toolVersions = null,
        IAgKnowledgeBaseDefinitionServices? knowledgeBases = null,
        IPublishedOrchestrationCatalog? orchestrationCatalog = null,
        IOrchestrationRepository? orchestrations = null,
        IMainAgentAssignmentRepository? mainAgentAssignments = null,
        IModelProfileReferenceCatalog? modelProfiles = null)
    {
        BaseDal = dal ?? throw new ArgumentNullException(nameof(dal));
        _jsonSchemaValidator = jsonSchemaValidator ?? new JsonSchemaValidator();
        _skillVersions = skillVersions;
        _toolVersions = toolVersions;
        _knowledgeBases = knowledgeBases;
        _orchestrationCatalog = orchestrationCatalog;
        _orchestrations = orchestrations;
        _mainAgentAssignments = mainAgentAssignments;
        _modelProfiles = modelProfiles;
    }

    #endregion

    #region 查询 Agent 管理列表
    /// <summary>
    /// 查询 Agent 管理列表，并批量加载草稿及最新发布版本摘要。
    /// </summary>
    public async Task<List<AgAgentDefinitionDto>> QueryAgentList(string? search = null, string? runtimeStatus = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? normalizedSearch = search?.Trim().ToLowerInvariant();
        await Db.Ado.BeginTranAsync(IsolationLevel.RepeatableRead);
        try
        {
            var definitions = await Db.Queryable<AgAgentDefinition>()
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
                    version.AgentId.HasValue &&
                    agentIds.Contains(version.AgentId.Value))
                .OrderBy(version => version.AgentId)
                .OrderBy(version => version.IsDraft, OrderByType.Desc)
                .OrderBy(version => version.Ordinal)
                .ToListAsync();

            var versionsByAgent = versions
                .GroupBy(version => version.AgentId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.ToArray());

            var result = new List<AgAgentDefinitionDto>(definitions.Count);
            foreach (AgAgentDefinition definition in definitions)
            {
                if (!versionsByAgent.TryGetValue(definition.ID, out AgAgentVersion[]? agentVersions) ||
                    agentVersions is null || agentVersions.Length == 0)
                {
                    continue;
                }

                AgAgentVersion[] drafts = agentVersions
                    .Where(version => version.IsDraft == true)
                    .ToArray();
                if (drafts.Length != 1)
                {
                    continue;
                }

                AgAgentVersion draft = drafts[0];
                AgAgentVersion? currentPublished = agentVersions
                    .Where(version => version.IsDraft != true)
                    .OrderBy(version => version.Ordinal)
                    .LastOrDefault();

                result.Add(new AgAgentDefinitionDto
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
                });
            }
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
    #endregion

    #region 查询 Agent 明细
    /// <summary>
    /// 查询 Agent 明细及其版本、快照和资源绑定。
    /// </summary>
    public async Task<AgAgentDefinitionDetailDto?> QueryAgent(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgAgentDefinition? definition = await Db.Queryable<AgAgentDefinition>()
                .Where(value => value.ID == id)
                .FirstAsync();
            if (definition is null)
            {
                await Db.Ado.CommitTranAsync();
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<AgAgentVersion> versions = await Db.Queryable<AgAgentVersion>()
                .Where(value => value.AgentId == id)
                .OrderBy(value => value.IsDraft, OrderByType.Desc)
                .OrderBy(value => value.Ordinal)
                .ToListAsync();
            if (versions.Count == 0)
            {
                await Db.Ado.CommitTranAsync();
                return null;
            }

            List<AgAgentVersionSnapshot> snapshots = await Db
                .Queryable<AgAgentVersionSnapshot, AgAgentVersion>(
                    (snapshot, version) => new JoinQueryInfos(
                        JoinType.Inner,
                        version.ID == snapshot.VersionId))
                .Where((snapshot, version) => version.AgentId == id)
                .Select((snapshot, version) => snapshot)
                .ToListAsync();
            List<AgAgentVersionBinding> bindings = await Db
                .Queryable<AgAgentVersionBinding, AgAgentVersion>(
                    (binding, version) => new JoinQueryInfos(
                        JoinType.Inner,
                        version.ID == binding.VersionId))
                .Where((binding, version) => version.AgentId == id)
                .OrderBy((binding, version) => binding.VersionId)
                .OrderBy((binding, version) => binding.Scope)
                .OrderBy((binding, version) => binding.BindingType)
                .OrderBy((binding, version) => binding.Ordinal)
                .Select((binding, version) => binding)
                .ToListAsync();

            var snapshotsByVersion = new Dictionary<Guid, AgAgentVersionSnapshot>();
            foreach (AgAgentVersionSnapshot snapshot in snapshots)
            {
                snapshotsByVersion[snapshot.VersionId.GetValueOrDefault()] = snapshot;
            }
            var bindingsByVersion = bindings
                .GroupBy(value => value.VersionId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.ToList());
            AgAgentVersion draft = versions.SingleOrDefault(value => value.IsDraft == true)
                ?? throw new InvalidDataException(
                    "The Agent does not have exactly one Draft version.");
            var result = new AgAgentDefinitionDetailDto
            {
                Id = definition.ID,
                Code = Required(definition.Code, "Code"),
                Name = Required(definition.Name, "Name"),
                Description = Required(definition.Description, "Description"),
                RuntimeStatus = ParseEnum<AgentRuntimeStatus>(
                    Required(definition.RuntimeStatus, "RuntimeStatus"),
                    "RuntimeStatus"),
                LogicalRevision = definition.LogicalRevision
                    ?? throw new InvalidDataException("Agent LogicalRevision is required."),
                Draft = MapVersionDto(
                    draft,
                    snapshotsByVersion.GetValueOrDefault(draft.ID),
                    bindingsByVersion.GetValueOrDefault(draft.ID) ?? []),
                PublishedVersions = versions
                    .Where(value => value.IsDraft != true)
                    .OrderBy(value => value.Ordinal)
                    .Select(value => MapVersionDto(
                        value,
                        snapshotsByVersion.GetValueOrDefault(value.ID),
                        bindingsByVersion.GetValueOrDefault(value.ID) ?? []))
                    .ToList()
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
    #endregion

    #region 持久化 DTO 映射

    private static AgAgentVersionDetailDto MapVersionDto(AgAgentVersion version, AgAgentVersionSnapshot? snapshot, IReadOnlyList<AgAgentVersionBinding> bindings)
    {
        _ = version.Ordinal ?? throw new InvalidDataException("Agent Version.Ordinal is required.");
        var result = new AgAgentVersionDetailDto
        {
            Id = version.ID,
            Label = Required(version.Label, "Version.Label"),
            IsDraft = version.IsDraft
                ?? throw new InvalidDataException("Agent Version.IsDraft is required."),
            Instructions = Required(version.Instructions, "Version.Instructions"),
            ModelProfileId = Required(version.ModelProfileId, "Version.ModelProfileId"),
            OutputMode = ParseEnum<AgentOutputMode>(
                Required(version.OutputMode, "Version.OutputMode"),
                "Version.OutputMode"),
            OutputJsonSchema = version.OutputJsonSchema,
            OutputSchemaSha256 = version.OutputSchemaSha256
        };

        foreach (AgAgentVersionBinding binding in bindings.OrderBy(value => value.Ordinal))
        {
            Guid referenceId = Required(binding.ReferenceId, "Binding.ReferenceId");
            if (binding.Scope == "Version")
            {
                AddVersionBinding(result, binding, referenceId);
                continue;
            }

            if (binding.Scope != "Snapshot" || snapshot is null)
            {
                throw new InvalidDataException(
                    $"Unknown Agent binding scope '{binding.Scope}'.");
            }
        }

        result.Snapshot = snapshot is null
            ? null
            : MapSnapshotDto(snapshot, bindings.Where(value => value.Scope == "Snapshot"));
        return result;
    }

    private static void AddVersionBinding(AgAgentVersionDetailDto target, AgAgentVersionBinding binding, Guid referenceId)
    {
        switch (binding.BindingType)
        {
            case "Skill":
                target.SkillVersionIds.Add(referenceId);
                break;
            case "Tool":
                target.ToolVersionIds.Add(referenceId);
                break;
            case "KnowledgeBase":
                target.KnowledgeBaseIds.Add(referenceId);
                break;
            case "ChildAgent":
                target.ChildAgentIds.Add(referenceId);
                if (binding.ReferenceVersionId is Guid childVersionId)
                {
                    target.ChildAgentPins.Add(new AgentChildBindingSnapshot(referenceId, childVersionId)
                    {
                        AgentCode = binding.ReferenceCode ?? string.Empty,
                        AgentName = binding.ReferenceName,
                        AgentDescription = binding.ReferenceDescription
                    });
                }
                break;
            case "Orchestration":
                target.OrchestrationIds.Add(referenceId);
                if (binding.ReferenceVersionId is Guid orchestrationVersionId)
                {
                    target.OrchestrationPins.Add(
                        new AgentOrchestrationBindingSnapshot(referenceId, orchestrationVersionId));
                }
                break;
            default:
                throw new InvalidDataException(
                    $"Unknown Agent binding type '{binding.BindingType}'.");
        }
    }

    private static AgAgentVersionSnapshotDetailDto MapSnapshotDto(AgAgentVersionSnapshot snapshot, IEnumerable<AgAgentVersionBinding> bindings)
    {
        var result = new AgAgentVersionSnapshotDetailDto
        {
            VersionId = Required(snapshot.SnapshotVersionId, "SnapshotVersionId"),
            AgentCode = Required(snapshot.AgentCode, "Snapshot.AgentCode"),
            AgentName = snapshot.AgentName,
            AgentDescription = snapshot.AgentDescription,
            Instructions = Required(snapshot.Instructions, "Snapshot.Instructions"),
            ModelProfileId = Required(snapshot.ModelProfileId, "Snapshot.ModelProfileId"),
            OutputMode = ParseEnum<AgentOutputMode>(
                Required(snapshot.OutputMode, "Snapshot.OutputMode"),
                "Snapshot.OutputMode"),
            OutputJsonSchema = snapshot.OutputJsonSchema
        };

        foreach (AgAgentVersionBinding binding in bindings.OrderBy(value => value.Ordinal))
        {
            Guid referenceId = Required(binding.ReferenceId, "Binding.ReferenceId");
            switch (binding.BindingType)
            {
                case "Skill":
                    result.Skills.Add(new AgentSkillBindingSnapshot(referenceId));
                    break;
                case "Tool":
                    result.Tools.Add(new AgentToolBindingSnapshot(referenceId));
                    break;
                case "KnowledgeBase":
                    result.KnowledgeBases.Add(new AgentKnowledgeBindingSnapshot(
                        referenceId,
                        binding.LogicalRevision ?? throw new InvalidDataException(
                            "A snapshot knowledge binding requires a revision.")));
                    break;
                case "ChildAgent":
                    result.ChildAgents.Add(new AgentChildBindingSnapshot(
                        referenceId,
                        Required(binding.ReferenceVersionId, "ChildAgent.ReferenceVersionId"))
                    {
                        AgentCode = binding.ReferenceCode ?? string.Empty,
                        AgentName = binding.ReferenceName,
                        AgentDescription = binding.ReferenceDescription
                    });
                    break;
                case "Orchestration":
                    result.Orchestrations.Add(new AgentOrchestrationBindingSnapshot(
                        referenceId,
                        Required(
                            binding.ReferenceVersionId,
                            "Orchestration.ReferenceVersionId")));
                    break;
                default:
                    throw new InvalidDataException(
                        $"Unknown Agent snapshot binding type '{binding.BindingType}'.");
            }
        }

        return result;
    }

    private static Guid Required(Guid? value, string name) =>
        value ?? throw new InvalidDataException($"Agent {name} is required.");

    private static string Required(string? value, string name) =>
        value ?? throw new InvalidDataException($"Agent {name} is required.");

    private static T ParseEnum<T>(string value, string name) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out T parsed)
            ? parsed
            : throw new InvalidDataException(
                $"Agent {name} contains unsupported value '{value}'.");

    #endregion

    #region Agent 持久化

    private async Task<bool> CreateAgentAsync(
        AgentDefinition definition,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(IsolationLevel.Serializable);
        try
        {
            if (await AnyAsync(value => value.ID == definition.Id || value.Code == definition.Code))
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            var entity = new AgAgentDefinition
            {
                ID = definition.Id,
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                RuntimeStatus = definition.RuntimeStatus.ToString(),
                LogicalRevision = definition.LogicalRevision
            };
            var draft = MapVersionEntity(definition.Id, 0, definition.Draft);
            var bindings = MapVersionBindingEntities(definition.Draft);

            await Db.Insertable(entity).ExecuteCommandAsync();
            await Db.Insertable(draft).ExecuteCommandAsync();
            if (bindings.Any())
                await Db.Insertable(bindings).ExecuteCommandAsync();

            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private static AgAgentVersion MapVersionEntity(
        Guid agentId,
        int ordinal,
        AgentVersion version) =>
        new()
        {
            ID = version.Id,
            AgentId = agentId,
            Ordinal = ordinal,
            Label = version.Label,
            IsDraft = version.IsDraft,
            Instructions = version.Instructions,
            ModelProfileId = version.ModelProfileId,
            OutputMode = version.OutputMode.ToString(),
            OutputJsonSchema = version.OutputJsonSchema,
            OutputSchemaSha256 = version.OutputSchemaSha256
        };

    private static List<AgAgentVersionBinding> MapVersionBindingEntities(AgentVersion version)
    {
        var result = new List<AgAgentVersionBinding>();
        AddSimpleBindingEntities(result, version.Id, "Skill", version.SkillVersionIds);
        AddSimpleBindingEntities(result, version.Id, "Tool", version.ToolVersionIds);
        AddSimpleBindingEntities(result, version.Id, "KnowledgeBase", version.KnowledgeBaseIds);

        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot> childPins =
            version.ChildAgentPins.ToDictionary(value => value.AgentId);
        for (int index = 0; index < version.ChildAgentIds.Count; index++)
        {
            Guid referenceId = version.ChildAgentIds[index];
            childPins.TryGetValue(referenceId, out AgentChildBindingSnapshot? pin);
            result.Add(NewBindingEntity(
                version.Id,
                "ChildAgent",
                index,
                referenceId,
                pin?.AgentVersionId,
                pin?.AgentCode,
                pin?.AgentName,
                pin?.AgentDescription));
        }

        IReadOnlyDictionary<Guid, AgentOrchestrationBindingSnapshot> orchestrationPins =
            version.OrchestrationPins.ToDictionary(value => value.OrchestrationId);
        for (int index = 0; index < version.OrchestrationIds.Count; index++)
        {
            Guid referenceId = version.OrchestrationIds[index];
            orchestrationPins.TryGetValue(referenceId, out AgentOrchestrationBindingSnapshot? pin);
            result.Add(NewBindingEntity(
                version.Id,
                "Orchestration",
                index,
                referenceId,
                pin?.OrchestrationVersionId));
        }

        return result;
    }

    private static void AddSimpleBindingEntities(
        ICollection<AgAgentVersionBinding> target,
        Guid versionId,
        string bindingType,
        IReadOnlyList<Guid> referenceIds)
    {
        for (int index = 0; index < referenceIds.Count; index++)
        {
            target.Add(NewBindingEntity(
                versionId,
                bindingType,
                index,
                referenceIds[index]));
        }
    }

    private static AgAgentVersionBinding NewBindingEntity(
        Guid versionId,
        string bindingType,
        int ordinal,
        Guid referenceId,
        Guid? referenceVersionId = null,
        string? referenceCode = null,
        string? referenceName = null,
        string? referenceDescription = null,
        string scope = "Version",
        long? logicalRevision = null) =>
        new()
        {
            ID = Guid.NewGuid(),
            VersionId = versionId,
            Scope = scope,
            BindingType = bindingType,
            Ordinal = ordinal,
            ReferenceId = referenceId,
            ReferenceVersionId = referenceVersionId,
            LogicalRevision = logicalRevision,
            ReferenceCode = referenceCode,
            ReferenceName = referenceName,
            ReferenceDescription = referenceDescription
        };

    #endregion

    #region Agent 运行时目录

    public async Task<AgentDefinition?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        AgAgentDefinitionDetailDto? value = await QueryAgent(id, cancellationToken);
        return value is null ? null : MapAgentDefinition(value);
    }

    public async Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? normalizedSearch = query.Search?.Trim().ToLowerInvariant();
        string? runtimeStatus = query.RuntimeStatus?.ToString();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgAgentDefinition> definitions = await Db.Queryable<AgAgentDefinition>()
                .WhereIF(
                    runtimeStatus.IsNullOrEmpty(),
                    value => value.RuntimeStatus != "Archived")
                .WhereIF(
                    runtimeStatus.IsNotEmptyOrNull(),
                    value => value.RuntimeStatus == runtimeStatus)
                .WhereIF(
                    normalizedSearch.IsNotEmptyOrNull(),
                    value =>
                        SqlFunc.ToLower(value.Code).Contains(normalizedSearch!) ||
                        SqlFunc.ToLower(value.Name).Contains(normalizedSearch!) ||
                        SqlFunc.ToLower(value.Description).Contains(normalizedSearch!))
                .OrderBy(value => value.Code)
                .OrderBy(value => value.ID)
                .ToListAsync();
            if (definitions.Count == 0)
            {
                await Db.Ado.CommitTranAsync();
                return [];
            }

            cancellationToken.ThrowIfCancellationRequested();
            Guid[] agentIds = definitions.Select(value => value.ID).ToArray();
            List<AgAgentVersion> versions = await Db.Queryable<AgAgentVersion>()
                .Where(value =>
                    value.AgentId.HasValue &&
                    agentIds.Contains(value.AgentId.Value))
                .OrderBy(value => value.AgentId)
                .OrderBy(value => value.IsDraft, OrderByType.Desc)
                .OrderBy(value => value.Ordinal)
                .ToListAsync();
            List<AgAgentVersionSnapshot> snapshots = await Db
                .Queryable<AgAgentVersionSnapshot, AgAgentVersion>(
                    (snapshot, version) => new JoinQueryInfos(
                        JoinType.Inner,
                        version.ID == snapshot.VersionId))
                .Where((snapshot, version) =>
                    version.AgentId.HasValue &&
                    agentIds.Contains(version.AgentId.Value))
                .Select((snapshot, version) => snapshot)
                .ToListAsync();
            List<AgAgentVersionBinding> bindings = await Db
                .Queryable<AgAgentVersionBinding, AgAgentVersion>(
                    (binding, version) => new JoinQueryInfos(
                        JoinType.Inner,
                        version.ID == binding.VersionId))
                .Where((binding, version) =>
                    version.AgentId.HasValue &&
                    agentIds.Contains(version.AgentId.Value))
                .OrderBy((binding, version) => binding.VersionId)
                .OrderBy((binding, version) => binding.Scope)
                .OrderBy((binding, version) => binding.BindingType)
                .OrderBy((binding, version) => binding.Ordinal)
                .Select((binding, version) => binding)
                .ToListAsync();

            var versionsByAgent = versions
                .GroupBy(value => value.AgentId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.ToArray());
            var snapshotsByVersion = snapshots
                .ToDictionary(value => value.VersionId.GetValueOrDefault());
            var bindingsByVersion = bindings
                .GroupBy(value => value.VersionId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.ToList());
            var result = new List<AgentDefinition>(definitions.Count);
            foreach (AgAgentDefinition definition in definitions)
            {
                if (!versionsByAgent.TryGetValue(
                        definition.ID,
                        out AgAgentVersion[]? agentVersions) ||
                    agentVersions.Count(value => value.IsDraft == true) != 1)
                {
                    continue;
                }

                AgAgentVersion draft = agentVersions.Single(value => value.IsDraft == true);
                var detail = new AgAgentDefinitionDetailDto
                {
                    Id = definition.ID,
                    Code = Required(definition.Code, "Code"),
                    Name = Required(definition.Name, "Name"),
                    Description = Required(definition.Description, "Description"),
                    RuntimeStatus = ParseEnum<AgentRuntimeStatus>(
                        Required(definition.RuntimeStatus, "RuntimeStatus"),
                        "RuntimeStatus"),
                    LogicalRevision = definition.LogicalRevision
                        ?? throw new InvalidDataException(
                            "Agent LogicalRevision is required."),
                    Draft = MapVersionDto(
                        draft,
                        snapshotsByVersion.GetValueOrDefault(draft.ID),
                        bindingsByVersion.GetValueOrDefault(draft.ID) ?? []),
                    PublishedVersions = agentVersions
                        .Where(value => value.IsDraft != true)
                        .OrderBy(value => value.Ordinal)
                        .Select(value => MapVersionDto(
                            value,
                            snapshotsByVersion.GetValueOrDefault(value.ID),
                            bindingsByVersion.GetValueOrDefault(value.ID) ?? []))
                        .ToList()
                };
                result.Add(MapAgentDefinition(detail));
            }

            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return AgentContractCloner.ReadOnly(result);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private static AgentDefinition MapAgentDefinition(AgAgentDefinitionDetailDto value) =>
        new(
            value.Id,
            value.Code,
            value.Name,
            value.Description,
            value.RuntimeStatus,
            value.LogicalRevision,
            MapAgentVersion(value.Draft),
            AgentContractCloner.ReadOnly(value.PublishedVersions.Select(MapAgentVersion)));

    private static AgentVersion MapAgentVersion(AgAgentVersionDetailDto value) =>
        new(
            value.Id,
            value.Label,
            value.IsDraft,
            value.Instructions,
            value.ModelProfileId,
            value.OutputMode,
            value.OutputJsonSchema,
            value.OutputSchemaSha256,
            value.Snapshot is null ? null : MapAgentSnapshot(value.Snapshot))
        {
            SkillVersionIds = AgentContractCloner.ReadOnly(value.SkillVersionIds),
            ToolVersionIds = AgentContractCloner.ReadOnly(value.ToolVersionIds),
            KnowledgeBaseIds = AgentContractCloner.ReadOnly(value.KnowledgeBaseIds),
            ChildAgentIds = AgentContractCloner.ReadOnly(value.ChildAgentIds),
            OrchestrationIds = AgentContractCloner.ReadOnly(value.OrchestrationIds),
            ChildAgentPins = AgentContractCloner.ReadOnly(
                value.ChildAgentPins.Select(pin => pin with { })),
            OrchestrationPins = AgentContractCloner.ReadOnly(
                value.OrchestrationPins.Select(pin => pin with { }))
        };

    private static AgentVersionSnapshot MapAgentSnapshot(
        AgAgentVersionSnapshotDetailDto value) =>
        new(
            value.VersionId,
            value.AgentCode,
            value.Instructions,
            value.ModelProfileId,
            value.OutputMode,
            value.OutputJsonSchema,
            AgentContractCloner.ReadOnly(value.Skills.Select(item => item with { })),
            AgentContractCloner.ReadOnly(value.Tools.Select(item => item with { })))
        {
            AgentName = value.AgentName,
            AgentDescription = value.AgentDescription,
            KnowledgeBases = AgentContractCloner.ReadOnly(
                value.KnowledgeBases.Select(item => item with { })),
            ChildAgents = AgentContractCloner.ReadOnly(
                value.ChildAgents.Select(item => item with { })),
            Orchestrations = AgentContractCloner.ReadOnly(
                value.Orchestrations.Select(item => item with { }))
        };

    #endregion

    #region Agent 版本持久化

    private async Task<bool> TryReplaceAgentAsync(
        AgentDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken,
        Guid? advanceMainAgentToVersionId = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (expectedLogicalRevision == long.MaxValue ||
            definition.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(IsolationLevel.Serializable);
        try
        {
            var entity = new AgAgentDefinition
            {
                ID = definition.Id,
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                RuntimeStatus = definition.RuntimeStatus.ToString(),
                LogicalRevision = definition.LogicalRevision
            };
            int affected = await Db.Updateable(entity)
                .UpdateColumns(value => new
                {
                    value.Name,
                    value.Description,
                    value.RuntimeStatus,
                    value.LogicalRevision
                })
                .Where(value =>
                    value.ID == definition.Id &&
                    value.Code == definition.Code &&
                    value.LogicalRevision == expectedLogicalRevision)
                .ExecuteCommandAsync();
            if (affected != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            List<Guid> existingPublishedIds = await Db.Queryable<AgAgentVersion>()
                .Where(value => value.AgentId == definition.Id && value.IsDraft == false)
                .OrderBy(value => value.Ordinal)
                .Select(value => value.ID)
                .ToListAsync();
            if (existingPublishedIds.Count > definition.PublishedVersions.Count ||
                existingPublishedIds.Where((id, index) =>
                    definition.PublishedVersions[index].Id != id).Any())
            {
                throw new InvalidDataException(
                    "Published Agent versions are immutable and must remain an ordered prefix.");
            }

            List<Guid> draftIds = await Db.Queryable<AgAgentVersion>()
                .Where(value => value.AgentId == definition.Id && value.IsDraft == true)
                .Select(value => value.ID)
                .ToListAsync();
            foreach (Guid draftId in draftIds)
            {
                await Db.Deleteable<AgAgentVersionBinding>()
                    .Where(value => value.VersionId == draftId)
                    .ExecuteCommandAsync();
                await Db.Deleteable<AgAgentVersionSnapshot>()
                    .Where(value => value.VersionId == draftId)
                    .ExecuteCommandAsync();
            }
            await Db.Deleteable<AgAgentVersion>()
                .Where(value => value.AgentId == definition.Id && value.IsDraft == true)
                .ExecuteCommandAsync();

            await WriteAgentVersionAsync(definition.Id, 0, definition.Draft);
            for (int index = existingPublishedIds.Count;
                 index < definition.PublishedVersions.Count;
                 index++)
            {
                await WriteAgentVersionAsync(
                    definition.Id,
                    index,
                    definition.PublishedVersions[index]);
            }

            if (advanceMainAgentToVersionId.HasValue && _mainAgentAssignments is not null)
            {
                AgMainAgentAssignment? assignment = await Db.Queryable<AgMainAgentAssignment>()
                    .Where(value =>
                        value.AssignmentKey == MainAgentAssignmentKey &&
                        value.AgentId == definition.Id)
                    .FirstAsync();
                if (assignment is not null)
                {
                    long assignmentRevision = assignment.LogicalRevision
                        ?? throw new InvalidDataException(
                            "Main Agent assignment field 'LogicalRevision' is missing.");
                    int assignmentAffected = await Db.Updateable<AgMainAgentAssignment>()
                        .SetColumns(_ => new AgMainAgentAssignment
                        {
                            AgentVersionId = advanceMainAgentToVersionId.Value,
                            LogicalRevision = assignmentRevision + 1,
                            UpdatedAtUtc = DateTime.UtcNow
                        })
                        .Where(value =>
                            value.ID == assignment.ID &&
                            value.LogicalRevision == assignmentRevision)
                        .ExecuteCommandAsync();
                    if (assignmentAffected != 1)
                    {
                        throw new InvalidOperationException(
                            "The Main Agent assignment changed while publishing its new version.");
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task WriteAgentVersionAsync(
        Guid agentId,
        int ordinal,
        AgentVersion version)
    {
        await Db.Insertable(MapVersionEntity(agentId, ordinal, version)).ExecuteCommandAsync();
        List<AgAgentVersionBinding> bindings = MapVersionBindingEntities(version);
        if (version.Snapshot is not null)
        {
            await Db.Insertable(MapSnapshotEntity(version.Id, version.Snapshot)).ExecuteCommandAsync();
            bindings.AddRange(MapSnapshotBindingEntities(version.Id, version.Snapshot));
        }

        if (bindings.Count > 0)
        {
            await Db.Insertable(bindings).ExecuteCommandAsync();
        }
    }

    private static AgAgentVersionSnapshot MapSnapshotEntity(
        Guid versionId,
        AgentVersionSnapshot snapshot) =>
        new()
        {
            ID = versionId,
            VersionId = versionId,
            SnapshotVersionId = snapshot.VersionId,
            AgentCode = snapshot.AgentCode,
            AgentName = snapshot.AgentName,
            AgentDescription = snapshot.AgentDescription,
            Instructions = snapshot.Instructions,
            ModelProfileId = snapshot.ModelProfileId,
            OutputMode = snapshot.OutputMode.ToString(),
            OutputJsonSchema = snapshot.OutputJsonSchema
        };

    private static List<AgAgentVersionBinding> MapSnapshotBindingEntities(
        Guid versionId,
        AgentVersionSnapshot snapshot)
    {
        var result = new List<AgAgentVersionBinding>();
        AddSnapshotSimpleBindings(result, versionId, "Skill",
            snapshot.Skills.Select(value => value.SkillVersionId));
        AddSnapshotSimpleBindings(result, versionId, "Tool",
            snapshot.Tools.Select(value => value.ToolVersionId));
        for (int index = 0; index < snapshot.KnowledgeBases.Count; index++)
        {
            AgentKnowledgeBindingSnapshot value = snapshot.KnowledgeBases[index];
            result.Add(NewBindingEntity(
                versionId, "KnowledgeBase", index, value.KnowledgeBaseId,
                scope: "Snapshot", logicalRevision: value.LogicalRevision));
        }
        for (int index = 0; index < snapshot.ChildAgents.Count; index++)
        {
            AgentChildBindingSnapshot value = snapshot.ChildAgents[index];
            result.Add(NewBindingEntity(
                versionId, "ChildAgent", index, value.AgentId, value.AgentVersionId,
                value.AgentCode, value.AgentName, value.AgentDescription, "Snapshot"));
        }
        for (int index = 0; index < snapshot.Orchestrations.Count; index++)
        {
            AgentOrchestrationBindingSnapshot value = snapshot.Orchestrations[index];
            result.Add(NewBindingEntity(
                versionId, "Orchestration", index, value.OrchestrationId,
                value.OrchestrationVersionId, scope: "Snapshot"));
        }

        return result;
    }

    private static void AddSnapshotSimpleBindings(
        ICollection<AgAgentVersionBinding> target,
        Guid versionId,
        string bindingType,
        IEnumerable<Guid> referenceIds)
    {
        int ordinal = 0;
        foreach (Guid referenceId in referenceIds)
        {
            target.Add(NewBindingEntity(
                versionId, bindingType, ordinal++, referenceId, scope: "Snapshot"));
        }
    }

    #endregion

    #region Agent 管理

    private IModelProfileReferenceCatalog ModelProfiles =>
        _modelProfiles ?? throw AgentManagementUnavailable();

    public async Task<ServiceResult<Guid>> CreateAsync(CreateAgentCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(command);
        if (!TryNormalizeCode(command.Code, out string? normalizedCode))
        {
            return Failed<Guid>("Agent code must normalize to lowercase kebab-case.");
        }

        Guid id = Guid.NewGuid();
        var draft = new AgentVersion(
            Guid.NewGuid(),
            "0.1.0",
            true,
            string.Empty,
            string.Empty,
            AgentOutputMode.Text,
            null,
            null,
            null);
        var definition = new AgentDefinition(
            id,
            normalizedCode!,
            command.Name ?? string.Empty,
            command.Description ?? string.Empty,
            AgentRuntimeStatus.Enabled,
            0,
            draft,
            AgentContractCloner.ReadOnly(Array.Empty<AgentVersion>()));
        if (!await CreateAgentAsync(definition, cancellationToken))
        {
            return Failed<Guid>("An Agent already uses this code.");
        }

        return Success(id);
    }

    public async Task<ServiceResult<AgentDefinition>> CreateImportedAsync(ImportAgentCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(command);
        if (!TryNormalizeCode(command.Code, out string? normalizedCode) ||
            !string.Equals(command.Code, normalizedCode, StringComparison.Ordinal))
        {
            return Failed<AgentDefinition>(
                "Imported Agent code must already be lowercase kebab-case.");
        }

        if (!Enum.IsDefined(command.RuntimeStatus))
        {
            return Failed<AgentDefinition>(
                "Runtime status must be Enabled, Disabled, or Archived.");
        }

        if (command.OutputMode is AgentOutputMode.Text)
        {
            if (command.OutputJsonSchema is not null)
            {
                return Failed<AgentDefinition>(
                    "Text output cannot carry a JSON schema.");
            }
        }
        else if (command.OutputMode is AgentOutputMode.Structured)
        {
            JsonSchemaValidationResult validation = _jsonSchemaValidator.Validate(command.OutputJsonSchema);
            if (!validation.IsValid)
            {
                return Failed<AgentDefinition>(validation.Error!);
            }
        }
        else
        {
            return Failed<AgentDefinition>("Output mode is not supported.");
        }

        IReadOnlyList<Guid> importedSkillVersionIds =
            command.SkillVersionIds ?? Array.Empty<Guid>();
        ServiceResult<AgentDefinition>? importedSkillError =
            await ValidateSkillVersionsAsync(importedSkillVersionIds, cancellationToken);
        if (importedSkillError is not null)
        {
            return importedSkillError;
        }

        IReadOnlyList<Guid> importedToolVersionIds =
            command.ToolVersionIds ?? Array.Empty<Guid>();
        ServiceResult<AgentDefinition>? importedToolError =
            await ValidateToolVersionsAsync(importedToolVersionIds, cancellationToken);
        if (importedToolError is not null)
        {
            return importedToolError;
        }

        IReadOnlyList<Guid> importedKnowledgeBaseIds =
            command.KnowledgeBaseIds ?? Array.Empty<Guid>();
        ServiceResult<AgentDefinition>? importedKnowledgeError =
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
        if (!await CreateAgentAsync(definition, cancellationToken))
        {
            return Failed<AgentDefinition>("An Agent already uses this code.");
        }

        return Success(definition);
    }

    public async Task<ServiceResult<AgentDefinition>> SaveDraftAsync(SaveAgentDraftCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(command);
        AgentDefinition? existing = await GetDefinitionAsync(command.AgentId, cancellationToken);
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
            return Failed<AgentDefinition>(
                "An archived Agent must be restored before its Draft can be edited.");
        }

        IReadOnlyList<Guid> skillVersionIds = command.SkillVersionIds ??
                                              existing.Draft.SkillVersionIds;
        ServiceResult<AgentDefinition>? skillError =
            await ValidateSkillVersionsAsync(skillVersionIds, cancellationToken);
        if (skillError is not null)
        {
            return skillError;
        }

        IReadOnlyList<Guid> toolVersionIds = command.ToolVersionIds ??
                                             existing.Draft.ToolVersionIds;
        ServiceResult<AgentDefinition>? toolError =
            await ValidateToolVersionsAsync(toolVersionIds, cancellationToken);
        if (toolError is not null)
        {
            return toolError;
        }

        IReadOnlyList<Guid> knowledgeBaseIds = command.KnowledgeBaseIds ??
                                               existing.Draft.KnowledgeBaseIds;
        ServiceResult<AgentDefinition>? knowledgeError =
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
        if (!await TryReplaceAgentAsync(updated, command.ExpectedLogicalRevision, cancellationToken))
        {
            return RowVersionConflict();
        }

        return Success(updated);
    }

    public async Task<ServiceResult<AgentDefinition>> SetRuntimeStatusAsync(SetAgentRuntimeStatusCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.RuntimeStatus))
        {
            return Failed<AgentDefinition>("Runtime status must be Enabled, Disabled, or Archived.");
        }

        AgentDefinition? existing = await GetDefinitionAsync(command.AgentId, cancellationToken);
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
            return Failed<AgentDefinition>(
                "An Agent must be disabled before it can be archived.");
        }

        if (command.RuntimeStatus is AgentRuntimeStatus.Archived)
        {
            IReadOnlyList<string> blockers = await FindArchiveBlockersAsync(
                existing.Id,
                cancellationToken);
            if (blockers.Count > 0)
            {
                return Failed<AgentDefinition>(
                    $"The Agent is still referenced by {string.Join(", ", blockers)}.");
            }
        }

        if (existing.RuntimeStatus is AgentRuntimeStatus.Archived &&
            command.RuntimeStatus is not AgentRuntimeStatus.Disabled)
        {
            return Failed<AgentDefinition>(
                "An archived Agent must be restored to Disabled before it can be enabled.");
        }

        AgentDefinition updated = existing with { RuntimeStatus = command.RuntimeStatus, LogicalRevision = existing.LogicalRevision + 1 };
        if (!await TryReplaceAgentAsync(updated, command.ExpectedLogicalRevision, cancellationToken))
        {
            return RowVersionConflict();
        }

        return Success(updated);
    }

    public async Task<ServiceResult<AgentDefinition>> PublishAsync(PublishAgentCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(command);
        AgentDefinition? existing = await GetDefinitionAsync(command.AgentId, cancellationToken);
        if (existing is null)
            return NotFound();

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return RowVersionConflict();
        }

        if (existing.RuntimeStatus is AgentRuntimeStatus.Archived)
        {
            return Failed<AgentDefinition>(
                "An archived Agent must be restored before a version can be published.");
        }

        AgentVersion draft = existing.Draft;
        if (string.IsNullOrWhiteSpace(draft.Instructions) || string.IsNullOrWhiteSpace(draft.ModelProfileId))
        {
            return Failed<AgentDefinition>("Instructions and ModelProfileId are required before publish.");
        }

        string? canonicalSchema = null;
        string? schemaHash = null;
        if (draft.OutputMode is AgentOutputMode.Text)
        {
            if (draft.OutputJsonSchema is not null)
            {
                return Failed<AgentDefinition>("Text output cannot carry a JSON schema.");
            }
        }
        else if (draft.OutputMode is AgentOutputMode.Structured)
        {
            JsonSchemaValidationResult validation = _jsonSchemaValidator.Validate(draft.OutputJsonSchema);
            if (!validation.IsValid)
            {
                return Failed<AgentDefinition>(validation.Error!);
            }

            canonicalSchema = validation.CanonicalJson;
            schemaHash = validation.Sha256;
        }
        else
        {
            return Failed<AgentDefinition>("Output mode is not supported.");
        }

        ServiceResult<IReadOnlyList<AgentChildBindingSnapshot>> childBindings =
            await ResolveChildAgentBindingsAsync(existing.Id, draft, cancellationToken);
        if (!childBindings.Success)
        {
            return Failed<AgentDefinition>(childBindings.Message);
        }

        ServiceResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>> orchestrationBindings =
            await ResolveOrchestrationBindingsAsync(draft, cancellationToken);
        if (!orchestrationBindings.Success)
        {
            return Failed<AgentDefinition>(orchestrationBindings.Message);
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
            ChildAgents = childBindings.Data,
            Orchestrations = orchestrationBindings.Data
        };
        var published = new AgentVersion(versionId, label, false, draft.Instructions, draft.ModelProfileId, draft.OutputMode, canonicalSchema, schemaHash, snapshot);
        AgentDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            PublishedVersions = AgentContractCloner.ReadOnly(existing.PublishedVersions.Append(published))
        };
        if (!await TryReplaceAgentAsync(
                updated,
                command.ExpectedLogicalRevision,
                cancellationToken,
                versionId))
        {
            return RowVersionConflict();
        }

        return Success(updated);
    }

    public async Task<IReadOnlyList<AgentListItem>> ListAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        ArgumentNullException.ThrowIfNull(query);
        IReadOnlyList<AgentDefinition> definitions = await ListDefinitionsAsync(query, cancellationToken);
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
    #region 检查当前 Host 是否启用了 Agent 管理能力
    /// <summary>
    /// 检查当前 Host 是否启用了 Agent 管理能力。
    /// 仅负责运行 Agent 的 Host 可以不注册管理依赖；如果误调用创建、编辑、发布、
    /// 导入或导出等管理操作，则在这里提前抛出明确的配置异常。
    /// </summary>
    /// <remarks>
    /// 模型配置目录是 Agent 管理操作必须具备的依赖，因此使用
    /// <see cref="_modelProfiles"/> 是否已注册作为管理能力可用性的判断标志。
    /// </remarks>
    private void EnsureAgentManagementAvailable()
    {
        // 避免管理依赖缺失时继续执行，并在后续流程中产生不明确的空引用异常。
        if (_modelProfiles is null)
            throw AgentManagementUnavailable();
    }
    #endregion

    private static InvalidOperationException AgentManagementUnavailable() => new("Agent management dependencies are not registered in this Host.");

    private ServiceResult<AgentDefinition> NotFound() => Failed<AgentDefinition>("The Agent was not found.");

    private ServiceResult<AgentDefinition> RowVersionConflict() => Failed<AgentDefinition>("The Agent changed before this operation completed.");

    private async Task<IReadOnlyList<string>> FindArchiveBlockersAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var blockers = new List<string>();
        IReadOnlyList<AgentDefinition> enabledAgents = await ListDefinitionsAsync(
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

    private async Task<ServiceResult<AgentDefinition>?> ValidateSkillVersionsAsync(
        IReadOnlyList<Guid> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count != versionIds.Distinct().Count())
        {
            return Failed<AgentDefinition>(
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
            return Failed<AgentDefinition>(
                "Agent Drafts may bind only published Skill versions.");
        }

        return null;
    }

    private async Task<ServiceResult<AgentDefinition>?> ValidateToolVersionsAsync(
        IReadOnlyList<Guid> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count > 128 ||
            versionIds.Count != versionIds.Distinct().Count())
        {
            return Failed<AgentDefinition>(
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
            return Failed<AgentDefinition>(
                "Agent Drafts may bind only classified MCP tool versions.");
        }

        return null;
    }

    private async Task<ServiceResult<AgentDefinition>?> ValidateKnowledgeBasesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count > 32 || ids.Count != ids.Distinct().Count())
        {
            return Failed<AgentDefinition>(
                "Agent knowledge bindings must contain no more than 32 unique knowledge bases.");
        }

        IReadOnlySet<Guid> available = (await GetKnowledgeReferencesAsync(ids, cancellationToken))
            .Select(value => value.KnowledgeBaseId)
            .ToHashSet();
        if (ids.Any(id => id == Guid.Empty || !available.Contains(id)))
        {
            return Failed<AgentDefinition>(
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
        return Common.Extensions.CollectionExtensions.ToReadOnlyList(
            (await _knowledgeBases.ListPublishedAsync(cancellationToken))
            .Where(value => selected.Contains(value.KnowledgeBaseId)));
    }

    private async Task<ServiceResult<IReadOnlyList<AgentChildBindingSnapshot>>> ResolveChildAgentBindingsAsync(
        Guid agentId,
        AgentVersion draft,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> childAgentIds = draft.ChildAgentIds;
        if (childAgentIds.Count > AgentDelegationPolicy.MaximumChildAgentBindings)
        {
            return Failed<IReadOnlyList<AgentChildBindingSnapshot>>(
                $"Main Agent publications may bind no more than {AgentDelegationPolicy.MaximumChildAgentBindings} child Agents.");
        }

        if (childAgentIds.Count != childAgentIds.Distinct().Count() ||
            childAgentIds.Any(id => id == Guid.Empty || id == agentId))
        {
            return Failed<IReadOnlyList<AgentChildBindingSnapshot>>(
                "Child Agent bindings must contain unique published Agent identities other than the Agent itself.");
        }

        if (draft.ChildAgentPins.Count > 0 &&
            (draft.ChildAgentPins.Select(value => value.AgentId).Distinct().Count() != draft.ChildAgentPins.Count ||
             draft.ChildAgentPins.Count != childAgentIds.Count ||
             draft.ChildAgentPins.Select(value => value.AgentId).Except(childAgentIds).Any() ||
             draft.ChildAgentPins.Any(value => value.AgentVersionId == Guid.Empty)))
        {
            return Failed<IReadOnlyList<AgentChildBindingSnapshot>>(
                "Imported child Agent pins must match unique child Agent identities.");
        }
        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot> pins = draft.ChildAgentPins
            .ToDictionary(value => value.AgentId);

        var resolved = new List<AgentChildBindingSnapshot>(childAgentIds.Count);
        foreach (Guid childAgentId in childAgentIds)
        {
            AgentDefinition? child = await GetDefinitionAsync(childAgentId, cancellationToken);
            if (child is null ||
                child.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                child.PublishedVersions.Count == 0)
            {
                return Failed<IReadOnlyList<AgentChildBindingSnapshot>>(
                    "Child Agent bindings must reference enabled published Agents.");
            }

            Guid versionId = pins.TryGetValue(childAgentId, out AgentChildBindingSnapshot? pin)
                ? pin.AgentVersionId
                : child.PublishedVersions[^1].Id;
            AgentVersion? selectedVersion = child.PublishedVersions
                .FirstOrDefault(version => version.Id == versionId);
            if (selectedVersion is null)
            {
                return Failed<IReadOnlyList<AgentChildBindingSnapshot>>(
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

        return Success<IReadOnlyList<AgentChildBindingSnapshot>>(
            AgentContractCloner.ReadOnly(resolved));
    }

    private async Task<ServiceResult<IReadOnlyList<AgentOrchestrationBindingSnapshot>>> ResolveOrchestrationBindingsAsync(
        AgentVersion draft,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> orchestrationIds = draft.OrchestrationIds;
        if (orchestrationIds.Count != orchestrationIds.Distinct().Count() ||
            orchestrationIds.Any(id => id == Guid.Empty) ||
            (orchestrationIds.Count > 0 && _orchestrationCatalog is null))
        {
            return Failed<IReadOnlyList<AgentOrchestrationBindingSnapshot>>(
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
            return Failed<IReadOnlyList<AgentOrchestrationBindingSnapshot>>(
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
                return Failed<IReadOnlyList<AgentOrchestrationBindingSnapshot>>(
                    "Orchestration bindings must reference enabled published orchestrations.");
            }
            resolved.Add(new AgentOrchestrationBindingSnapshot(orchestrationId, selected.OrchestrationVersionId));
        }

        return Success<IReadOnlyList<AgentOrchestrationBindingSnapshot>>(
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

    #endregion

    /// <summary>Agent 导入导出包的格式标识。</summary>
    public const string AgentPackageFormatIdentifier = "eu.core.agent-package";
    /// <summary>当前支持的 Agent 包格式版本。</summary>
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

    #region Agent 包导入导出

    public async Task<ServiceResult<string>> ExportAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        AgentDefinition? definition = await GetDefinitionAsync(agentId, cancellationToken);
        if (definition is null)
        {
            return Failed<string>("The Agent was not found.");
        }

        string? bindingError = await ValidateDraftChildReferencesAsync(
            definition.Draft.ChildAgentIds, definition.Draft.ChildAgentPins, cancellationToken);
        if (bindingError is not null)
        {
            return Failed<string>(bindingError);
        }

        bindingError = await ValidateDraftOrchestrationReferencesAsync(
            definition.Draft.OrchestrationIds, definition.Draft.OrchestrationPins, cancellationToken);
        if (bindingError is not null)
        {
            return Failed<string>(bindingError);
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
        if (!TryReadPackage(json, out AgentPackageV1? verifiedPackage, out string? safetyError))
        {
            return Failed<string>(safetyError!);
        }

        if (!TryValidatePackage(verifiedPackage!, out _, out _, out string? contractError))
        {
            return Failed<string>(contractError!);
        }

        string? referenceError = await ValidatePackageReferencesAsync(
            verifiedPackage!, cancellationToken);
        return referenceError is null
            ? Success(data: json)
            : Failed<string>(referenceError);
    }

    public async Task<ServiceResult<AgentDefinition>> ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        EnsureAgentManagementAvailable();
        if (!TryReadPackage(json, out AgentPackageV1? package, out string? error))
        {
            return Failed<AgentDefinition>(error!);
        }

        if (!TryValidatePackage(
                package!,
                out AgentRuntimeStatus runtimeStatus,
                out AgentOutputMode outputMode,
                out error))
        {
            return Failed<AgentDefinition>(error!);
        }

        string? referenceError = await ValidatePackageReferencesAsync(
            package!, cancellationToken);
        if (referenceError is not null)
        {
            return Failed<AgentDefinition>(referenceError);
        }

        ServiceResult<AgentDefinition> result = await CreateImportedAsync(
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

        return result;
    }

    private async Task<string?> ValidatePackageReferencesAsync(AgentPackageV1 package, CancellationToken cancellationToken)
    {
        string? error = await ValidateModelReferenceAsync(
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

    private async Task<string?> ValidateModelReferenceAsync(string modelProfileId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(modelProfileId) &&
            !await ModelProfiles.ExistsAsync(modelProfileId, cancellationToken))
        {
            return "The package references a model profile that is not available.";
        }

        return null;
    }

    private async Task<string?> ValidateToolReferencesAsync(IReadOnlyList<string> references, CancellationToken cancellationToken)
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
            return "The package references an MCP tool version that is not available.";
        }

        return null;
    }

    private async Task<string?> ValidateSkillReferencesAsync(IReadOnlyList<string> references, CancellationToken cancellationToken)
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
            return "The package references a Skill version that is not published.";
        }

        return null;
    }

    private async Task<string?> ValidateKnowledgeReferencesAsync(IReadOnlyList<string> references, CancellationToken cancellationToken)
    {
        IReadOnlySet<Guid> available = _knowledgeBases is null
            ? new HashSet<Guid>()
            : (await _knowledgeBases.ListPublishedAsync(cancellationToken))
                .Select(value => value.KnowledgeBaseId)
                .ToHashSet();
        foreach (string reference in references)
        {
            if (!Guid.TryParseExact(reference, "D", out Guid id) || !available.Contains(id))
            {
                return "The package references a knowledge base that is not enabled and indexed.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateDraftChildReferencesAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentChildBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        if (pins.Count > 0 && pins.Select(value => value.AgentId).Distinct().Count() != pins.Count)
        {
            return "The package child Agent pins contain duplicate identities.";
        }

        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot>? byId = pins.Count == 0
            ? null
            : pins.ToDictionary(value => value.AgentId);
        if (byId is not null && (byId.Count != ids.Count || byId.Keys.Except(ids).Any()))
        {
            return "The package child Agent pins do not match its identities.";
        }

        foreach (Guid id in ids)
        {
            AgentDefinition? agent = await GetDefinitionAsync(id, cancellationToken);
            Guid versionId = byId?.TryGetValue(id, out AgentChildBindingSnapshot? pin) is true
                ? pin.AgentVersionId
                : agent?.PublishedVersions.LastOrDefault()?.Id ?? Guid.Empty;
            if (agent is null ||
                agent.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                !agent.PublishedVersions.Any(value => value.Id == versionId))
            {
                return "The package references an enabled published child Agent that is not available.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateDraftOrchestrationReferencesAsync(
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
            return "The package orchestration pins contain duplicate identities.";
        }

        IReadOnlyDictionary<Guid, AgentOrchestrationBindingSnapshot>? byId = pins.Count == 0
            ? null
            : pins.ToDictionary(value => value.OrchestrationId);
        if (byId is not null && (byId.Count != ids.Count || byId.Keys.Except(ids).Any()))
        {
            return "The package orchestration pins do not match its identities.";
        }

        return ids.Any(id =>
            (byId?.TryGetValue(id, out AgentOrchestrationBindingSnapshot? pin) is true
                ? values.SingleOrDefault(value =>
                    value.OrchestrationId == id &&
                    value.OrchestrationVersionId == pin.OrchestrationVersionId)
                : values.LastOrDefault(value => value.OrchestrationId == id)) is not { Enabled: true })
            ? "The package references an enabled published orchestration that is not available."
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

        var result = new List<AgentPackageChildBindingV1>(ids.Count);
        foreach (Guid id in ids)
        {
            AgentDefinition agent = (await GetDefinitionAsync(id, cancellationToken))!;
            result.Add(new AgentPackageChildBindingV1(
                id.ToString("D"),
                agent.PublishedVersions[^1].Id.ToString("D")));
        }

        return AgentContractCloner.ReadOnly(result);
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

    private async Task<string?> ValidateChildBindingReferencesAsync(
        IReadOnlyList<AgentPackageChildBindingV1> references,
        CancellationToken cancellationToken)
    {
        foreach (AgentPackageChildBindingV1 reference in references)
        {
            if (!Guid.TryParseExact(reference.AgentId, "D", out Guid id) ||
                !Guid.TryParseExact(reference.AgentVersionId, "D", out Guid versionId))
            {
                return "The package references an invalid child Agent version.";
            }

            AgentDefinition? agent = await GetDefinitionAsync(id, cancellationToken);
            if (agent is null ||
                agent.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                !agent.PublishedVersions.Any(value => value.Id == versionId))
            {
                return "The package references a child Agent version that is not available.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateOrchestrationBindingReferencesAsync(
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
                return "The package references an orchestration version that is not available.";
            }
        }

        return null;
    }

    private static bool TryReadPackage(string? json, out AgentPackageV1? package, out string? error)
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
        out string? error)
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
            error = "The Agent package major version is not supported.";
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
                error = schema.Error!;
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

    private static string PackageInvalid(string message) => message;

    #endregion
}

using EU.Core.IServices.Orchestration;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：AgOrchestrationDefinitionServices 职责实现

/// <summary>
/// 编排定义、版本、节点、连线和发布绑定的规范化持久化服务。
/// </summary>
public sealed class AgOrchestrationDefinitionServices :
    BaseServices<AgOrchestrationDefinition>,
    IAgOrchestrationDefinitionServices,
    IOrchestrationRepository,
    IPublishedOrchestrationCatalog
{
    public AgOrchestrationDefinitionServices(IBaseRepository<AgOrchestrationDefinition> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<OrchestrationDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgOrchestrationDefinition? definition = await Db.Queryable<AgOrchestrationDefinition>()
            .Where(value => value.ID == id && !value.IsDeleted)
            .FirstAsync();
        return definition is null
            ? null
            : await LoadDefinitionAsync(definition, cancellationToken);
    }

    public async Task<IReadOnlyList<OrchestrationDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgOrchestrationDefinition> definitions = await Db.Queryable<AgOrchestrationDefinition>()
            .Where(value => !value.IsDeleted)
            .OrderBy(value => value.Code)
            .OrderBy(value => value.ID)
            .ToListAsync();
        return await LoadDefinitionsAsync(definitions, cancellationToken);
    }

    public async Task<IReadOnlyList<PublishedOrchestrationReference>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await Db
            .Queryable<AgOrchestrationDefinition, AgOrchestrationVersion>(
                (definition, version) => new JoinQueryInfos(
                    JoinType.Inner,
                    definition.ID == version.OrchestrationId))
            .Where((definition, version) =>
                !definition.IsDeleted &&
                definition.Status != nameof(OrchestrationStatus.Archived) &&
                !version.IsDeleted &&
                version.IsDraft == false)
            .OrderBy((definition, version) => definition.Code)
            .OrderBy((definition, version) => version.Ordinal)
            .Select((definition, version) => new PublishedReferenceRow
            {
                OrchestrationId = definition.ID,
                OrchestrationVersionId = version.ID,
                Status = definition.Status
            })
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return OrchestrationContractCloner.ReadOnly(rows.Select(value =>
            new PublishedOrchestrationReference(
                value.OrchestrationId,
                value.OrchestrationVersionId,
                ParseStatus(value.Status) is OrchestrationStatus.Enabled)));
    }

    public async Task<bool> TryCreateAsync(OrchestrationDefinition value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool exists = await Db.Queryable<AgOrchestrationDefinition>()
                .Where(candidate =>
                    !candidate.IsDeleted &&
                    (candidate.ID == value.Id || candidate.Code == value.Code))
                .AnyAsync();
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapDefinitionEntity(value)).ExecuteCommandAsync();
            await InsertVersionAsync(value.Id, value.Code, value.Draft, 0, cancellationToken);
            for (int index = 0; index < value.PublishedVersions.Count; index++)
            {
                await InsertVersionAsync(
                    value.Id,
                    value.Code,
                    value.PublishedVersions[index],
                    index + 1,
                    cancellationToken);
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TryReplaceAsync(OrchestrationDefinition value, long expectedRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedRevision == long.MaxValue || value.LogicalRevision != expectedRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            List<AgOrchestrationVersion> existingVersions = await Db.Queryable<AgOrchestrationVersion>()
                .Where(candidate => candidate.OrchestrationId == value.Id && !candidate.IsDeleted)
                .OrderBy(candidate => candidate.Ordinal)
                .OrderBy(candidate => candidate.ID)
                .ToListAsync();
            AgOrchestrationVersion? existingDraft = existingVersions.SingleOrDefault(
                candidate => candidate.IsDraft == true);
            HashSet<Guid> existingPublishedIds = existingVersions
                .Where(candidate => candidate.IsDraft == false)
                .Select(candidate => candidate.ID)
                .ToHashSet();
            HashSet<Guid> requestedPublishedIds = value.PublishedVersions
                .Select(version => version.Id)
                .ToHashSet();
            if (existingDraft?.ID != value.Draft.Id ||
                !existingPublishedIds.IsSubsetOf(requestedPublishedIds) ||
                value.PublishedVersions.Count != requestedPublishedIds.Count)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            AgOrchestrationDefinition entity = MapDefinitionEntity(value);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(candidate => new
                {
                    candidate.Name,
                    candidate.Description,
                    candidate.Status,
                    candidate.LogicalRevision
                })
                .Where(candidate =>
                    candidate.ID == value.Id &&
                    candidate.Code == value.Code &&
                    candidate.LogicalRevision == expectedRevision &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            int draftUpdated = await Db.Updateable(MapVersionEntity(value.Id, value.Draft, 0))
                .UpdateColumns(candidate => new
                {
                    candidate.Ordinal,
                    candidate.Label,
                    candidate.IsDraft,
                    candidate.StartNodeId
                })
                .Where(candidate =>
                    candidate.ID == value.Draft.Id &&
                    candidate.OrchestrationId == value.Id &&
                    candidate.IsDraft == true &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (draftUpdated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await DeleteVersionChildrenAsync(value.Draft.Id);
            await InsertVersionChildrenAsync(
                value.Id,
                value.Code,
                value.Draft,
                cancellationToken);

            for (int index = 0; index < value.PublishedVersions.Count; index++)
            {
                OrchestrationVersion version = value.PublishedVersions[index];
                if (!existingPublishedIds.Contains(version.Id))
                {
                    await InsertVersionAsync(
                        value.Id,
                        value.Code,
                        version,
                        index + 1,
                        cancellationToken);
                }
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task<OrchestrationDefinition> LoadDefinitionAsync(AgOrchestrationDefinition definition, CancellationToken cancellationToken)
    {
        IReadOnlyList<OrchestrationDefinition> values = await LoadDefinitionsAsync(
            [definition],
            cancellationToken);
        return values[0];
    }

    private async Task<IReadOnlyList<OrchestrationDefinition>> LoadDefinitionsAsync(
        IReadOnlyList<AgOrchestrationDefinition> definitions,
        CancellationToken cancellationToken)
    {
        if (definitions.Count == 0)
        {
            return [];
        }

        Guid[] orchestrationIds = definitions.Select(value => value.ID).ToArray();
        List<AgOrchestrationVersion> versions = await Db.Queryable<AgOrchestrationVersion>()
            .Where(value =>
                value.OrchestrationId.HasValue &&
                orchestrationIds.Contains(value.OrchestrationId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.OrchestrationId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        Guid[] versionIds = versions.Select(value => value.ID).ToArray();
        List<AgOrchestrationNode> nodes = versionIds.Length == 0
            ? []
            : await Db.Queryable<AgOrchestrationNode>()
                .Where(value =>
                    value.VersionId.HasValue &&
                    versionIds.Contains(value.VersionId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.VersionId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        List<AgOrchestrationEdge> edges = versionIds.Length == 0
            ? []
            : await Db.Queryable<AgOrchestrationEdge>()
                .Where(value =>
                    value.VersionId.HasValue &&
                    versionIds.Contains(value.VersionId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.VersionId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        List<AgOrchestrationAgentBinding> bindings = versionIds.Length == 0
            ? []
            : await Db.Queryable<AgOrchestrationAgentBinding>()
                .Where(value =>
                    value.VersionId.HasValue &&
                    versionIds.Contains(value.VersionId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.VersionId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgOrchestrationNode[]> nodesByVersion = nodes
            .GroupBy(value => Required(value.VersionId, "Node.VersionId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgOrchestrationEdge[]> edgesByVersion = edges
            .GroupBy(value => Required(value.VersionId, "Edge.VersionId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgOrchestrationAgentBinding[]> bindingsByVersion = bindings
            .GroupBy(value => Required(value.VersionId, "AgentBinding.VersionId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgOrchestrationVersion[]> versionsByDefinition = versions
            .GroupBy(value => Required(value.OrchestrationId, "Version.OrchestrationId"))
            .ToDictionary(group => group.Key, group => group.ToArray());

        return OrchestrationContractCloner.ReadOnly(definitions.Select(definition => MapDefinition(
            definition,
            versionsByDefinition.GetValueOrDefault(definition.ID) ?? [],
            nodesByVersion,
            edgesByVersion,
            bindingsByVersion)));
    }

    private static OrchestrationDefinition MapDefinition(
        AgOrchestrationDefinition definition,
        IReadOnlyList<AgOrchestrationVersion> versions,
        IReadOnlyDictionary<Guid, AgOrchestrationNode[]> nodesByVersion,
        IReadOnlyDictionary<Guid, AgOrchestrationEdge[]> edgesByVersion,
        IReadOnlyDictionary<Guid, AgOrchestrationAgentBinding[]> bindingsByVersion)
    {
        AgOrchestrationVersion draftEntity = versions.SingleOrDefault(value => value.IsDraft == true)
            ?? throw new InvalidDataException(
                $"Orchestration '{definition.Code}' does not have exactly one draft version.");
        OrchestrationVersion Map(AgOrchestrationVersion version)
        {
            OrchestrationNode[] mappedNodes = (nodesByVersion.GetValueOrDefault(version.ID) ?? [])
                .OrderBy(value => Required(value.Ordinal, "Node.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(MapNode)
                .ToArray();
            OrchestrationEdge[] mappedEdges = (edgesByVersion.GetValueOrDefault(version.ID) ?? [])
                .OrderBy(value => Required(value.Ordinal, "Edge.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(MapEdge)
                .ToArray();
            bool isDraft = Required(version.IsDraft, "Version.IsDraft");
            string startNodeId = Required(version.StartNodeId, "Version.StartNodeId");
            OrchestrationVersionSnapshot? snapshot = isDraft
                ? null
                : new OrchestrationVersionSnapshot(
                    version.ID,
                    Required(definition.Code, "Code"),
                    startNodeId,
                    OrchestrationContractCloner.ReadOnly(mappedNodes),
                    OrchestrationContractCloner.ReadOnly(mappedEdges),
                    OrchestrationContractCloner.ReadOnly(
                        (bindingsByVersion.GetValueOrDefault(version.ID) ?? [])
                            .OrderBy(value => Required(value.Ordinal, "AgentBinding.Ordinal"))
                            .ThenBy(value => value.ID)
                            .Select(MapBinding)));
            return new OrchestrationVersion(
                version.ID,
                Required(version.Label, "Version.Label"),
                isDraft,
                startNodeId,
                OrchestrationContractCloner.ReadOnly(mappedNodes),
                OrchestrationContractCloner.ReadOnly(mappedEdges),
                snapshot);
        }

        return new OrchestrationDefinition(
            definition.ID,
            Required(definition.Code, "Code"),
            Required(definition.Name, "Name"),
            Required(definition.Description, "Description"),
            ParseStatus(definition.Status),
            Required(definition.LogicalRevision, "LogicalRevision"),
            Map(draftEntity),
            OrchestrationContractCloner.ReadOnly(versions
                .Where(value => value.IsDraft == false)
                .OrderBy(value => Required(value.Ordinal, "Version.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(Map)));
    }

    private async Task InsertVersionAsync(
        Guid orchestrationId,
        string orchestrationCode,
        OrchestrationVersion version,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await Db.Insertable(MapVersionEntity(orchestrationId, version, ordinal))
            .ExecuteCommandAsync();
        await InsertVersionChildrenAsync(
            orchestrationId,
            orchestrationCode,
            version,
            cancellationToken);
    }

    private async Task InsertVersionChildrenAsync(
        Guid orchestrationId,
        string orchestrationCode,
        OrchestrationVersion version,
        CancellationToken cancellationToken)
    {
        List<AgOrchestrationNode> nodes = version.Nodes
            .Select((value, ordinal) => MapNodeEntity(orchestrationId, version.Id, value, ordinal))
            .ToList();
        if (nodes.Count > 0)
        {
            await Db.Insertable(nodes).ExecuteCommandAsync();
        }

        List<AgOrchestrationEdge> edges = version.Edges
            .Select((value, ordinal) => MapEdgeEntity(orchestrationId, version.Id, value, ordinal))
            .ToList();
        if (edges.Count > 0)
        {
            await Db.Insertable(edges).ExecuteCommandAsync();
        }

        IReadOnlyList<OrchestrationAgentBinding> sourceBindings = version.Snapshot?.Agents ?? [];
        if (version.Snapshot is not null &&
            (!string.Equals(version.Snapshot.OrchestrationCode, orchestrationCode, StringComparison.Ordinal) ||
             version.Snapshot.VersionId != version.Id ||
             !string.Equals(version.Snapshot.StartNodeId, version.StartNodeId, StringComparison.Ordinal) ||
             !version.Snapshot.Nodes.SequenceEqual(version.Nodes) ||
             !version.Snapshot.Edges.SequenceEqual(version.Edges)))
        {
            throw new InvalidDataException("The orchestration version snapshot does not match its published graph.");
        }

        List<AgOrchestrationAgentBinding> bindings = sourceBindings
            .Select((value, ordinal) => MapBindingEntity(orchestrationId, version.Id, value, ordinal))
            .ToList();
        if (bindings.Count > 0)
        {
            await Db.Insertable(bindings).ExecuteCommandAsync();
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task DeleteVersionChildrenAsync(Guid versionId)
    {
        await Db.Deleteable<AgOrchestrationAgentBinding>()
            .Where(value => value.VersionId == versionId)
            .ExecuteCommandAsync();
        await Db.Deleteable<AgOrchestrationEdge>()
            .Where(value => value.VersionId == versionId)
            .ExecuteCommandAsync();
        await Db.Deleteable<AgOrchestrationNode>()
            .Where(value => value.VersionId == versionId)
            .ExecuteCommandAsync();
    }

    private static AgOrchestrationDefinition MapDefinitionEntity(OrchestrationDefinition value) =>
        new()
        {
            ID = value.Id,
            Code = value.Code,
            Name = value.Name,
            Description = value.Description,
            Status = value.Status.ToString(),
            LogicalRevision = value.LogicalRevision,
            IsDeleted = false,
            IsActive = true
        };

    private static AgOrchestrationVersion MapVersionEntity(Guid orchestrationId, OrchestrationVersion value, int ordinal) =>
        new()
        {
            ID = value.Id,
            OrchestrationId = orchestrationId,
            Ordinal = ordinal,
            Label = value.Label,
            IsDraft = value.IsDraft,
            StartNodeId = value.StartNodeId,
            IsDeleted = false,
            IsActive = true
        };

    private static OrchestrationNode MapNode(AgOrchestrationNode value) =>
        new(
            Required(value.NodeId, "Node.NodeId"),
            Required(value.Name, "Node.Name"),
            Required(value.AgentId, "Node.AgentId"),
            ParseInputMode(value.InputMode),
            Required(value.InputTemplate, "Node.InputTemplate"),
            Required(value.MaximumRetries, "Node.MaximumRetries"),
            Required(value.TimeoutSeconds, "Node.TimeoutSeconds"));

    private static AgOrchestrationNode MapNodeEntity(Guid orchestrationId, Guid versionId, OrchestrationNode value, int ordinal) =>
        new()
        {
            ID = Guid.NewGuid(),
            OrchestrationId = orchestrationId,
            VersionId = versionId,
            Ordinal = ordinal,
            NodeId = value.Id,
            Name = value.Name,
            AgentId = value.AgentId,
            InputMode = value.InputMode.ToString(),
            InputTemplate = value.InputTemplate,
            MaximumRetries = value.MaximumRetries,
            TimeoutSeconds = value.TimeoutSeconds,
            IsDeleted = false,
            IsActive = true
        };

    private static OrchestrationEdge MapEdge(AgOrchestrationEdge value) =>
        new(
            Required(value.FromNodeId, "Edge.FromNodeId"),
            Required(value.ToNodeId, "Edge.ToNodeId"),
            ParseEdgeCondition(value.Condition),
            Required(value.ConditionValue, "Edge.ConditionValue"),
            Required(value.SortOrder, "Edge.SortOrder"));

    private static AgOrchestrationEdge MapEdgeEntity(Guid orchestrationId, Guid versionId, OrchestrationEdge value, int ordinal) =>
        new()
        {
            ID = Guid.NewGuid(),
            OrchestrationId = orchestrationId,
            VersionId = versionId,
            Ordinal = ordinal,
            FromNodeId = value.FromNodeId,
            ToNodeId = value.ToNodeId,
            Condition = value.Condition.ToString(),
            ConditionValue = value.ConditionValue,
            SortOrder = value.Order,
            IsDeleted = false,
            IsActive = true
        };

    private static OrchestrationAgentBinding MapBinding(AgOrchestrationAgentBinding value) =>
        new(
            Required(value.AgentId, "AgentBinding.AgentId"),
            Required(value.AgentVersionId, "AgentBinding.AgentVersionId"));

    private static AgOrchestrationAgentBinding MapBindingEntity(Guid orchestrationId, Guid versionId, OrchestrationAgentBinding value, int ordinal) =>
        new()
        {
            ID = Guid.NewGuid(),
            OrchestrationId = orchestrationId,
            VersionId = versionId,
            Ordinal = ordinal,
            AgentId = value.AgentId,
            AgentVersionId = value.AgentVersionId,
            IsDeleted = false,
            IsActive = true
        };

    private static OrchestrationStatus ParseStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out OrchestrationStatus result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"Orchestration Status contains unsupported value '{value}'.");

    private static OrchestrationNodeInputMode ParseInputMode(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out OrchestrationNodeInputMode result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"Orchestration InputMode contains unsupported value '{value}'.");

    private static OrchestrationEdgeCondition ParseEdgeCondition(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out OrchestrationEdgeCondition result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"Orchestration Condition contains unsupported value '{value}'.");

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Orchestration field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Orchestration field '{field}' is missing.");

    /// <summary>
    /// 已发布编排版本联表查询的内部投影行。
    /// </summary>
    private sealed class PublishedReferenceRow
    {
        /// <summary>
        /// 编排定义标识。
        /// </summary>
        public Guid OrchestrationId { get; set; }

        /// <summary>
        /// 已发布编排版本标识。
        /// </summary>
        public Guid OrchestrationVersionId { get; set; }

        /// <summary>
        /// 编排定义的当前运行状态。
        /// </summary>
        public string? Status { get; set; }
    }
}

#endregion

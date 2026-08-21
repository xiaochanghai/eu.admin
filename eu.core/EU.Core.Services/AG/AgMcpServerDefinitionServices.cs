using EU.Core.IServices.Mcp;
using System.Security.Cryptography;
using System.Text.Json;

#nullable enable

namespace EU.Core.Services;

public sealed class AgMcpServerDefinitionServices :
    BaseServices<AgMcpServerDefinition>,
    IAgMcpServerDefinitionServices,
    IMcpServerDefinitionCatalog,
    IPublishedMcpToolCatalog
{
    private const int MaximumTools = 256;
    private readonly IMcpToolDiscovery _discovery;

    public AgMcpServerDefinitionServices(
        IBaseRepository<AgMcpServerDefinition> dal,
        IMcpToolDiscovery discovery)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
    }

    public async Task<ServiceResult<McpServerDefinition>> CreateAsync(
        CreateMcpServerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeCode(command.Code, out string? code))
        {
            return Failure(McpErrorCodes.CodeInvalid, "MCP Server code must normalize to lowercase kebab-case.");
        }

        ServiceResult<McpServerDefinition>? configurationError = ValidateConfiguration(
            command.Transport,
            command.Endpoint,
            command.Command,
            command.Arguments,
            command.CredentialAlias);
        if (configurationError is not null)
        {
            return configurationError;
        }

        var definition = new McpServerDefinition(
            Guid.NewGuid(),
            code!,
            command.Name ?? string.Empty,
            command.Description ?? string.Empty,
            command.Transport,
            command.Endpoint?.Trim() ?? string.Empty,
            command.Command?.Trim() ?? string.Empty,
            McpContractCloner.ReadOnly(command.Arguments ?? []),
            command.CredentialAlias?.Trim() ?? string.Empty,
            command.Enabled,
            0,
            command.Enabled ? McpServerStatus.NotSynced : McpServerStatus.Disabled,
            string.Empty,
            null,
            McpContractCloner.ReadOnly(Array.Empty<Guid>()),
            McpContractCloner.ReadOnly(Array.Empty<McpToolVersion>()));
        return await TryCreateDefinitionAsync(definition, cancellationToken)
            ? ServiceResult<McpServerDefinition>.OprateSuccess(definition)
            : Failure(McpErrorCodes.CodeConflict, "An MCP Server already uses this code.");
    }

    public Task<McpServerDefinition?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        GetDefinitionAsync(id, cancellationToken);

    public Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
        CancellationToken cancellationToken = default) =>
        QueryDefinitionsAsync(query, cancellationToken);

    public async Task<ServiceResult<McpServerDefinition>> UpdateAsync(
        UpdateMcpServerCommand command,
        CancellationToken cancellationToken = default)
    {
        McpServerDefinition? existing =
            await GetDefinitionAsync(command.ServerId, cancellationToken);
        if (existing is null)
        {
            return Failure(McpErrorCodes.NotFound, "The MCP Server was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before this operation completed.");
        }

        if (existing.Status is McpServerStatus.Archived)
        {
            return Failure(
                McpErrorCodes.LifecycleTransitionInvalid,
                "An archived MCP Server must be restored before its configuration can be edited.");
        }

        ServiceResult<McpServerDefinition>? configurationError = ValidateConfiguration(
            command.Transport,
            command.Endpoint,
            command.Command,
            command.Arguments,
            command.CredentialAlias);
        if (configurationError is not null)
        {
            return configurationError;
        }

        if (existing.Enabled && !command.Enabled)
        {
            var serverToolIds = existing.ToolVersions
                .Select(value => value.Id)
                .ToHashSet();
            string[] blockers = await FindAgentReferenceBlockersAsync(
                serverToolIds,
                cancellationToken);
            if (blockers.Length > 0)
            {
                return Failure(
                    McpErrorCodes.DisableBlocked,
                    $"The MCP Server is still referenced by Agent(s): {string.Join(", ", blockers)}.");
            }
        }

        bool connectionChanged =
            existing.Transport != command.Transport ||
            !string.Equals(existing.Endpoint, command.Endpoint?.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existing.Command, command.Command?.Trim(), StringComparison.Ordinal) ||
            !existing.Arguments.SequenceEqual(command.Arguments ?? [], StringComparer.Ordinal) ||
            !string.Equals(existing.CredentialAlias, command.CredentialAlias?.Trim(), StringComparison.Ordinal);
        bool reEnabled = !existing.Enabled && command.Enabled;
        var updated = existing with
        {
            Name = command.Name ?? string.Empty,
            Description = command.Description ?? string.Empty,
            Transport = command.Transport,
            Endpoint = command.Endpoint?.Trim() ?? string.Empty,
            Command = command.Command?.Trim() ?? string.Empty,
            Arguments = McpContractCloner.ReadOnly(command.Arguments ?? []),
            CredentialAlias = command.CredentialAlias?.Trim() ?? string.Empty,
            Enabled = command.Enabled,
            LogicalRevision = existing.LogicalRevision + 1,
            Status = !command.Enabled
                ? McpServerStatus.Disabled
                : connectionChanged || reEnabled ? McpServerStatus.NotSynced : existing.Status,
            LastError = connectionChanged || reEnabled ? string.Empty : existing.LastError
        };
        return await TryReplaceDefinitionAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? ServiceResult<McpServerDefinition>.OprateSuccess(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before this operation completed.");
    }

    public async Task<ServiceResult<McpServerDefinition>> SyncAsync(
        SyncMcpServerCommand command,
        CancellationToken cancellationToken = default)
    {
        McpServerDefinition? existing =
            await GetDefinitionAsync(command.ServerId, cancellationToken);
        if (existing is null)
        {
            return Failure(McpErrorCodes.NotFound, "The MCP Server was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before this operation completed.");
        }

        if (existing.Status is McpServerStatus.Archived)
        {
            return Failure(
                McpErrorCodes.LifecycleTransitionInvalid,
                "An archived MCP Server must be restored before it can be synchronized.");
        }

        if (!existing.Enabled)
        {
            return Failure(McpErrorCodes.ConfigurationInvalid, "A disabled MCP Server cannot be synchronized.");
        }

        IReadOnlyList<DiscoveredMcpTool> discovered;
        try
        {
            discovered = await _discovery.DiscoverAsync(existing, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            string reason = SanitizeFailure(exception);
            McpServerDefinition unhealthy = existing with
            {
                LogicalRevision = existing.LogicalRevision + 1,
                Status = McpServerStatus.Unhealthy,
                LastError = reason,
                LastSyncedAtUtc = DateTimeOffset.UtcNow
            };
            await TryReplaceDefinitionAsync(
                unhealthy,
                command.ExpectedLogicalRevision,
                cancellationToken);
            return Failure(McpErrorCodes.DiscoveryFailed, reason);
        }

        ServiceResult<McpServerDefinition>? discoveryError = ValidateDiscoveredTools(discovered);
        if (discoveryError is not null)
        {
            return discoveryError;
        }

        DateTimeOffset synchronizedAt = DateTimeOffset.UtcNow;
        var versions = existing.ToolVersions.ToList();
        var currentIds = new List<Guid>();
        foreach (DiscoveredMcpTool tool in discovered.OrderBy(tool => tool.Name, StringComparer.Ordinal))
        {
            string canonicalSchema = CanonicalizeJson(tool.InputSchemaJson);
            string hash = Hash(tool.Name, tool.Description ?? string.Empty, canonicalSchema, McpToolRisk.Unknown);
            McpToolVersion? version = existing.CurrentToolVersionIds
                .Select(id => versions.Single(candidate => candidate.Id == id))
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, tool.Name, StringComparison.Ordinal) &&
                    string.Equals(candidate.Description, tool.Description ?? string.Empty, StringComparison.Ordinal) &&
                    string.Equals(candidate.InputSchemaJson, canonicalSchema, StringComparison.Ordinal));
            version ??= versions.LastOrDefault(candidate =>
                string.Equals(candidate.Name, tool.Name, StringComparison.Ordinal) &&
                string.Equals(candidate.Sha256, hash, StringComparison.Ordinal));
            if (version is null)
            {
                version = new McpToolVersion(
                    Guid.NewGuid(),
                    existing.Id,
                    tool.Name,
                    tool.Description ?? string.Empty,
                    canonicalSchema,
                    McpToolRisk.Unknown,
                    hash,
                    synchronizedAt);
                versions.Add(version);
            }

            currentIds.Add(version.Id);
        }

        McpServerDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            Status = McpServerStatus.Healthy,
            LastError = string.Empty,
            LastSyncedAtUtc = synchronizedAt,
            CurrentToolVersionIds = McpContractCloner.ReadOnly(currentIds),
            ToolVersions = McpContractCloner.ReadOnly(versions)
        };
        return await TryReplaceDefinitionAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? ServiceResult<McpServerDefinition>.OprateSuccess(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before synchronization completed.");
    }

    public async Task<ServiceResult<McpServerDefinition>> ClassifyToolAsync(
        ClassifyMcpToolCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Risk) || command.Risk == McpToolRisk.Unknown)
        {
            return Failure(McpErrorCodes.RiskInvalid, "Tool risk must be ReadOnly, Mutating, or HighRisk.");
        }

        McpServerDefinition? existing =
            await GetDefinitionAsync(command.ServerId, cancellationToken);
        if (existing is null)
        {
            return Failure(McpErrorCodes.NotFound, "The MCP Server was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before this operation completed.");
        }

        if (existing.Status is McpServerStatus.Archived)
        {
            return Failure(
                McpErrorCodes.LifecycleTransitionInvalid,
                "An archived MCP Server must be restored before its tools can be classified.");
        }

        McpToolVersion? source = existing.ToolVersions.FirstOrDefault(
            version => version.Id == command.ToolVersionId &&
                       existing.CurrentToolVersionIds.Contains(version.Id));
        if (source is null)
        {
            return Failure(McpErrorCodes.ToolNotFound, "The current MCP tool version was not found.");
        }

        McpToolVersion classified = source with
        {
            Id = Guid.NewGuid(),
            Risk = command.Risk,
            Sha256 = Hash(
                source.Name,
                source.Description,
                source.InputSchemaJson,
                command.Risk),
            DiscoveredAtUtc = DateTimeOffset.UtcNow
        };
        var versions = existing.ToolVersions.Append(classified);
        var currentIds = existing.CurrentToolVersionIds
            .Select(id => id == source.Id ? classified.Id : id);
        McpServerDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            ToolVersions = McpContractCloner.ReadOnly(versions),
            CurrentToolVersionIds = McpContractCloner.ReadOnly(currentIds)
        };
        return await TryReplaceDefinitionAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? ServiceResult<McpServerDefinition>.OprateSuccess(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before classification completed.");
    }

    public async Task<ServiceResult<McpServerDefinition>> SetArchivedAsync(
        SetMcpServerArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        McpServerDefinition? existing =
            await GetDefinitionAsync(command.ServerId, cancellationToken);
        if (existing is null)
        {
            return Failure(McpErrorCodes.NotFound, "The MCP Server was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Failure(
                McpErrorCodes.RevisionConflict,
                "The MCP Server changed before this operation completed.");
        }

        if (command.Archived &&
            (existing.Enabled || existing.Status is not McpServerStatus.Disabled))
        {
            return Failure(
                McpErrorCodes.LifecycleTransitionInvalid,
                "An MCP Server must be disabled before it can be archived.");
        }

        if (command.Archived)
        {
            var serverToolIds = existing.ToolVersions
                .Select(value => value.Id)
                .ToHashSet();
            string[] blockers = await FindAgentReferenceBlockersAsync(
                serverToolIds,
                cancellationToken);
            if (blockers.Length > 0)
            {
                return Failure(
                    McpErrorCodes.ArchiveBlocked,
                    $"The MCP Server is still referenced by Agent(s): {string.Join(", ", blockers)}.");
            }
        }

        if (!command.Archived && existing.Status is not McpServerStatus.Archived)
        {
            return Failure(
                McpErrorCodes.LifecycleTransitionInvalid,
                "Only an archived MCP Server can be restored.");
        }

        McpServerDefinition updated = existing with
        {
            Enabled = false,
            Status = command.Archived
                ? McpServerStatus.Archived
                : McpServerStatus.Disabled,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await TryReplaceDefinitionAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? ServiceResult<McpServerDefinition>.OprateSuccess(updated)
            : Failure(
                McpErrorCodes.RevisionConflict,
                "The MCP Server changed before this operation completed.");
    }

    private async Task<string[]> FindAgentReferenceBlockersAsync(
        IReadOnlySet<Guid> serverToolIds,
        CancellationToken cancellationToken)
    {
        if (serverToolIds.Count == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<AgAgentDefinition> enabledAgents = await Db.Queryable<AgAgentDefinition>()
            .Where(value =>
                !value.IsDeleted &&
                value.RuntimeStatus == "Enabled")
            .OrderBy(value => value.Code)
            .OrderBy(value => value.ID)
            .ToListAsync();
        if (enabledAgents.Count == 0)
        {
            return [];
        }

        Guid[] agentIds = enabledAgents.Select(value => value.ID).ToArray();
        List<AgAgentVersion> publishedVersions = await Db.Queryable<AgAgentVersion>()
            .Where(value =>
                value.AgentId.HasValue &&
                agentIds.Contains(value.AgentId.Value) &&
                value.IsDraft != true &&
                !value.IsDeleted)
            .OrderBy(value => value.AgentId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        Dictionary<Guid, AgAgentVersion> latestVersions = publishedVersions
            .GroupBy(value => Required(value.AgentId, "AgentVersion.AgentId"))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(value => Required(value.Ordinal, "AgentVersion.Ordinal"))
                    .ThenBy(value => value.ID)
                    .Last());
        if (latestVersions.Count == 0)
        {
            return [];
        }

        Guid[] latestVersionIds = latestVersions.Values
            .Select(value => value.ID)
            .ToArray();
        Guid[] toolVersionIds = serverToolIds.ToArray();
        List<AgAgentVersionBinding> bindings = await Db
            .Queryable<AgAgentVersionBinding, AgAgentVersionSnapshot>(
                (binding, snapshot) => new JoinQueryInfos(
                    JoinType.Inner,
                    binding.VersionId == snapshot.VersionId))
            .Where((binding, snapshot) =>
                binding.VersionId.HasValue &&
                latestVersionIds.Contains(binding.VersionId.Value) &&
                binding.Scope == "Snapshot" &&
                binding.BindingType == "Tool" &&
                binding.ReferenceId.HasValue &&
                toolVersionIds.Contains(binding.ReferenceId.Value) &&
                !binding.IsDeleted &&
                !snapshot.IsDeleted)
            .Select((binding, snapshot) => binding)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        HashSet<Guid> blockingVersionIds = bindings
            .Select(value => Required(value.VersionId, "Binding.VersionId"))
            .ToHashSet();
        HashSet<Guid> blockingAgentIds = latestVersions
            .Where(value => blockingVersionIds.Contains(value.Value.ID))
            .Select(value => value.Key)
            .ToHashSet();
        return enabledAgents
            .Where(value => blockingAgentIds.Contains(value.ID))
            .Select(value => Required(value.Code, "Agent.Code"))
            .Take(8)
            .ToArray();
    }

    public async Task<bool> ExistsAsync(
        Guid toolVersionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Db.Queryable<AgMcpToolVersion, AgMcpServerDefinition>(
                (tool, server) => new JoinQueryInfos(
                    JoinType.Inner,
                    tool.ServerId == server.ID))
            .Where((tool, server) =>
                tool.ID == toolVersionId &&
                !tool.IsDeleted &&
                !server.IsDeleted &&
                server.Status != nameof(McpServerStatus.Archived) &&
                tool.Risk != nameof(McpToolRisk.Unknown))
            .AnyAsync();
    }

    async Task<IReadOnlyList<PublishedMcpToolReference>> IPublishedMcpToolCatalog.ListAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgMcpServerDefinition> servers = await Db.Queryable<AgMcpServerDefinition>()
            .Where(value =>
                !value.IsDeleted &&
                value.Enabled == true &&
                value.Status != nameof(McpServerStatus.Archived))
            .OrderBy(value => value.Code)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (servers.Count == 0)
        {
            return [];
        }

        Guid[] serverIds = servers.Select(value => value.ID).ToArray();
        List<AgMcpToolVersion> tools = await Db.Queryable<AgMcpToolVersion>()
            .Where(value =>
                value.ServerId.HasValue &&
                serverIds.Contains(value.ServerId.Value) &&
                value.CurrentOrdinal >= 0 &&
                !value.IsDeleted &&
                value.Risk != nameof(McpToolRisk.Unknown))
            .OrderBy(value => value.ServerId)
            .OrderBy(value => value.CurrentOrdinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgMcpToolVersion[]> toolsByServer = tools
            .GroupBy(value => Required(value.ServerId, "ToolVersion.ServerId"))
            .ToDictionary(group => group.Key, group => group
                .OrderBy(value => Required(value.CurrentOrdinal, "ToolVersion.CurrentOrdinal"))
                .ThenBy(value => value.ID)
                .ToArray());
        return McpContractCloner.ReadOnly(servers.SelectMany(server =>
            (toolsByServer.GetValueOrDefault(server.ID) ?? []).Select(tool =>
                new PublishedMcpToolReference(
                    server.ID,
                    Required(server.Code, "Code"),
                    Required(server.Name, "Name"),
                    tool.ID,
                    Required(tool.Name, "ToolVersion.Name"),
                    Required(tool.Description, "ToolVersion.Description"),
                    Required(tool.InputSchemaJson, "ToolVersion.InputSchemaJson"),
                    ParseRisk(tool.Risk),
                    Required(tool.Sha256, "ToolVersion.Sha256")))));
    }

    private async Task<McpServerDefinition?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgMcpServerDefinition? server = await Db.Queryable<AgMcpServerDefinition>()
            .Where(value => value.ID == id && !value.IsDeleted)
            .FirstAsync();
        if (server is null)
        {
            return null;
        }

        return await LoadDefinitionAsync(server, cancellationToken);
    }

    private async Task<IReadOnlyList<McpServerDefinition>> QueryDefinitionsAsync(
        McpServerQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        var expression = Db.Queryable<AgMcpServerDefinition>()
            .Where(value => !value.IsDeleted);
        if (query.Status.HasValue)
        {
            string status = query.Status.Value.ToString();
            expression = expression.Where(value => value.Status == status);
        }
        else
        {
            expression = expression.Where(value => value.Status != nameof(McpServerStatus.Archived));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim().ToLowerInvariant();
            expression = expression.Where(value =>
                SqlFunc.ToLower(value.Code).Contains(search) ||
                SqlFunc.ToLower(value.Name).Contains(search) ||
                SqlFunc.ToLower(value.Description).Contains(search));
        }

        List<AgMcpServerDefinition> servers = await expression
            .OrderBy(value => value.Code)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (servers.Count == 0)
        {
            return [];
        }

        Guid[] serverIds = servers.Select(value => value.ID).ToArray();
        List<AgMcpServerArgument> arguments = await Db.Queryable<AgMcpServerArgument>()
            .Where(value =>
                value.ServerId.HasValue &&
                serverIds.Contains(value.ServerId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.ServerId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        List<AgMcpToolVersion> tools = await Db.Queryable<AgMcpToolVersion>()
            .Where(value =>
                value.ServerId.HasValue &&
                serverIds.Contains(value.ServerId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.ServerId)
            .OrderBy(value => value.HistoryOrdinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgMcpServerArgument[]> argumentsByServer = arguments
            .GroupBy(value => Required(value.ServerId, "Argument.ServerId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgMcpToolVersion[]> toolsByServer = tools
            .GroupBy(value => Required(value.ServerId, "ToolVersion.ServerId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        return McpContractCloner.ReadOnly(servers.Select(server => MapDefinition(
            server,
            argumentsByServer.GetValueOrDefault(server.ID) ?? [],
            toolsByServer.GetValueOrDefault(server.ID) ?? [])));
    }

    private async Task<McpServerDefinition> LoadDefinitionAsync(
        AgMcpServerDefinition server,
        CancellationToken cancellationToken)
    {
        List<AgMcpServerArgument> arguments = await Db.Queryable<AgMcpServerArgument>()
            .Where(value => value.ServerId == server.ID && !value.IsDeleted)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        List<AgMcpToolVersion> tools = await Db.Queryable<AgMcpToolVersion>()
            .Where(value => value.ServerId == server.ID && !value.IsDeleted)
            .OrderBy(value => value.HistoryOrdinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return MapDefinition(server, arguments, tools);
    }

    private async Task<bool> TryCreateDefinitionAsync(
        McpServerDefinition definition,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool exists = await Db.Queryable<AgMcpServerDefinition>()
                .Where(value =>
                    !value.IsDeleted &&
                    (value.ID == definition.Id || value.Code == definition.Code))
                .AnyAsync();
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapDefinitionEntity(definition)).ExecuteCommandAsync();
            List<AgMcpServerArgument> arguments = MapArgumentEntities(definition);
            if (arguments.Count > 0)
            {
                await Db.Insertable(arguments).ExecuteCommandAsync();
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

    private async Task<bool> TryReplaceDefinitionAsync(
        McpServerDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken)
    {
        if (expectedLogicalRevision == long.MaxValue ||
            definition.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        McpServerDefinition? existing = await GetDefinitionAsync(definition.Id, cancellationToken);
        if (existing is null ||
            !string.Equals(existing.Code, definition.Code, StringComparison.Ordinal) ||
            !McpContractCloner.PreservesToolHistory(existing, definition))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgMcpServerDefinition entity = MapDefinitionEntity(definition);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(value => new
                {
                    value.Name,
                    value.Description,
                    value.Transport,
                    value.Endpoint,
                    value.Command,
                    value.CredentialAlias,
                    value.Enabled,
                    value.LogicalRevision,
                    value.Status,
                    value.LastError,
                    value.LastSyncedAtUtc
                })
                .Where(value =>
                    value.ID == definition.Id &&
                    value.Code == definition.Code &&
                    value.LogicalRevision == expectedLogicalRevision &&
                    !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Deleteable<AgMcpServerArgument>()
                .Where(value => value.ServerId == definition.Id)
                .ExecuteCommandAsync();
            List<AgMcpServerArgument> arguments = MapArgumentEntities(definition);
            if (arguments.Count > 0)
            {
                await Db.Insertable(arguments).ExecuteCommandAsync();
            }

            McpToolVersion[] appended = definition.ToolVersions
                .Skip(existing.ToolVersions.Count)
                .ToArray();
            if (appended.Length > 0)
            {
                List<AgMcpToolVersion> toolEntities = appended
                    .Select((tool, index) => MapToolEntity(
                        tool,
                        existing.ToolVersions.Count + index,
                        null))
                    .ToList();
                await Db.Insertable(toolEntities).ExecuteCommandAsync();
            }

            await Db.Updateable<AgMcpToolVersion>()
                .SetColumns(value => value.CurrentOrdinal == null)
                .Where(value => value.ServerId == definition.Id && !value.IsDeleted)
                .ExecuteCommandAsync();
            for (int ordinal = 0; ordinal < definition.CurrentToolVersionIds.Count; ordinal++)
            {
                Guid toolVersionId = definition.CurrentToolVersionIds[ordinal];
                int affected = await Db.Updateable<AgMcpToolVersion>()
                    .SetColumns(value => value.CurrentOrdinal == ordinal)
                    .Where(value =>
                        value.ID == toolVersionId &&
                        value.ServerId == definition.Id &&
                        !value.IsDeleted)
                    .ExecuteCommandAsync();
                if (affected != 1)
                {
                    await Db.Ado.RollbackTranAsync();
                    return false;
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

    private static AgMcpServerDefinition MapDefinitionEntity(McpServerDefinition definition) =>
        new()
        {
            ID = definition.Id,
            Code = definition.Code,
            Name = definition.Name,
            Description = definition.Description,
            Transport = definition.Transport.ToString(),
            Endpoint = definition.Endpoint,
            Command = definition.Command,
            CredentialAlias = definition.CredentialAlias,
            Enabled = definition.Enabled,
            LogicalRevision = definition.LogicalRevision,
            Status = definition.Status.ToString(),
            LastError = definition.LastError,
            LastSyncedAtUtc = definition.LastSyncedAtUtc?.UtcDateTime
        };

    private static List<AgMcpServerArgument> MapArgumentEntities(McpServerDefinition definition) =>
        definition.Arguments.Select((value, ordinal) => new AgMcpServerArgument
        {
            ID = Guid.NewGuid(),
            ServerId = definition.Id,
            Ordinal = ordinal,
            Value = value
        }).ToList();

    private static AgMcpToolVersion MapToolEntity(
        McpToolVersion tool,
        int historyOrdinal,
        int? currentOrdinal) =>
        new()
        {
            ID = tool.Id,
            ServerId = tool.ServerId,
            HistoryOrdinal = historyOrdinal,
            CurrentOrdinal = currentOrdinal,
            Name = tool.Name,
            Description = tool.Description,
            InputSchemaJson = tool.InputSchemaJson,
            Risk = tool.Risk.ToString(),
            Sha256 = tool.Sha256,
            DiscoveredAtUtc = tool.DiscoveredAtUtc.UtcDateTime
        };

    private static McpServerDefinition MapDefinition(
        AgMcpServerDefinition server,
        IReadOnlyList<AgMcpServerArgument> arguments,
        IReadOnlyList<AgMcpToolVersion> tools)
    {
        McpToolVersion[] mappedTools = tools
            .OrderBy(value => Required(value.HistoryOrdinal, "ToolVersion.HistoryOrdinal"))
            .Select(value => new McpToolVersion(
                value.ID,
                Required(value.ServerId, "ToolVersion.ServerId"),
                Required(value.Name, "ToolVersion.Name"),
                Required(value.Description, "ToolVersion.Description"),
                Required(value.InputSchemaJson, "ToolVersion.InputSchemaJson"),
                ParseRisk(value.Risk),
                Required(value.Sha256, "ToolVersion.Sha256"),
                ToDateTimeOffset(Required(value.DiscoveredAtUtc, "ToolVersion.DiscoveredAtUtc"))))
            .ToArray();
        Guid[] currentToolIds = tools
            .Where(value => value.CurrentOrdinal.HasValue)
            .OrderBy(value => value.CurrentOrdinal)
            .ThenBy(value => value.ID)
            .Select(value => value.ID)
            .ToArray();
        return new McpServerDefinition(
            server.ID,
            Required(server.Code, "Code"),
            Required(server.Name, "Name"),
            Required(server.Description, "Description"),
            ParseTransport(server.Transport),
            Required(server.Endpoint, "Endpoint"),
            Required(server.Command, "Command"),
            McpContractCloner.ReadOnly(arguments
                .OrderBy(value => Required(value.Ordinal, "Argument.Ordinal"))
                .Select(value => Required(value.Value, "Argument.Value"))),
            Required(server.CredentialAlias, "CredentialAlias"),
            Required(server.Enabled, "Enabled"),
            Required(server.LogicalRevision, "LogicalRevision"),
            ParseStatus(server.Status),
            Required(server.LastError, "LastError"),
            server.LastSyncedAtUtc.HasValue ? ToDateTimeOffset(server.LastSyncedAtUtc.Value) : null,
            McpContractCloner.ReadOnly(currentToolIds),
            McpContractCloner.ReadOnly(mappedTools));
    }

    private static McpTransportKind ParseTransport(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out McpTransportKind result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"MCP Transport contains unsupported value '{value}'.");

    private static McpServerStatus ParseStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out McpServerStatus result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"MCP Status contains unsupported value '{value}'.");

    private static McpToolRisk ParseRisk(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out McpToolRisk result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"MCP Tool Risk contains unsupported value '{value}'.");

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string Required(string? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");

    private static Guid Required(Guid? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");

    private static int Required(int? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");

    private static long Required(long? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");

    private static bool Required(bool? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");

    private static DateTime Required(DateTime? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");

    private static ServiceResult<McpServerDefinition>? ValidateConfiguration(
        McpTransportKind transport,
        string? endpoint,
        string? command,
        IReadOnlyList<string>? arguments,
        string? credentialAlias)
    {
        if (!Enum.IsDefined(transport))
        {
            return Failure(McpErrorCodes.ConfigurationInvalid, "The MCP transport is invalid.");
        }

        if (transport is McpTransportKind.StreamableHttp or McpTransportKind.Sse)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo))
            {
                return Failure(
                    McpErrorCodes.ConfigurationInvalid,
                    "HTTP MCP endpoints must be absolute HTTP or HTTPS URIs without embedded credentials.");
            }
        }
        else if (string.IsNullOrWhiteSpace(command) || command.Length > 512)
        {
            return Failure(McpErrorCodes.ConfigurationInvalid, "Stdio MCP command is required.");
        }

        if ((arguments?.Count ?? 0) > 32 ||
            (arguments?.Any(argument => argument is null || argument.Length > 1024) ?? false))
        {
            return Failure(McpErrorCodes.ConfigurationInvalid, "MCP command arguments exceed the supported limits.");
        }

        if (!string.IsNullOrWhiteSpace(credentialAlias) &&
            (!credentialAlias.StartsWith("alias:", StringComparison.Ordinal) ||
             credentialAlias.Length > 200 ||
             credentialAlias["alias:".Length..].Length == 0 ||
             credentialAlias["alias:".Length..].Any(character =>
                 !char.IsAsciiLetterOrDigit(character) &&
                 character is not ('.' or '_' or '-'))))
        {
            return Failure(
                McpErrorCodes.ConfigurationInvalid,
                "MCP credentials must be represented by an alias: identifier.");
        }

        return null;
    }

    private static ServiceResult<McpServerDefinition>? ValidateDiscoveredTools(IReadOnlyList<DiscoveredMcpTool>? tools)
    {
        if (tools is null || tools.Count > MaximumTools)
        {
            return Failure(McpErrorCodes.DiscoveryFailed, "MCP discovery returned an unsupported tool count.");
        }

        if (tools.Any(tool =>
                string.IsNullOrWhiteSpace(tool.Name) ||
                tool.Name.Length > 256 ||
                tool.Description?.Length > 4096 ||
                tool.InputSchemaJson?.Length > 65_536) ||
            tools.Select(tool => tool.Name).Distinct(StringComparer.Ordinal).Count() != tools.Count)
        {
            return Failure(McpErrorCodes.DiscoveryFailed, "MCP discovery returned invalid or duplicate tools.");
        }

        try
        {
            foreach (DiscoveredMcpTool tool in tools)
            {
                using JsonDocument schema = JsonDocument.Parse(tool.InputSchemaJson);
                if (schema.RootElement.ValueKind is not JsonValueKind.Object)
                {
                    return Failure(McpErrorCodes.DiscoveryFailed, "MCP tool input schemas must be JSON objects.");
                }
            }
        }
        catch (JsonException)
        {
            return Failure(McpErrorCodes.DiscoveryFailed, "MCP discovery returned an invalid input schema.");
        }

        return null;
    }

    private static string CanonicalizeJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static string Hash(string name, string description, string schema, McpToolRisk risk)
    {
        string source = $"{name}\n{description}\n{schema}\n{risk}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))
            .ToLowerInvariant();
    }

    private static string SanitizeFailure(Exception exception)
    {
        string value;
        if (exception is TimeoutException)
        {
            value = "MCP discovery timed out.";
        }
        else if (exception is InvalidOperationException &&
                 new[]
                 {
                     "The MCP endpoint ",
                     "The MCP stdio ",
                     "The MCP credential ",
                     "Credential aliases ",
                     "The MCP transport "
                 }.Any(prefix => exception.Message.StartsWith(prefix, StringComparison.Ordinal)))
        {
            value = exception.Message;
        }
        else
        {
            value = "MCP discovery failed.";
        }

        return value.Length <= 500 ? value : value[..500];
    }

    private static ServiceResult<McpServerDefinition> Failure(string code, string message) =>
        ServiceResult<McpServerDefinition>.Failure(
            McpServiceStatusCodes.FromErrorCode(code),
            message);

    private static bool TryNormalizeCode(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var builder = new StringBuilder();
        bool separator = false;
        foreach (char character in value.Trim())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (separator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                separator = false;
            }
            else
            {
                separator = true;
            }
        }

        if (builder.Length is 0 or > 100)
        {
            return false;
        }

        normalized = builder.ToString();
        return true;
    }
}

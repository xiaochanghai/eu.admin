using EU.Core.IServices.Mcp;
using System.Security.Cryptography;
using System.Text.Json;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgMcpServerDefinitionServices 职责实现

/// <summary>
/// 提供 MCP 服务定义及工具版本的持久化服务。
/// </summary>
public sealed class AgMcpServerDefinitionServices :
    BaseServices<AgMcpServerDefinition>,
    IAgMcpServerDefinitionServices,
    IMcpServerDefinitionCatalog,
    IPublishedMcpToolCatalog
{
    private const int MaximumTools = 256;
    private readonly IMcpToolDiscovery _discovery;

    #region 构造（AgMcpServerDefinitionServices）
    /// <summary>
    /// 构造（AgMcpServerDefinitionServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    /// <param name="discovery">MCP 工具发现服务。</param>
    public AgMcpServerDefinitionServices(IBaseRepository<AgMcpServerDefinition> dal, IMcpToolDiscovery discovery)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
    }
    #endregion

    #region 创建（CreateAsync）
    /// <summary>
    /// 创建（CreateAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<McpServerDefinition>> CreateAsync(CreateMcpServerCommand command, CancellationToken cancellationToken = default)
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
            ? Success(definition)
            : Failure(McpErrorCodes.CodeConflict, "An MCP Server already uses this code.");
    }
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含参数及工具版本历史的 MCP 服务定义；不存在时为 null。</returns>
    public Task<McpServerDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetDefinitionAsync(id, cancellationToken);
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配搜索和状态条件的完整 MCP 服务定义，按编码及标识排序；未指定状态时排除已归档服务。</returns>
    public Task<IReadOnlyList<McpServerDefinition>> ListAsync(McpServerQuery query, CancellationToken cancellationToken = default) =>
        QueryDefinitionsAsync(query, cancellationToken);
    #endregion

    #region 更新（UpdateAsync）
    /// <summary>
    /// 更新（UpdateAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<McpServerDefinition>> UpdateAsync(UpdateMcpServerCommand command, CancellationToken cancellationToken = default)
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
            ? Success(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before this operation completed.");
    }
    #endregion

    #region 处理（SyncAsync）
    /// <summary>
    /// 处理（SyncAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<McpServerDefinition>> SyncAsync(SyncMcpServerCommand command, CancellationToken cancellationToken = default)
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
            ? Success(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before synchronization completed.");
    }
    #endregion

    #region 处理（ClassifyToolAsync）
    /// <summary>
    /// 处理（ClassifyToolAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<McpServerDefinition>> ClassifyToolAsync(ClassifyMcpToolCommand command, CancellationToken cancellationToken = default)
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
            ? Success(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before classification completed.");
    }
    #endregion

    #region 设置（SetArchivedAsync）
    /// <summary>
    /// 设置（SetArchivedAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<McpServerDefinition>> SetArchivedAsync(SetMcpServerArchiveCommand command, CancellationToken cancellationToken = default)
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
            ? Success(updated)
            : Failure(
                McpErrorCodes.RevisionConflict,
                "The MCP Server changed before this operation completed.");
    }
    #endregion

    #region 查找（FindAgentReferenceBlockersAsync）
    /// <summary>
    /// 查找（FindAgentReferenceBlockersAsync）
    /// </summary>
    /// <param name="serverToolIds">服务器所属工具标识集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>最多八个启用 Agent 的编码，其最新发布快照引用了指定服务的工具版本。</returns>
    private async Task<string[]> FindAgentReferenceBlockersAsync(IReadOnlySet<Guid> serverToolIds, CancellationToken cancellationToken)
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
    #endregion

    #region 查询可用的 MCP 工具版本是否存在（ExistsAsync）
    /// <summary>
    /// 查询可用的 MCP 工具版本是否存在（ExistsAsync）。
    /// </summary>
    /// <param name="toolVersionId">工具版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定工具版本及所属服务均未删除、服务未归档且工具风险不是 Unknown 时返回 true，否则返回 false。</returns>
    public async Task<bool> ExistsAsync(Guid toolVersionId, CancellationToken cancellationToken = default)
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
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>启用且未归档 MCP 服务的当前工具版本引用，排除已删除及风险级别为 Unknown 的工具。</returns>
    async Task<IReadOnlyList<PublishedMcpToolReference>> IPublishedMcpToolCatalog.ListAsync(CancellationToken cancellationToken)
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
    #endregion

    #region 获取（GetDefinitionAsync）
    /// <summary>
    /// 获取（GetDefinitionAsync）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含参数及工具版本历史的 MCP 服务定义；不存在时为 null。</returns>
    private async Task<McpServerDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken)
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
    #endregion

    #region 查询（QueryDefinitionsAsync）
    /// <summary>
    /// 查询（QueryDefinitionsAsync）
    /// </summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配搜索和状态条件的完整 MCP 服务定义，按编码及标识排序；未指定状态时排除已归档服务。</returns>
    private async Task<IReadOnlyList<McpServerDefinition>> QueryDefinitionsAsync(McpServerQuery query, CancellationToken cancellationToken)
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
    #endregion

    #region 加载（LoadDefinitionAsync）
    /// <summary>
    /// 加载（LoadDefinitionAsync）
    /// </summary>
    /// <param name="server">MCP 服务器定义。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>补齐启动参数、当前工具标识及工具历史的 MCP 服务定义。</returns>
    private async Task<McpServerDefinition> LoadDefinitionAsync(AgMcpServerDefinition server, CancellationToken cancellationToken)
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
    #endregion

    #region 尝试执行（TryCreateDefinitionAsync）
    /// <summary>
    /// 尝试执行（TryCreateDefinitionAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，其结果为：操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    private async Task<bool> TryCreateDefinitionAsync(McpServerDefinition definition, CancellationToken cancellationToken)
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
    #endregion

    #region 尝试执行（TryReplaceDefinitionAsync）
    /// <summary>
    /// 尝试执行（TryReplaceDefinitionAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，其结果为：操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    private async Task<bool> TryReplaceDefinitionAsync(McpServerDefinition definition, long expectedLogicalRevision, CancellationToken cancellationToken)
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
    #endregion

    #region 映射（MapDefinitionEntity）
    /// <summary>
    /// 映射（MapDefinitionEntity）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <returns>由 MCP 服务定义构造的主表持久化实体。</returns>
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
    #endregion

    #region 映射（MapArgumentEntities）
    /// <summary>
    /// 映射（MapArgumentEntities）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <returns>保持启动参数顺序、具有新标识的服务参数实体集合。</returns>
    private static List<AgMcpServerArgument> MapArgumentEntities(McpServerDefinition definition) =>
        definition.Arguments.Select((value, ordinal) => new AgMcpServerArgument
        {
            ID = Guid.NewGuid(),
            ServerId = definition.Id,
            Ordinal = ordinal,
            Value = value
        }).ToList();
    #endregion

    #region 映射（MapToolEntity）
    /// <summary>
    /// 映射（MapToolEntity）
    /// </summary>
    /// <param name="tool">工具定义。</param>
    /// <param name="historyOrdinal">工具版本在历史版本集合中的排序序号。</param>
    /// <param name="currentOrdinal">工具版本在当前工具集合中的序号；null 表示不是当前版本。</param>
    /// <returns>带有历史序号和可选当前序号的 MCP 工具版本实体。</returns>
    private static AgMcpToolVersion MapToolEntity(McpToolVersion tool, int historyOrdinal, int? currentOrdinal) =>
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
    #endregion

    #region 映射（MapDefinition）
    /// <summary>
    /// 映射（MapDefinition）
    /// </summary>
    /// <param name="server">MCP 服务器定义。</param>
    /// <param name="arguments">调用参数。</param>
    /// <param name="tools">工具集合。</param>
    /// <returns>包含有序启动参数、当前工具标识及工具版本历史的 MCP 服务定义。</returns>
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
    #endregion

    #region 解析（ParseTransport）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseTransport）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static McpTransportKind ParseTransport(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out McpTransportKind result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"MCP Transport contains unsupported value '{value}'.");
    #endregion

    #region 解析（ParseStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static McpServerStatus ParseStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out McpServerStatus result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"MCP Status contains unsupported value '{value}'.");
    #endregion

    #region 解析（ParseRisk）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseRisk）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static McpToolRisk ParseRisk(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out McpToolRisk result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException($"MCP Tool Risk contains unsupported value '{value}'.");
    #endregion

    #region 转换（ToDateTimeOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToDateTimeOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。</returns>
    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static Guid Required(Guid? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static int Required(int? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static long Required(long? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");
    #endregion

    #region 读取必填的 MCP 布尔字段（Required）
    /// <summary>
    /// 读取必填的 MCP 布尔字段（Required）。
    /// </summary>
    /// <param name="value">从持久化数据中读取的可空布尔字段。</param>
    /// <param name="name">用于构造缺失字段错误信息的字段名称。</param>
    /// <returns>返回字段原有布尔值，包括 false；为 null 时抛出 InvalidDataException。</returns>
    private static bool Required(bool? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static DateTime Required(DateTime? value, string name) =>
        value ?? throw new InvalidDataException($"MCP {name} is required.");
    #endregion

    #region 校验（ValidateConfiguration）
    /// <summary>
    /// 校验（ValidateConfiguration）
    /// </summary>
    /// <param name="transport">MCP 传输方式。</param>
    /// <param name="endpoint">远程服务端点地址。</param>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="arguments">调用参数。</param>
    /// <param name="credentialAlias">模型凭据别名。</param>
    /// <returns>传输方式、端点、启动参数或凭据别名配置无效时的失败服务结果；全部通过时为 null。</returns>
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
    #endregion

    #region 校验（ValidateDiscoveredTools）
    /// <summary>
    /// 校验（ValidateDiscoveredTools）
    /// </summary>
    /// <param name="tools">工具集合。</param>
    /// <returns>发现工具数量、名称或输入 Schema 无效时的失败服务结果；全部通过时为 null。</returns>
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
    #endregion

    #region 判断是否允许（CanonicalizeJson）
    /// <summary>
    /// 判断是否允许（CanonicalizeJson）
    /// </summary>
    /// <param name="json">需要解析并重新序列化的 JSON 文本。</param>
    /// <returns>解析后重新序列化的紧凑 JSON 文本；无效 JSON 抛出解析异常。</returns>
    private static string CanonicalizeJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }
    #endregion

    #region 检查是否存在（Hash）
    /// <summary>
    /// 检查是否存在（Hash）
    /// </summary>
    /// <param name="name">对象或字段名称。</param>
    /// <param name="description">对象说明文本。</param>
    /// <param name="schema">用于校验的 JSON 架构。</param>
    /// <param name="risk">工具风险等级。</param>
    /// <returns>工具名称、描述、Schema 和风险级别以换行分隔后计算的 SHA-256 小写十六进制摘要。</returns>
    private static string Hash(string name, string description, string schema, McpToolRisk risk)
    {
        string source = $"{name}\n{description}\n{schema}\n{risk}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))
            .ToLowerInvariant();
    }
    #endregion

    #region 处理（SanitizeFailure）
    /// <summary>
    /// 处理（SanitizeFailure）
    /// </summary>
    /// <param name="exception">当前捕获的异常。</param>
    /// <returns>最多 500 字符的安全发现失败提示，仅保留允许的异常消息前缀。</returns>
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
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<McpServerDefinition> Failure(string code, string message) =>
        ServiceResult<McpServerDefinition>.Failure(
            McpServiceStatusCodes.FromErrorCode(code),
            message);
    #endregion

    #region 尝试执行（TryNormalizeCode）
    /// <summary>
    /// 尝试执行（TryNormalizeCode）
    /// </summary>
    /// <param name="value">待规范化并校验格式的业务编码。</param>
    /// <param name="normalized">规范化后的值。</param>
    /// <returns>操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
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
    #endregion
}

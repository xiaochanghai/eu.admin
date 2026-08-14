using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Agent.Application.Mcp;

public sealed class McpLifecycleService(
    IMcpServerRepository repository,
    IMcpToolDiscovery discovery,
    IAgentDefinitionCatalog? agents = null)
{
    private const int MaximumTools = 256;

    public async Task<McpOperationResult<McpServerDefinition>> CreateAsync(
        CreateMcpServerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeCode(command.Code, out string? code))
        {
            return Failure(McpErrorCodes.CodeInvalid, "MCP Server code must normalize to lowercase kebab-case.");
        }

        McpError? configurationError = ValidateConfiguration(
            command.Transport,
            command.Endpoint,
            command.Command,
            command.Arguments,
            command.CredentialAlias);
        if (configurationError is not null)
        {
            return new McpOperationResult<McpServerDefinition>(null, configurationError);
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
        return await repository.TryCreateAsync(definition, cancellationToken)
            ? McpOperationResult<McpServerDefinition>.Success(definition)
            : Failure(McpErrorCodes.CodeConflict, "An MCP Server already uses this code.");
    }

    public Task<McpServerDefinition?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(query, cancellationToken);

    public async Task<McpOperationResult<McpServerDefinition>> UpdateAsync(
        UpdateMcpServerCommand command,
        CancellationToken cancellationToken = default)
    {
        McpServerDefinition? existing =
            await repository.GetByIdAsync(command.ServerId, cancellationToken);
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

        McpError? configurationError = ValidateConfiguration(
            command.Transport,
            command.Endpoint,
            command.Command,
            command.Arguments,
            command.CredentialAlias);
        if (configurationError is not null)
        {
            return new McpOperationResult<McpServerDefinition>(null, configurationError);
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
        return await repository.TryReplaceAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? McpOperationResult<McpServerDefinition>.Success(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before this operation completed.");
    }

    public async Task<McpOperationResult<McpServerDefinition>> SyncAsync(
        SyncMcpServerCommand command,
        CancellationToken cancellationToken = default)
    {
        McpServerDefinition? existing =
            await repository.GetByIdAsync(command.ServerId, cancellationToken);
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
            discovered = await discovery.DiscoverAsync(existing, cancellationToken);
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
            await repository.TryReplaceAsync(
                unhealthy,
                command.ExpectedLogicalRevision,
                cancellationToken);
            return Failure(McpErrorCodes.DiscoveryFailed, reason);
        }

        McpError? discoveryError = ValidateDiscoveredTools(discovered);
        if (discoveryError is not null)
        {
            return new McpOperationResult<McpServerDefinition>(null, discoveryError);
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
        return await repository.TryReplaceAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? McpOperationResult<McpServerDefinition>.Success(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before synchronization completed.");
    }

    public async Task<McpOperationResult<McpServerDefinition>> ClassifyToolAsync(
        ClassifyMcpToolCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Risk) || command.Risk == McpToolRisk.Unknown)
        {
            return Failure(McpErrorCodes.RiskInvalid, "Tool risk must be ReadOnly, Mutating, or HighRisk.");
        }

        McpServerDefinition? existing =
            await repository.GetByIdAsync(command.ServerId, cancellationToken);
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
        return await repository.TryReplaceAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? McpOperationResult<McpServerDefinition>.Success(updated)
            : Failure(McpErrorCodes.RevisionConflict, "The MCP Server changed before classification completed.");
    }

    public async Task<McpOperationResult<McpServerDefinition>> SetArchivedAsync(
        SetMcpServerArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        McpServerDefinition? existing =
            await repository.GetByIdAsync(command.ServerId, cancellationToken);
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

        if (command.Archived && agents is not null)
        {
            var serverToolIds = existing.ToolVersions
                .Select(value => value.Id)
                .ToHashSet();
            IReadOnlyList<AgentDefinition> enabledAgents = await agents.ListDefinitionsAsync(
                new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Enabled),
                cancellationToken);
            string[] blockers = enabledAgents
                .Where(value => value.PublishedVersions.LastOrDefault()?.Snapshot?.Tools
                    .Any(binding => serverToolIds.Contains(binding.ToolVersionId)) == true)
                .Select(value => value.Code)
                .Take(8)
                .ToArray();
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
        return await repository.TryReplaceAsync(
            updated,
            command.ExpectedLogicalRevision,
            cancellationToken)
            ? McpOperationResult<McpServerDefinition>.Success(updated)
            : Failure(
                McpErrorCodes.RevisionConflict,
                "The MCP Server changed before this operation completed.");
    }

    private static McpError? ValidateConfiguration(
        McpTransportKind transport,
        string? endpoint,
        string? command,
        IReadOnlyList<string>? arguments,
        string? credentialAlias)
    {
        if (!Enum.IsDefined(transport))
        {
            return new McpError(McpErrorCodes.ConfigurationInvalid, "The MCP transport is invalid.");
        }

        if (transport is McpTransportKind.StreamableHttp or McpTransportKind.Sse)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo))
            {
                return new McpError(
                    McpErrorCodes.ConfigurationInvalid,
                    "HTTP MCP endpoints must be absolute HTTP or HTTPS URIs without embedded credentials.");
            }
        }
        else if (string.IsNullOrWhiteSpace(command) || command.Length > 512)
        {
            return new McpError(McpErrorCodes.ConfigurationInvalid, "Stdio MCP command is required.");
        }

        if ((arguments?.Count ?? 0) > 32 ||
            (arguments?.Any(argument => argument is null || argument.Length > 1024) ?? false))
        {
            return new McpError(McpErrorCodes.ConfigurationInvalid, "MCP command arguments exceed the supported limits.");
        }

        if (!string.IsNullOrWhiteSpace(credentialAlias) &&
            (!credentialAlias.StartsWith("alias:", StringComparison.Ordinal) ||
             credentialAlias.Length > 200 ||
             credentialAlias["alias:".Length..].Length == 0 ||
             credentialAlias["alias:".Length..].Any(character =>
                 !char.IsAsciiLetterOrDigit(character) &&
                 character is not ('.' or '_' or '-'))))
        {
            return new McpError(
                McpErrorCodes.ConfigurationInvalid,
                "MCP credentials must be represented by an alias: identifier.");
        }

        return null;
    }

    private static McpError? ValidateDiscoveredTools(IReadOnlyList<DiscoveredMcpTool>? tools)
    {
        if (tools is null || tools.Count > MaximumTools)
        {
            return new McpError(McpErrorCodes.DiscoveryFailed, "MCP discovery returned an unsupported tool count.");
        }

        if (tools.Any(tool =>
                string.IsNullOrWhiteSpace(tool.Name) ||
                tool.Name.Length > 256 ||
                tool.Description?.Length > 4096 ||
                tool.InputSchemaJson?.Length > 65_536) ||
            tools.Select(tool => tool.Name).Distinct(StringComparer.Ordinal).Count() != tools.Count)
        {
            return new McpError(McpErrorCodes.DiscoveryFailed, "MCP discovery returned invalid or duplicate tools.");
        }

        try
        {
            foreach (DiscoveredMcpTool tool in tools)
            {
                using JsonDocument schema = JsonDocument.Parse(tool.InputSchemaJson);
                if (schema.RootElement.ValueKind is not JsonValueKind.Object)
                {
                    return new McpError(McpErrorCodes.DiscoveryFailed, "MCP tool input schemas must be JSON objects.");
                }
            }
        }
        catch (JsonException)
        {
            return new McpError(McpErrorCodes.DiscoveryFailed, "MCP discovery returned an invalid input schema.");
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

    private static McpOperationResult<McpServerDefinition> Failure(string code, string message) =>
        McpOperationResult<McpServerDefinition>.Failure(code, message);

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

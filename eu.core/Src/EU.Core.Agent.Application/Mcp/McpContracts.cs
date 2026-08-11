using System.Collections.ObjectModel;

namespace EU.Core.Agent.Application.Mcp;

public enum McpTransportKind
{
    StreamableHttp,
    Sse,
    Stdio
}

public enum McpServerStatus
{
    NotSynced,
    Healthy,
    Unhealthy,
    Disabled,
    Archived
}

public enum McpToolRisk
{
    Unknown,
    ReadOnly,
    Mutating,
    HighRisk
}

public sealed record McpToolVersion(
    Guid Id,
    Guid ServerId,
    string Name,
    string Description,
    string InputSchemaJson,
    McpToolRisk Risk,
    string Sha256,
    DateTimeOffset DiscoveredAtUtc);

public sealed record McpServerDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string> Arguments,
    string CredentialAlias,
    bool Enabled,
    long LogicalRevision,
    McpServerStatus Status,
    string LastError,
    DateTimeOffset? LastSyncedAtUtc,
    IReadOnlyList<Guid> CurrentToolVersionIds,
    IReadOnlyList<McpToolVersion> ToolVersions);

public sealed record McpServerQuery(string? Search = null, McpServerStatus? Status = null);

public sealed record DiscoveredMcpTool(
    string Name,
    string Description,
    string InputSchemaJson);

public sealed record PublishedMcpToolReference(
    Guid ServerId,
    string ServerCode,
    string ServerName,
    Guid ToolVersionId,
    string ToolName,
    string Description,
    string InputSchemaJson,
    McpToolRisk Risk,
    string Sha256);

public sealed record CreateMcpServerCommand(
    string Code,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string>? Arguments,
    string CredentialAlias,
    bool Enabled);

public sealed record UpdateMcpServerCommand(
    Guid ServerId,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string>? Arguments,
    string CredentialAlias,
    bool Enabled);

public sealed record SyncMcpServerCommand(Guid ServerId, long ExpectedLogicalRevision);

public sealed record ClassifyMcpToolCommand(
    Guid ServerId,
    Guid ToolVersionId,
    long ExpectedLogicalRevision,
    McpToolRisk Risk);

public sealed record SetMcpServerArchiveCommand(
    Guid ServerId,
    long ExpectedLogicalRevision,
    bool Archived);

public sealed record McpError(string Code, string Message);

public sealed record McpOperationResult<T>(T? Value, McpError? Error)
{
    public bool Succeeded => Error is null;

    public static McpOperationResult<T> Success(T value) => new(value, null);

    public static McpOperationResult<T> Failure(string code, string message) =>
        new(default, new McpError(code, message));
}

public static class McpErrorCodes
{
    public const string NotFound = "MCP_SERVER_NOT_FOUND";
    public const string CodeInvalid = "MCP_SERVER_CODE_INVALID";
    public const string CodeConflict = "MCP_SERVER_CODE_CONFLICT";
    public const string ConfigurationInvalid = "MCP_CONFIGURATION_INVALID";
    public const string RevisionConflict = "MCP_REVISION_CONFLICT";
    public const string DiscoveryFailed = "MCP_DISCOVERY_FAILED";
    public const string ToolNotFound = "MCP_TOOL_NOT_FOUND";
    public const string RiskInvalid = "MCP_TOOL_RISK_INVALID";
    public const string LifecycleTransitionInvalid = "MCP_LIFECYCLE_TRANSITION_INVALID";
    public const string ArchiveBlocked = "MCP_ARCHIVE_BLOCKED";
}

public interface IMcpServerRepository
{
    Task<McpServerDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        McpServerDefinition definition,
        CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        McpServerDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);
}

public interface IMcpToolDiscovery
{
    Task<IReadOnlyList<DiscoveredMcpTool>> DiscoverAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken = default);
}

public interface IPublishedMcpToolCatalog
{
    Task<bool> ExistsAsync(Guid toolVersionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(
        CancellationToken cancellationToken = default);
}

public static class McpContractCloner
{
    public static McpServerDefinition Clone(McpServerDefinition definition) =>
        definition with
        {
            Arguments = ReadOnly(definition.Arguments),
            CurrentToolVersionIds = ReadOnly(definition.CurrentToolVersionIds),
            ToolVersions = ReadOnly(definition.ToolVersions.Select(version => version with { }))
        };

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());

    public static bool PreservesToolHistory(
        McpServerDefinition existing,
        McpServerDefinition replacement)
    {
        if (replacement.ToolVersions.Count < existing.ToolVersions.Count)
        {
            return false;
        }

        return existing.ToolVersions
            .Select((version, index) => version == replacement.ToolVersions[index])
            .All(value => value);
    }
}

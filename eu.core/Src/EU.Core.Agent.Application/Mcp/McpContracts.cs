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
    public const string DisableBlocked = "MCP_DISABLE_BLOCKED";
    public const string ArchiveBlocked = "MCP_ARCHIVE_BLOCKED";
}

public static class McpServiceStatusCodes
{
    public const int NotFound = 630001;
    public const int CodeInvalid = 630002;
    public const int CodeConflict = 630003;
    public const int ConfigurationInvalid = 630004;
    public const int RevisionConflict = 630005;
    public const int DiscoveryFailed = 630006;
    public const int ToolNotFound = 630007;
    public const int RiskInvalid = 630008;
    public const int LifecycleTransitionInvalid = 630009;
    public const int DisableBlocked = 630010;
    public const int ArchiveBlocked = 630011;

    public static int FromErrorCode(string errorCode) => errorCode switch
    {
        McpErrorCodes.NotFound => NotFound,
        McpErrorCodes.CodeInvalid => CodeInvalid,
        McpErrorCodes.CodeConflict => CodeConflict,
        McpErrorCodes.ConfigurationInvalid => ConfigurationInvalid,
        McpErrorCodes.RevisionConflict => RevisionConflict,
        McpErrorCodes.DiscoveryFailed => DiscoveryFailed,
        McpErrorCodes.ToolNotFound => ToolNotFound,
        McpErrorCodes.RiskInvalid => RiskInvalid,
        McpErrorCodes.LifecycleTransitionInvalid => LifecycleTransitionInvalid,
        McpErrorCodes.DisableBlocked => DisableBlocked,
        McpErrorCodes.ArchiveBlocked => ArchiveBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
    };

    public static string ToErrorCode(int status) => status switch
    {
        NotFound => McpErrorCodes.NotFound,
        CodeInvalid => McpErrorCodes.CodeInvalid,
        CodeConflict => McpErrorCodes.CodeConflict,
        ConfigurationInvalid => McpErrorCodes.ConfigurationInvalid,
        RevisionConflict => McpErrorCodes.RevisionConflict,
        DiscoveryFailed => McpErrorCodes.DiscoveryFailed,
        ToolNotFound => McpErrorCodes.ToolNotFound,
        RiskInvalid => McpErrorCodes.RiskInvalid,
        LifecycleTransitionInvalid => McpErrorCodes.LifecycleTransitionInvalid,
        DisableBlocked => McpErrorCodes.DisableBlocked,
        ArchiveBlocked => McpErrorCodes.ArchiveBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}

public interface IMcpServerDefinitionCatalog
{
    Task<McpServerDefinition?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
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

#nullable enable

namespace EU.Core.IServices.MainAgent;

public sealed record MainAgentAssignment(
    Guid AgentId,
    Guid AgentVersionId,
    long LogicalRevision,
    DateTimeOffset UpdatedAtUtc);

public interface IMainAgentAssignmentRepository
{
    Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        MainAgentAssignment value,
        long? expectedLogicalRevision,
        CancellationToken cancellationToken = default);
}

public sealed record SetMainAgentCommand(Guid AgentId, long? ExpectedLogicalRevision);

public static class MainAgentErrorCodes
{
    public const string NotConfigured = "MAIN_AGENT_NOT_CONFIGURED";
    public const string AgentNotFound = "MAIN_AGENT_AGENT_NOT_FOUND";
    public const string AgentDisabled = "MAIN_AGENT_AGENT_DISABLED";
    public const string VersionMissing = "MAIN_AGENT_VERSION_MISSING";
    public const string RowVersionConflict = "MAIN_AGENT_ROW_VERSION_CONFLICT";
}

public static class MainAgentServiceStatusCodes
{
    public const int NotConfigured = 610004;
    public const int AgentNotFound = 610018;
    public const int AgentDisabled = 610019;
    public const int VersionMissing = 610020;
    public const int RowVersionConflict = 610021;

    public static int FromErrorCode(string code) => code switch
    {
        MainAgentErrorCodes.NotConfigured => NotConfigured,
        MainAgentErrorCodes.AgentNotFound => AgentNotFound,
        MainAgentErrorCodes.AgentDisabled => AgentDisabled,
        MainAgentErrorCodes.VersionMissing => VersionMissing,
        MainAgentErrorCodes.RowVersionConflict => RowVersionConflict,
        _ => 500
    };

    public static string ToErrorCode(int status) => status switch
    {
        NotConfigured => MainAgentErrorCodes.NotConfigured,
        AgentNotFound => MainAgentErrorCodes.AgentNotFound,
        AgentDisabled => MainAgentErrorCodes.AgentDisabled,
        VersionMissing => MainAgentErrorCodes.VersionMissing,
        RowVersionConflict => MainAgentErrorCodes.RowVersionConflict,
        _ => "INTERNAL_ERROR"
    };
}

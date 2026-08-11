namespace EU.Core.Agent.Application.MainAgent;

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

public sealed record MainAgentError(string Code, string Message);

public static class MainAgentErrorCodes
{
    public const string NotConfigured = "MAIN_AGENT_NOT_CONFIGURED";
    public const string AgentNotFound = "MAIN_AGENT_AGENT_NOT_FOUND";
    public const string AgentDisabled = "MAIN_AGENT_AGENT_DISABLED";
    public const string VersionMissing = "MAIN_AGENT_VERSION_MISSING";
    public const string RowVersionConflict = "MAIN_AGENT_ROW_VERSION_CONFLICT";
}

public sealed record MainAgentOperationResult(MainAgentAssignment? Value, MainAgentError? Error)
{
    public bool Succeeded => Error is null;

    public static MainAgentOperationResult Success(MainAgentAssignment value) => new(value, null);

    public static MainAgentOperationResult Failure(string code, string message) =>
        new(null, new MainAgentError(code, message));
}

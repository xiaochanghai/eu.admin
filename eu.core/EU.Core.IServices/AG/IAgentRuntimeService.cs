using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.IServices;

public interface IAgentRuntimeService
{
    Task<AgentRunPreparationResult> PrepareAsync(
        Guid agentId,
        string? input,
        CancellationToken cancellationToken = default);

    Task<AgentRunPreparationResult> PrepareVersionAsync(
        Guid agentId,
        Guid agentVersionId,
        string? input,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AgentRunEvent> StreamAsync(
        AgentRunContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentRunAuditRecord>> ListAuditAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default);

    Task TerminatePreparedRunAsync(
        AgentRunContext context,
        AgentRunStatus status,
        string errorCode,
        CancellationToken cancellationToken = default);
}

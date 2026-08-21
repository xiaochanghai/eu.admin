#nullable enable

namespace EU.Core.IServices.Abstractions.Auditing;

public sealed record AgentOperationAuditRecord(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string TenantId,
    string UserId,
    string CorrelationId,
    string Policy,
    string Method,
    string Path,
    int StatusCode,
    string Outcome,
    string? ErrorCode,
    long DurationMilliseconds);

public interface IAgentOperationAuditRepository
{
    Task SaveAsync(
        AgentOperationAuditRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(
        string tenantId,
        int take,
        CancellationToken cancellationToken = default);
}

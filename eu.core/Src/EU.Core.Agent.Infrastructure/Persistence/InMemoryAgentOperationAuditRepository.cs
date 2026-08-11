using EU.Core.Agent.Application.Abstractions.Auditing;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryAgentOperationAuditRepository
    : IAgentOperationAuditRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, AgentOperationAuditRecord> _records = [];

    public Task SaveAsync(
        AgentOperationAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_records.TryGetValue(record.Id, out AgentOperationAuditRecord? current))
            {
                _records[record.Id] = record;
            }
            else if (string.Equals(current.Outcome, "Started", StringComparison.Ordinal) &&
                SameIdentity(current, record))
            {
                _records[record.Id] = record;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<AgentOperationAuditRecord> values = _records.Values
                .Where(record => string.Equals(
                    record.TenantId,
                    tenantId,
                    StringComparison.Ordinal))
                .OrderByDescending(record => record.OccurredAtUtc)
                .ThenByDescending(record => record.Id)
                .Take(Math.Clamp(take, 1, 100))
                .ToArray();
            return Task.FromResult(values);
        }
    }

    private static bool SameIdentity(
        AgentOperationAuditRecord current,
        AgentOperationAuditRecord replacement) =>
        current.OccurredAtUtc == replacement.OccurredAtUtc &&
        string.Equals(current.TenantId, replacement.TenantId, StringComparison.Ordinal) &&
        string.Equals(current.UserId, replacement.UserId, StringComparison.Ordinal) &&
        string.Equals(current.CorrelationId, replacement.CorrelationId, StringComparison.Ordinal) &&
        string.Equals(current.Policy, replacement.Policy, StringComparison.Ordinal) &&
        string.Equals(current.Method, replacement.Method, StringComparison.Ordinal) &&
        string.Equals(current.Path, replacement.Path, StringComparison.Ordinal);
}

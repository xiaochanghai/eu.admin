using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryAgentRunAuditRepository : IAgentRunAuditRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, AgentRunAuditRecord> _records = [];

    public Task SaveAsync(
        AgentRunAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _records[record.RunId] = AgentRunContractCloner.Clone(record);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<AgentRunAuditRecord> result = _records.Values
                .Where(record => record.AgentId == agentId)
                .OrderByDescending(record => record.StartedAtUtc)
                .ThenByDescending(record => record.RunId)
                .Take(take)
                .Select(AgentRunContractCloner.Clone)
                .ToArray();
            return Task.FromResult(result);
        }
    }
}

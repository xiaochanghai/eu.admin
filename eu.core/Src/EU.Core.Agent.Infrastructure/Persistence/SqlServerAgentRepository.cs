using EU.Core.Agent.Application.Agents;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerAgentRepository : IAgentRepository
{
    private readonly SqlServerNormalizedAgentStore _store;

    public SqlServerAgentRepository(string connectionString)
    {
        _store = new SqlServerNormalizedAgentStore(connectionString);
    }

    public async Task<AgentDefinition?> GetByIdAsync(
        Guid Id,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetByIdAsync(Id, cancellationToken);
    }

    public async Task<AgentDefinition?> GetByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetByCodeAsync(normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentDefinition>> ListAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _store.ListAsync(query, cancellationToken);
    }

    public async Task<bool> TryCreateAsync(
        AgentDefinition definition,
        CancellationToken cancellationToken = default)
    {
        return await _store.TryCreateAsync(definition, cancellationToken);
    }

    public async Task<bool> TryReplaceAsync(
        AgentDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        return await _store.TryReplaceAsync(definition, expectedLogicalRevision, cancellationToken);
    }
}

using EU.Core.Agent.Application.Agents;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryAgentRepository : IAgentRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, AgentDefinition> _definitions = [];
    private readonly Dictionary<string, Guid> _idsByCode = new(StringComparer.Ordinal);

    public Task<AgentDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_definitions.TryGetValue(id, out AgentDefinition? definition)
                ? AgentContractCloner.Clone(definition)
                : null);
        }
    }

    public Task<AgentDefinition?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedCode);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_idsByCode.TryGetValue(normalizedCode, out Guid id) && _definitions.TryGetValue(id, out AgentDefinition? definition)
                ? AgentContractCloner.Clone(definition)
                : null);
        }
    }

    public Task<IReadOnlyList<AgentDefinition>> ListAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IEnumerable<AgentDefinition> results = _definitions.Values;
            if (query.RuntimeStatus is null)
            {
                results = results.Where(definition =>
                    definition.RuntimeStatus is not AgentRuntimeStatus.Archived);
            }
            if (query.RuntimeStatus is AgentRuntimeStatus status)
            {
                results = results.Where(definition => definition.RuntimeStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                results = results.Where(definition =>
                    definition.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    definition.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    definition.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(AgentContractCloner.ReadOnly(results
                .OrderBy(definition => definition.Code, StringComparer.Ordinal)
                .ThenBy(definition => definition.Id)
                .Select(AgentContractCloner.Clone)));
        }
    }

    public Task<bool> TryCreateAsync(AgentDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_idsByCode.ContainsKey(definition.Code) || _definitions.ContainsKey(definition.Id))
            {
                return Task.FromResult(false);
            }

            _definitions.Add(definition.Id, AgentContractCloner.Clone(definition));
            _idsByCode.Add(definition.Code, definition.Id);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(AgentDefinition definition, long expectedLogicalRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_definitions.TryGetValue(definition.Id, out AgentDefinition? existing) ||
                existing.LogicalRevision != expectedLogicalRevision ||
                expectedLogicalRevision == long.MaxValue ||
                definition.LogicalRevision != expectedLogicalRevision + 1 ||
                !string.Equals(existing.Code, definition.Code, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _definitions[definition.Id] = AgentContractCloner.Clone(definition);
            return Task.FromResult(true);
        }
    }
}

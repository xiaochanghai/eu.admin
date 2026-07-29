using EU.Core.Agent.Application.Orchestration;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryOrchestrationRepository : IOrchestrationRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, OrchestrationDefinition> _values = [];

    public Task<OrchestrationDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(_values.TryGetValue(id, out OrchestrationDefinition? value)
            ? OrchestrationContractCloner.Clone(value) : null);
    }

    public Task<IReadOnlyList<OrchestrationDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(OrchestrationContractCloner.ReadOnly(
            _values.Values.OrderBy(value => value.Code, StringComparer.Ordinal)
                .Select(OrchestrationContractCloner.Clone)));
    }

    public Task<bool> TryCreateAsync(OrchestrationDefinition value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_values.ContainsKey(value.Id) || _values.Values.Any(existing =>
                    string.Equals(existing.Code, value.Code, StringComparison.Ordinal)))
                return Task.FromResult(false);
            _values[value.Id] = OrchestrationContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(
        OrchestrationDefinition value,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_values.TryGetValue(value.Id, out OrchestrationDefinition? existing) ||
                existing.LogicalRevision != expectedRevision ||
                value.LogicalRevision != expectedRevision + 1 ||
                !string.Equals(existing.Code, value.Code, StringComparison.Ordinal))
                return Task.FromResult(false);
            _values[value.Id] = OrchestrationContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }
}

public sealed class InMemoryOrchestrationRunRepository : IOrchestrationRunRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, OrchestrationRunRecord> _values = [];
    private readonly Dictionary<Guid, OrchestrationRunDetails> _details = [];

    public Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) _values[value.Id] = OrchestrationContractCloner.Clone(value);
        return Task.CompletedTask;
    }

    public Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(_values.TryGetValue(id, out OrchestrationRunRecord? value)
            ? OrchestrationContractCloner.Clone(value) : null);
    }

    public Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
        Guid orchestrationId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(OrchestrationContractCloner.ReadOnly(_values.Values
            .Where(value => value.OrchestrationId == orchestrationId)
            .OrderByDescending(value => value.StartedAtUtc).Take(Math.Clamp(take, 1, 100))
            .Select(OrchestrationContractCloner.Clone)));
    }

    public Task SaveDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) _details[value.RunId] = OrchestrationContractCloner.Clone(value);
        return Task.CompletedTask;
    }

    public Task<OrchestrationRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) return Task.FromResult(
            _details.TryGetValue(runId, out OrchestrationRunDetails? value)
                ? OrchestrationContractCloner.Clone(value)
                : null);
    }
}

using EU.Core.Agent.Application.Evaluation;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryEvaluationSuiteRepository : IEvaluationSuiteRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, EvaluationSuiteDefinition> _values = [];

    public Task<EvaluationSuiteDefinition?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _values.TryGetValue(id, out EvaluationSuiteDefinition? value)
                && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal)
                    ? EvaluationSuiteContractCloner.Clone(value)
                    : null);
        }
    }

    public Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(EvaluationSuiteContractCloner.ReadOnly(
                _values.Values
                    .Where(value => string.Equals(
                        value.TenantId, tenantId, StringComparison.Ordinal))
                    .OrderBy(value => value.Code, StringComparer.Ordinal)));
        }
    }

    public Task<bool> TryCreateAsync(
        EvaluationSuiteDefinition value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_values.ContainsKey(value.Id)
                || _values.Values.Any(existing =>
                    string.Equals(existing.TenantId, value.TenantId, StringComparison.Ordinal)
                    && string.Equals(existing.Code, value.Code, StringComparison.Ordinal)))
            {
                return Task.FromResult(false);
            }

            _values[value.Id] = EvaluationSuiteContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(
        EvaluationSuiteDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_values.TryGetValue(value.Id, out EvaluationSuiteDefinition? existing)
                || !string.Equals(existing.TenantId, value.TenantId, StringComparison.Ordinal)
                || !string.Equals(existing.Code, value.Code, StringComparison.Ordinal)
                || existing.LogicalRevision != expectedLogicalRevision
                || value.LogicalRevision != expectedLogicalRevision + 1)
            {
                return Task.FromResult(false);
            }

            _values[value.Id] = EvaluationSuiteContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }
}

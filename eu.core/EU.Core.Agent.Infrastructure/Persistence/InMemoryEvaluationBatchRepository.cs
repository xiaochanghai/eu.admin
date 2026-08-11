using EU.Core.Agent.Application.Evaluation;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryEvaluationBatchRepository : IEvaluationBatchRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, EvaluationBatchRecord> _values = [];

    public Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _values.TryGetValue(id, out EvaluationBatchRecord? value)
                && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal)
                    ? EvaluationBatchContractCloner.Clone(value)
                    : null);
        }
    }

    public Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(EvaluationBatchContractCloner.ReadOnly(
                _values.Values
                    .Where(value => value.SuiteId == suiteId
                        && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal))
                    .OrderByDescending(value => value.StartedAtUtc)
                    .ThenByDescending(value => value.Id)
                    .Take(Math.Clamp(take, 1, 100))));
        }
    }

    public Task<bool> TryCreateAsync(
        EvaluationBatchRecord value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_values.ContainsKey(value.Id))
            {
                return Task.FromResult(false);
            }

            _values[value.Id] = EvaluationBatchContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(
        EvaluationBatchRecord value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_values.TryGetValue(value.Id, out EvaluationBatchRecord? existing)
                || !string.Equals(existing.TenantId, value.TenantId, StringComparison.Ordinal)
                || existing.LogicalRevision != expectedLogicalRevision
                || value.LogicalRevision != expectedLogicalRevision + 1
                || existing.Status != EvaluationBatchStatus.Running)
            {
                return Task.FromResult(false);
            }

            _values[value.Id] = EvaluationBatchContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }
}

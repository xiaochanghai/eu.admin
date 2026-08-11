using System.Collections.Concurrent;
using EU.Core.Agent.Application.Evaluation;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryModelJudgeReportRepository : IModelJudgeReportRepository
{
    private readonly ConcurrentDictionary<Guid, ModelJudgeReport> _values = new();

    public Task<ModelJudgeReport?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_values.TryGetValue(id, out ModelJudgeReport? value)
            && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal)
                ? ModelJudgeContractCloner.Clone(value)
                : null);
    }

    public Task<ModelJudgeReport?> GetByConfigurationAsync(
        Guid batchId,
        string tenantId,
        string configurationSha256,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ModelJudgeReport? value = _values.Values.SingleOrDefault(item =>
            item.BatchId == batchId
            && string.Equals(item.TenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(item.ConfigurationSha256, configurationSha256, StringComparison.Ordinal));
        return Task.FromResult(value is null ? null : ModelJudgeContractCloner.Clone(value));
    }

    public Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ModelJudgeContractCloner.ReadOnly(_values.Values
            .Where(value => value.BatchId == batchId
                && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal))
            .OrderByDescending(value => value.StartedAtUtc)
            .ThenByDescending(value => value.Id)
            .Take(Math.Clamp(take, 1, 50))));
    }

    public Task<bool> TryCreateAsync(
        ModelJudgeReport value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool duplicate = _values.Values.Any(item =>
            item.BatchId == value.BatchId
            && string.Equals(item.TenantId, value.TenantId, StringComparison.Ordinal)
            && string.Equals(item.ConfigurationSha256, value.ConfigurationSha256, StringComparison.Ordinal));
        return Task.FromResult(!duplicate
            && _values.TryAdd(value.Id, ModelJudgeContractCloner.Clone(value)));
    }
}

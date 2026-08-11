using EU.Core.Agent.Application.Skills;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemorySkillRepository : ISkillRepository, IPublishedSkillVersionCatalog
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, SkillDefinition> _definitions = [];

    public Task<SkillDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_definitions.TryGetValue(id, out SkillDefinition? value)
                ? SkillContractCloner.Clone(value)
                : null);
        }
    }

    public Task<SkillDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            SkillDefinition? value = _definitions.Values.FirstOrDefault(
                definition => string.Equals(definition.Code, code, StringComparison.Ordinal));
            return Task.FromResult(value is null ? null : SkillContractCloner.Clone(value));
        }
    }

    public Task<IReadOnlyList<SkillDefinition>> ListAsync(
        SkillQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IEnumerable<SkillDefinition> values = _definitions.Values;
            values = query.Status.HasValue
                ? values.Where(value => value.Status == query.Status.Value)
                : values.Where(value => value.Status is not SkillStatus.Archived);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                values = values.Where(value =>
                    value.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    value.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    value.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                values = values.Where(value =>
                    string.Equals(value.Category, query.Category.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(SkillContractCloner.ReadOnly(values
                .OrderBy(value => value.Code, StringComparer.Ordinal)
                .Select(SkillContractCloner.Clone)));
        }
    }

    public Task<bool> TryCreateAsync(SkillDefinition definition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_definitions.ContainsKey(definition.Id) ||
                _definitions.Values.Any(value => string.Equals(value.Code, definition.Code, StringComparison.Ordinal)))
            {
                return Task.FromResult(false);
            }

            _definitions.Add(definition.Id, SkillContractCloner.Clone(definition));
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(
        SkillDefinition definition,
        long expectedDraftRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_definitions.TryGetValue(definition.Id, out SkillDefinition? existing) ||
                existing.DraftRevision != expectedDraftRevision ||
                expectedDraftRevision == long.MaxValue ||
                definition.DraftRevision != expectedDraftRevision + 1 ||
                !string.Equals(existing.Code, definition.Code, StringComparison.Ordinal) ||
                !SkillContractCloner.PreservesPublishedHistory(existing, definition))
            {
                return Task.FromResult(false);
            }

            _definitions[definition.Id] = SkillContractCloner.Clone(definition);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_definitions.Values.Any(
                definition => definition.Status is SkillStatus.Active &&
                    definition.PublishedVersions.Any(version => version.Id == versionId)));
        }
    }

    public Task<IReadOnlyList<PublishedSkillReference>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(SkillContractCloner.ReadOnly(_definitions.Values
                .Where(definition => definition.Status is SkillStatus.Active)
                .OrderBy(definition => definition.Code, StringComparer.Ordinal)
                .SelectMany(definition => definition.PublishedVersions.Select(version =>
                    new PublishedSkillReference(
                        definition.Id,
                        version.Id,
                        definition.Code,
                        definition.Name,
                        version.Label,
                        version.ManifestSha256)))));
        }
    }
}

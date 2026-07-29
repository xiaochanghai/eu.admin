using EU.Core.Agent.Application.Mcp;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryMcpServerRepository :
    IMcpServerRepository,
    IPublishedMcpToolCatalog
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, McpServerDefinition> _definitions = [];

    public Task<McpServerDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_definitions.TryGetValue(id, out McpServerDefinition? value)
                ? McpContractCloner.Clone(value)
                : null);
        }
    }

    public Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IEnumerable<McpServerDefinition> values = _definitions.Values;
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                values = values.Where(value =>
                    value.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    value.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    value.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Status is not null)
            {
                values = values.Where(value => value.Status == query.Status);
            }

            return Task.FromResult(McpContractCloner.ReadOnly(values
                .OrderBy(value => value.Code, StringComparer.Ordinal)
                .Select(McpContractCloner.Clone)));
        }
    }

    public Task<bool> TryCreateAsync(
        McpServerDefinition definition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_definitions.ContainsKey(definition.Id) ||
                _definitions.Values.Any(value =>
                    string.Equals(value.Code, definition.Code, StringComparison.Ordinal)))
            {
                return Task.FromResult(false);
            }

            _definitions.Add(definition.Id, McpContractCloner.Clone(definition));
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(
        McpServerDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_definitions.TryGetValue(definition.Id, out McpServerDefinition? existing) ||
                existing.LogicalRevision != expectedLogicalRevision ||
                expectedLogicalRevision == long.MaxValue ||
                definition.LogicalRevision != expectedLogicalRevision + 1 ||
                !string.Equals(existing.Code, definition.Code, StringComparison.Ordinal) ||
                !McpContractCloner.PreservesToolHistory(existing, definition))
            {
                return Task.FromResult(false);
            }

            _definitions[definition.Id] = McpContractCloner.Clone(definition);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsAsync(
        Guid toolVersionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_definitions.Values.Any(server =>
                server.ToolVersions.Any(tool =>
                    tool.Id == toolVersionId &&
                    tool.Risk != McpToolRisk.Unknown)));
        }
    }

    public Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(McpContractCloner.ReadOnly(_definitions.Values
                .Where(server => server.Enabled)
                .OrderBy(server => server.Code, StringComparer.Ordinal)
                .SelectMany(server => server.CurrentToolVersionIds
                    .Select(id => server.ToolVersions.Single(tool => tool.Id == id))
                    .Where(tool => tool.Risk != McpToolRisk.Unknown)
                    .Select(tool => new PublishedMcpToolReference(
                        server.Id,
                        server.Code,
                        server.Name,
                        tool.Id,
                        tool.Name,
                        tool.Description,
                        tool.InputSchemaJson,
                        tool.Risk,
                        tool.Sha256)))));
        }
    }
}

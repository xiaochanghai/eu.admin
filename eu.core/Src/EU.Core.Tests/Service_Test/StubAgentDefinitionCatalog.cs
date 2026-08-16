using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;

#nullable enable

namespace EU.Core.Tests.Service_Test;

internal sealed class StubAgentDefinitionCatalog(
    IReadOnlyList<AgentDefinition>? definitions = null) : IAgentDefinitionCatalog
{
    public IReadOnlyList<AgentDefinition> Definitions { get; set; } = definitions ?? [];

    public Task<AgentDefinition?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Definitions.FirstOrDefault(value => value.Id == id));
    }

    public Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<AgentDefinition> values = Definitions;
        if (query.RuntimeStatus.HasValue)
        {
            values = values.Where(value => value.RuntimeStatus == query.RuntimeStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            values = values.Where(value =>
                value.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                value.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<AgentDefinition>>(values.ToArray());
    }
}

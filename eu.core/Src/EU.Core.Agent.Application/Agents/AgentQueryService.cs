namespace EU.Core.Agent.Application.Agents;

public sealed class AgentQueryService(IAgentRepository repository)
{
    public Task<AgentDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<AgentListItem>> ListAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AgentDefinition> definitions = await repository.ListAsync(query, cancellationToken);
        return AgentContractCloner.ReadOnly(definitions.Select(definition => new AgentListItem(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.Description,
            definition.RuntimeStatus,
            definition.LogicalRevision,
            definition.Draft.Label,
            definition.Draft.ModelProfileId,
            definition.PublishedVersions.LastOrDefault()?.Label)));
    }
}

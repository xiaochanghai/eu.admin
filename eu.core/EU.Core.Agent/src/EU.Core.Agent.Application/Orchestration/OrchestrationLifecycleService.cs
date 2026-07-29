using System.Text.RegularExpressions;
using EU.Core.Agent.Application.Agents;

namespace EU.Core.Agent.Application.Orchestration;

public sealed class OrchestrationLifecycleService(
    IOrchestrationRepository repository,
    IAgentRepository agents)
{
    public async Task<OrchestrationOperationResult<OrchestrationDefinition>> CreateAsync(
        CreateOrchestrationCommand command,
        CancellationToken cancellationToken = default)
    {
        string code = (command.Code ?? string.Empty).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return Failure(OrchestrationErrorCodes.CodeInvalid, "Code must be lowercase kebab-case.");
        }
        var draft = new OrchestrationVersion(
            Guid.NewGuid(), "0.1.0", true, string.Empty, [], [], null);
        var value = new OrchestrationDefinition(
            Guid.NewGuid(), code, command.Name?.Trim() ?? string.Empty,
            command.Description?.Trim() ?? string.Empty, OrchestrationStatus.Enabled,
            0, draft, []);
        return await repository.TryCreateAsync(value, cancellationToken)
            ? OrchestrationOperationResult<OrchestrationDefinition>.Success(value)
            : Failure(OrchestrationErrorCodes.CodeConflict, "An orchestration already uses this code.");
    }

    public async Task<OrchestrationOperationResult<OrchestrationDefinition>> SaveDraftAsync(
        SaveOrchestrationDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        OrchestrationDefinition? existing = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (existing is null) return Failure(OrchestrationErrorCodes.NotFound, "The orchestration was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (!Enum.IsDefined(command.Status))
            return Failure(OrchestrationErrorCodes.DefinitionInvalid, "Status is invalid.");
        string? error = await ValidateAsync(command.StartNodeId, command.Nodes, command.Edges, cancellationToken);
        if (error is not null) return Failure(OrchestrationErrorCodes.DefinitionInvalid, error);
        OrchestrationDefinition updated = existing with
        {
            Name = command.Name?.Trim() ?? string.Empty,
            Description = command.Description?.Trim() ?? string.Empty,
            Status = command.Status,
            LogicalRevision = existing.LogicalRevision + 1,
            Draft = existing.Draft with
            {
                StartNodeId = command.StartNodeId,
                Nodes = OrchestrationContractCloner.ReadOnly(command.Nodes),
                Edges = OrchestrationContractCloner.ReadOnly(command.Edges),
                Snapshot = null
            }
        };
        return await repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? OrchestrationOperationResult<OrchestrationDefinition>.Success(updated) : Conflict();
    }

    public async Task<OrchestrationOperationResult<OrchestrationDefinition>> PublishAsync(
        PublishOrchestrationCommand command,
        CancellationToken cancellationToken = default)
    {
        OrchestrationDefinition? existing = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (existing is null) return Failure(OrchestrationErrorCodes.NotFound, "The orchestration was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        string? error = await ValidateAsync(
            existing.Draft.StartNodeId, existing.Draft.Nodes, existing.Draft.Edges, cancellationToken);
        if (error is not null) return Failure(OrchestrationErrorCodes.DefinitionInvalid, error);

        var bindings = new List<OrchestrationAgentBinding>();
        foreach (Guid agentId in existing.Draft.Nodes.Select(node => node.AgentId).Distinct())
        {
            AgentDefinition? agent = await agents.GetByIdAsync(agentId, cancellationToken);
            AgentVersion? version = agent?.PublishedVersions.LastOrDefault();
            if (agent?.RuntimeStatus != AgentRuntimeStatus.Enabled || version?.Snapshot is null)
            {
                return Failure(
                    OrchestrationErrorCodes.AgentUnavailable,
                    $"Agent '{agentId}' must be enabled and published.");
            }
            bindings.Add(new OrchestrationAgentBinding(agentId, version.Id));
        }

        Guid versionId = Guid.NewGuid();
        var snapshot = new OrchestrationVersionSnapshot(
            versionId, existing.Code, existing.Draft.StartNodeId,
            OrchestrationContractCloner.ReadOnly(existing.Draft.Nodes),
            OrchestrationContractCloner.ReadOnly(existing.Draft.Edges),
            OrchestrationContractCloner.ReadOnly(bindings));
        var published = new OrchestrationVersion(
            versionId, $"{existing.PublishedVersions.Count + 1}.0.0", false,
            snapshot.StartNodeId, snapshot.Nodes, snapshot.Edges, snapshot);
        OrchestrationDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            PublishedVersions = OrchestrationContractCloner.ReadOnly(
                existing.PublishedVersions.Append(published))
        };
        return await repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? OrchestrationOperationResult<OrchestrationDefinition>.Success(updated) : Conflict();
    }

    public Task<OrchestrationDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<OrchestrationListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        OrchestrationContractCloner.ReadOnly((await repository.ListAsync(cancellationToken)).Select(value =>
            new OrchestrationListItem(
                value.Id, value.Code, value.Name, value.Description, value.Status,
                value.LogicalRevision, value.Draft.Nodes.Count,
                value.PublishedVersions.LastOrDefault()?.Label)));

    private async Task<string?> ValidateAsync(
        string startNodeId,
        IReadOnlyList<OrchestrationNode> nodes,
        IReadOnlyList<OrchestrationEdge> edges,
        CancellationToken cancellationToken)
    {
        if (nodes.Count is < 1 or > 50) return "A flow must contain from 1 through 50 nodes.";
        if (nodes.Select(node => node.Id).Distinct(StringComparer.Ordinal).Count() != nodes.Count ||
            nodes.Any(node => !Regex.IsMatch(node.Id ?? "", "^[a-z][a-z0-9-]{0,63}$")))
            return "Node IDs must be unique lowercase identifiers.";
        if (!nodes.Any(node => node.Id == startNodeId)) return "StartNodeId must reference a node.";
        if (nodes.Any(node => node.MaximumRetries is < 0 or > 3 ||
                              node.TimeoutSeconds is < 5 or > 600 ||
                              (node.InputTemplate?.Length ?? 0) > 8_192 ||
                              !Enum.IsDefined(node.InputMode)))
            return "Node retry, timeout, or input template limits are invalid.";
        foreach (Guid agentId in nodes.Select(node => node.AgentId).Distinct())
        {
            if (agentId == Guid.Empty || await agents.GetByIdAsync(agentId, cancellationToken) is null)
                return $"Agent '{agentId}' does not exist.";
        }
        HashSet<string> ids = nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        if (edges.Count > 200 || edges.Any(edge =>
                !ids.Contains(edge.FromNodeId) || !ids.Contains(edge.ToNodeId) ||
                edge.FromNodeId == edge.ToNodeId || edge.Order < 0 ||
                (edge.ConditionValue?.Length ?? 0) > 512 ||
                !Enum.IsDefined(edge.Condition) ||
                (edge.Condition == OrchestrationEdgeCondition.OutputContains &&
                 string.IsNullOrEmpty(edge.ConditionValue))))
            return "Edges must reference distinct existing nodes and stay within limits.";
        if (edges.GroupBy(edge => edge.FromNodeId, StringComparer.Ordinal)
            .Any(group => group.Select(edge => edge.Order).Distinct().Count() != group.Count()))
            return "Outgoing edge order values must be unique per node.";
        if (HasCycle(startNodeId, edges)) return "Cycles are not supported in P7.";
        HashSet<string> reachable = Reachable(startNodeId, edges);
        if (nodes.Any(node => !reachable.Contains(node.Id))) return "Every node must be reachable from StartNodeId.";
        return null;
    }

    private static HashSet<string> Reachable(string start, IReadOnlyList<OrchestrationEdge> edges)
    {
        var values = new HashSet<string>(StringComparer.Ordinal) { start };
        var pending = new Queue<string>();
        pending.Enqueue(start);
        while (pending.TryDequeue(out string? current))
            foreach (string next in edges.Where(edge => edge.FromNodeId == current).Select(edge => edge.ToNodeId))
                if (values.Add(next)) pending.Enqueue(next);
        return values;
    }

    private static bool HasCycle(string start, IReadOnlyList<OrchestrationEdge> edges)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        bool Visit(string id)
        {
            if (!visiting.Add(id)) return true;
            if (!visited.Add(id)) { visiting.Remove(id); return false; }
            foreach (string next in edges.Where(edge => edge.FromNodeId == id).Select(edge => edge.ToNodeId))
                if (Visit(next)) return true;
            visiting.Remove(id);
            return false;
        }
        return Visit(start);
    }

    private static OrchestrationOperationResult<OrchestrationDefinition> Failure(string code, string message) =>
        OrchestrationOperationResult<OrchestrationDefinition>.Failure(code, message);
    private static OrchestrationOperationResult<OrchestrationDefinition> Conflict() =>
        Failure(OrchestrationErrorCodes.RowVersionConflict, "The orchestration changed; reload and retry.");
}

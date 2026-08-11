using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryOrchestrationRepository : IOrchestrationRepository, IPublishedOrchestrationCatalog
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

    public Task<IReadOnlyList<PublishedOrchestrationReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PublishedOrchestrationReference>>(
                OrchestrationContractCloner.ReadOnly(_values.Values
                    .Where(value => value.Status is not OrchestrationStatus.Archived &&
                        value.PublishedVersions.Count > 0)
                    .SelectMany(value => value.PublishedVersions.Select(version =>
                        new PublishedOrchestrationReference(
                            value.Id,
                            version.Id,
                            value.Status is OrchestrationStatus.Enabled)))));
        }
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
        lock (_gate)
        {
            if (!_values.TryGetValue(value.Id, out OrchestrationRunRecord? existing)
                || existing.Status == OrchestrationRunStatus.Running)
            {
                _values[value.Id] = OrchestrationContractCloner.Clone(value);
            }
        }
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

    public Task<bool> TrySaveRunningDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_values.TryGetValue(value.RunId, out OrchestrationRunRecord? run)
                || run.Status != OrchestrationRunStatus.Running)
            {
                return Task.FromResult(false);
            }

            _details[value.RunId] = OrchestrationContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }

    public Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken = default)
    {
        if (runStatus == OrchestrationRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runStatus),
                "A terminal run status is required.");
        }
        if (nodeStatus is OrchestrationNodeRunStatus.Pending
            or OrchestrationNodeRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeStatus),
                "A terminal node status is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(TransitionRunning(
                runId,
                runStatus,
                nodeStatus,
                transitionPolicy,
                finishedAtUtc,
                errorCode,
                detailsIfMissing));
        }
    }

    public Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        string errorCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(TransitionRunning(
                runId,
                OrchestrationRunStatus.Failed,
                OrchestrationNodeRunStatus.Failed,
                OrchestrationTerminalTransitionPolicy.TerminalizePending,
                recoveredAtUtc,
                errorCode,
                detailsIfMissing: null));
        }
    }

    private OrchestrationRunTransitionResult TransitionRunning(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing)
    {
        if (!_values.TryGetValue(runId, out OrchestrationRunRecord? value))
        {
            return new OrchestrationRunTransitionResult(null, false);
        }

        if (value.Status != OrchestrationRunStatus.Running)
        {
            return new OrchestrationRunTransitionResult(
                OrchestrationContractCloner.Clone(value),
                false);
        }
        if (detailsIfMissing is not null && detailsIfMissing.RunId != runId)
        {
            throw new InvalidOperationException(
                "Fallback orchestration details do not belong to the transitioned run.");
        }

        OrchestrationRunRecord terminal = TerminalizeRun(
            value,
            runStatus,
            nodeStatus,
            transitionPolicy,
            finishedAtUtc,
            errorCode);
        _values[runId] = OrchestrationContractCloner.Clone(terminal);
        if (!_details.ContainsKey(runId) && detailsIfMissing is not null)
        {
            _details[runId] = OrchestrationContractCloner.Clone(detailsIfMissing);
        }
        if (_details.TryGetValue(runId, out OrchestrationRunDetails? details))
        {
            _details[runId] = OrchestrationContractCloner.Clone(
                TerminalizeDetails(
                    details,
                    nodeStatus,
                    transitionPolicy,
                    finishedAtUtc,
                    errorCode));
        }

        return new OrchestrationRunTransitionResult(
            OrchestrationContractCloner.Clone(terminal),
            true);
    }

    private static OrchestrationRunRecord TerminalizeRun(
        OrchestrationRunRecord value,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode) =>
        value with
        {
            Status = runStatus,
            FinishedAtUtc = finishedAtUtc,
            ErrorCode = errorCode,
            Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                ShouldTerminalize(node.Status, transitionPolicy)
                    ? node with
                    {
                        Status = nodeStatus,
                        FinishedAtUtc = finishedAtUtc,
                        ErrorCode = errorCode
                    }
                    : node))
        };

    private static OrchestrationRunDetails TerminalizeDetails(
        OrchestrationRunDetails details,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode) =>
        details with
        {
            Attempts = OrchestrationContractCloner.ReadOnly(
                details.Attempts.Select(attempt =>
                    TerminalizeAttempt(
                        attempt,
                        nodeStatus,
                        transitionPolicy,
                        finishedAtUtc,
                        errorCode)))
        };

    private static OrchestrationNodeAttemptRecord TerminalizeAttempt(
        OrchestrationNodeAttemptRecord attempt,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode)
    {
        IReadOnlyList<OrchestrationToolCallRecord> tools =
            OrchestrationContractCloner.ReadOnly(
                attempt.ToolCalls.Select(tool =>
                    tool.Status == AgentRunEventKind.ToolStarted
                        ? tool with
                        {
                            Status = AgentRunEventKind.ToolFailed,
                            FinishedAtUtc = finishedAtUtc,
                            ErrorCode = errorCode
                        }
                        : tool));
        return ShouldTerminalize(attempt.Status, transitionPolicy)
            ? attempt with
            {
                Status = nodeStatus,
                FinishedAtUtc = finishedAtUtc,
                ErrorCode = errorCode,
                ToolCalls = tools
            }
            : attempt with { ToolCalls = tools };
    }

    private static bool ShouldTerminalize(
        OrchestrationNodeRunStatus status,
        OrchestrationTerminalTransitionPolicy transitionPolicy) =>
        status == OrchestrationNodeRunStatus.Running
        || (status == OrchestrationNodeRunStatus.Pending
            && transitionPolicy
                == OrchestrationTerminalTransitionPolicy.TerminalizePending);
}

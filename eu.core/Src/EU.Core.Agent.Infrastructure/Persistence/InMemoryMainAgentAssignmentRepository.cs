using EU.Core.Agent.Application.MainAgent;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryMainAgentAssignmentRepository : IMainAgentAssignmentRepository
{
    private readonly object _gate = new();
    private MainAgentAssignment? _assignment;

    public Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_assignment);
        }
    }

    public Task<bool> TryReplaceAsync(
        MainAgentAssignment value,
        long? expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if ((expectedLogicalRevision is null && _assignment is not null) ||
                (expectedLogicalRevision is not null &&
                 (_assignment is null || _assignment.LogicalRevision != expectedLogicalRevision)) ||
                value.LogicalRevision != (expectedLogicalRevision is null ? 0 : expectedLogicalRevision + 1))
            {
                return Task.FromResult(false);
            }

            _assignment = value;
            return Task.FromResult(true);
        }
    }
}

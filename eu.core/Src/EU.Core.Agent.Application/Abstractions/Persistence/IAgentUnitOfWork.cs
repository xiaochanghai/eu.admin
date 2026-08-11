namespace EU.Core.Agent.Application.Abstractions.Persistence;

public interface IAgentUnitOfWork : IAsyncDisposable
{
    bool IsTransactionActive { get; }

    Task BeginAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public sealed record AgentConcurrencyConflict(string EntityName, Guid EntityId);

public sealed class AgentConcurrencyConflictException : Exception
{
    public AgentConcurrencyConflictException(string entityName, Guid entityId)
        : base($"A concurrency conflict occurred while updating '{entityName}'.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        Conflict = new AgentConcurrencyConflict(entityName, entityId);
    }

    public AgentConcurrencyConflict Conflict { get; }

    public string EntityName => Conflict.EntityName;

    public Guid EntityId => Conflict.EntityId;
}

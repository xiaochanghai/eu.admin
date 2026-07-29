using EU.Core.Agent.Application.Abstractions.Persistence;
using SqlSugar;

namespace EU.Core.Agent.Infrastructure.Persistence;

public interface ISqlSugarTransactionOperations : IDisposable
{
    void Begin();

    void Commit();

    void Rollback();
}

public sealed class SqlSugarAgentUnitOfWork : IAgentUnitOfWork
{
    private readonly ISqlSugarTransactionOperations _operations;
    private bool _disposed;

    public SqlSugarAgentUnitOfWork(SqlSugarClient database)
        : this(new SqlSugarTransactionOperations(database))
    {
    }

    public SqlSugarAgentUnitOfWork(ISqlSugarTransactionOperations operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        _operations = operations;
    }

    public bool IsTransactionActive { get; private set; }

    public static void ConfigureAgentEntity<TEntity>(SqlSugarClient database, string tableName)
        where TEntity : EntityBase
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        if (!tableName.All(character => char.IsLetterOrDigit(character) || character == '_'))
        {
            throw new ArgumentException(
                "Table names may contain only letters, digits, and underscores.",
                nameof(tableName));
        }

        database.MappingTables.Add(typeof(TEntity).Name, $"agent.{tableName}");
    }

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        if (IsTransactionActive)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _operations.Begin();
        IsTransactionActive = true;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        EnsureTransactionIsActive();

        _operations.Commit();
        IsTransactionActive = false;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!IsTransactionActive)
        {
            return Task.CompletedTask;
        }

        _operations.Rollback();
        IsTransactionActive = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            if (IsTransactionActive)
            {
                _operations.Rollback();
                IsTransactionActive = false;
            }
        }
        finally
        {
            _operations.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private void EnsureTransactionIsActive()
    {
        if (!IsTransactionActive)
        {
            throw new InvalidOperationException("No transaction is active.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class SqlSugarTransactionOperations(SqlSugarClient database) : ISqlSugarTransactionOperations
    {
        private readonly SqlSugarClient _database = database ?? throw new ArgumentNullException(nameof(database));

        public void Begin() => _database.Ado.BeginTran();

        public void Commit() => _database.Ado.CommitTran();

        public void Rollback() => _database.Ado.RollbackTran();

        public void Dispose() => _database.Dispose();
    }
}

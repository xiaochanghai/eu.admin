using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed class BusinessQueryExecutionContext
{
    public BusinessQueryExecutionContext(
        string userId,
        string tenantId,
        IEnumerable<string> permissions,
        string correlationId,
        Guid agentRunId,
        Guid toolVersionId,
        string jti)
    {
        UserId = userId;
        TenantId = tenantId;
        Permissions = new ReadOnlyCollection<string>(permissions.ToArray());
        CorrelationId = correlationId;
        AgentRunId = agentRunId;
        ToolVersionId = toolVersionId;
        Jti = jti;
    }

    public string UserId { get; }
    public string TenantId { get; }
    public IReadOnlyList<string> Permissions { get; }
    public string CorrelationId { get; }
    public Guid AgentRunId { get; }
    public Guid ToolVersionId { get; }
    public string Jti { get; }
}

public sealed class BusinessQueryExecutionContextAccessor
{
    private readonly AsyncLocal<BusinessQueryExecutionContext?> _current = new();

    public BusinessQueryExecutionContext? Current => _current.Value;

    public IDisposable Enter(BusinessQueryExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_current.Value is not null)
        {
            throw new InvalidOperationException("A Business Query execution context is already active.");
        }

        _current.Value = context;
        return new Scope(this);
    }

    private sealed class Scope(BusinessQueryExecutionContextAccessor owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner._current.Value = null;
            }
        }
    }
}

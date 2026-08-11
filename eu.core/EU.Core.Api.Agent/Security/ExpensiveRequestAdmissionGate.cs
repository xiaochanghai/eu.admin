using Microsoft.Extensions.Options;
using EU.Core.Api.Agent.Configuration;

namespace EU.Core.Api.Agent.Security;

public sealed class ExpensiveRequestAdmissionGate
{
    private readonly SemaphoreSlim _permits;

    public ExpensiveRequestAdmissionGate(IOptions<AgentCapacityOptions> options)
    {
        _permits = new SemaphoreSlim(
            options.Value.MaximumConcurrentExpensiveRequests,
            options.Value.MaximumConcurrentExpensiveRequests);
    }

    public IDisposable? TryAcquire() =>
        _permits.Wait(0) ? new Lease(_permits) : null;

    private sealed class Lease(SemaphoreSlim permits) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                permits.Release();
        }
    }
}

using EU.Core.Agent.Api.Observability;

namespace EU.Core.Agent.Api.Health;

public sealed class HostDrainState(AgentMetrics metrics)
{
    private int _draining;

    public bool IsDraining => Volatile.Read(ref _draining) != 0;

    public void BeginDrain()
    {
        if (Interlocked.Exchange(ref _draining, 1) == 0)
            metrics.RecordResilience(AgentResilienceEvent.HostDrainStarted);
    }
}

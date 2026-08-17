using System.Globalization;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Health;
using Microsoft.Extensions.Options;
using EU.Core.Api.Agent.Observability;

namespace EU.Core.Api.Agent.Security;

public sealed class ExpensiveRequestAdmissionMiddleware(
    RequestDelegate next,
    ExpensiveRequestAdmissionGate gate,
    IOptions<AgentCapacityOptions> capacityOptions,
    AgentMetrics metrics,
    HostDrainState drainState)
{
    public const string RejectedItemKey = "AgentExpensiveAdmissionRejected";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!AgentWorkloadClassifier.IsExpensive(context.Request))
        {
            await next(context);
            return;
        }

        AgentCapacityOptions capacity = capacityOptions.Value;
        if (drainState.IsDraining)
        {
            metrics.RecordResilience(AgentResilienceEvent.HostDrainRejected);
            await RejectAsync(
                context,
                capacity.RetryAfterSeconds,
                "AGENT_INSTANCE_DRAINING",
                "This instance is stopping and cannot start new execution work. Retry on a ready instance.");
            return;
        }

        if (!capacity.Enabled)
        {
            await next(context);
            return;
        }

        using IDisposable? lease = gate.TryAcquire();
        if (lease is not null)
        {
            metrics.RecordExpensiveStarted();
            try
            {
                await next(context);
            }
            finally
            {
                metrics.RecordExpensiveCompleted();
            }
            return;
        }

        metrics.RecordResilience(AgentResilienceEvent.CapacityRejected);
        await RejectAsync(
            context,
            capacity.RetryAfterSeconds,
            "AGENT_CAPACITY_EXHAUSTED",
            "All execution slots are in use. Retry after the indicated interval.");
    }

    private static async Task RejectAsync(
        HttpContext context,
        int retryAfterSeconds,
        string code,
        string message)
    {
        context.Items[RejectedItemKey] = true;
        context.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        await AgentApiErrorResponseWriter.WriteAsync(
            context,
            code,
            message,
            cancellationToken: context.RequestAborted);
    }
}

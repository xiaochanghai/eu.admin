using System.Globalization;
using EU.Core.Agent.Api.Configuration;
using EU.Core.Agent.Api.Health;
using Microsoft.Extensions.Options;
using EU.Core.Agent.Api.Observability;

namespace EU.Core.Agent.Api.Security;

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
                "The Agent instance is draining.",
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
            "Execution capacity is temporarily unavailable.",
            "AGENT_CAPACITY_EXHAUSTED",
            "All execution slots are in use. Retry after the indicated interval.");
    }

    private static async Task RejectAsync(
        HttpContext context,
        int retryAfterSeconds,
        string title,
        string code,
        string detail)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Items[RejectedItemKey] = true;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/503",
            title,
            status = StatusCodes.Status503ServiceUnavailable,
            code,
            detail,
            correlationId = context.TraceIdentifier
        }, context.RequestAborted);
    }
}

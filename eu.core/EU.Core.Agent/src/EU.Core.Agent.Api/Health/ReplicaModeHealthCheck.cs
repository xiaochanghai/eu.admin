using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EU.Core.Agent.Api.Health;

public sealed class ReplicaModeHealthCheck : IHealthCheck
{
    public const string ReplicaMode = "single";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["replicaMode"] = ReplicaMode,
        };

        return Task.FromResult(HealthCheckResult.Healthy("The service is running as a single replica.", data));
    }

    public static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            status = report.Status.ToString(),
            replicaMode = ReplicaMode,
        });
    }
}

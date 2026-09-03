using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Observability;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Auditing;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Globalization;

namespace EU.Core.Api.Agent.Security;

public sealed class AgentOperationAuditMiddleware(
    RequestDelegate next,
    IUser user,
    TimeProvider timeProvider,
    AgentMetrics metrics,
    ILogger<AgentOperationAuditMiddleware> logger)
{
    internal const string CancelledItemKey =
        "EU.Core.Api.Agent.OperationAudit.Cancelled";

    public async Task InvokeAsync(
        HttpContext context,
        IAgAgentOperationAuditServices repository)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        DateTimeOffset occurredAt = timeProvider.GetUtcNow();
        long started = Stopwatch.GetTimestamp();
        Guid id = Guid.NewGuid();
        string userId = user.ID?.ToString("D") ?? "anonymous";
        string tenantId = user.TenantId.ToString(CultureInfo.InvariantCulture);
        string policy = SelectPolicy(context);
        string method = SelectMethod(context.Request.Method);
        string route = SelectRoute(context);
        var record = new AgentOperationAuditRecord(
            id,
            occurredAt,
            tenantId,
            userId,
            context.TraceIdentifier,
            policy,
            method,
            route,
            0,
            "Started",
            null,
            0);

        metrics.RecordStarted(method, route, policy);
        try
        {
            await repository.SaveAsync(record, context.RequestAborted);
        }
        catch (OperationCanceledException) when (
            context.RequestAborted.IsCancellationRequested)
        {
            metrics.RecordCompleted(
                method,
                route,
                policy,
                499,
                "Cancelled",
                ElapsedMilliseconds(started));
            throw;
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException ||
            !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(
                exception,
                "Agent operation audit start persistence failed. CorrelationId: {CorrelationId}",
                context.TraceIdentifier);
            metrics.RecordCompleted(
                method,
                route,
                policy,
                StatusCodes.Status503ServiceUnavailable,
                "Failed",
                ElapsedMilliseconds(started));
            await AgentApiErrorResponseWriter.WriteAsync(
                context,
                "AGENT_AUDIT_UNAVAILABLE",
                "The audit service is unavailable.",
                cancellationToken: context.RequestAborted);
            return;
        }

        try
        {
            await next(context);
            AgentOperationAuditRecord terminal = await SaveTerminalAsync(
                repository,
                record,
                context.Response.StatusCode,
                ElapsedMilliseconds(started),
                context.Items.ContainsKey(CancelledItemKey),
                CancellationToken.None);
            metrics.RecordCompleted(
                method,
                route,
                policy,
                terminal.StatusCode,
                terminal.Outcome,
                terminal.DurationMilliseconds);
        }
        catch (Exception)
        {
            AgentOperationAuditRecord terminal = await SaveTerminalAsync(
                repository,
                record,
                StatusCodes.Status500InternalServerError,
                ElapsedMilliseconds(started),
                cancelled: false,
                CancellationToken.None);
            metrics.RecordCompleted(
                method,
                route,
                policy,
                terminal.StatusCode,
                terminal.Outcome,
                terminal.DurationMilliseconds);
            throw;
        }
    }

    private async Task<AgentOperationAuditRecord> SaveTerminalAsync(
        IAgAgentOperationAuditServices repository,
        AgentOperationAuditRecord record,
        int statusCode,
        long durationMilliseconds,
        bool cancelled,
        CancellationToken cancellationToken)
    {
        string outcome = (statusCode, cancelled) switch
        {
            (_, true) => "Cancelled",
            (>= 200 and < 400, false) => "Succeeded",
            (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden, false) =>
                "Rejected",
            _ => "Failed"
        };
        string? errorCode = (statusCode, cancelled) switch
        {
            (_, true) => "OPERATION_CANCELLED",
            (StatusCodes.Status401Unauthorized, false) => "AUTHENTICATION_REQUIRED",
            (StatusCodes.Status403Forbidden, false) => "AUTHORIZATION_DENIED",
            (>= 400, false) => $"HTTP_{statusCode}",
            _ => null
        };
        AgentOperationAuditRecord terminal = record with
        {
            StatusCode = statusCode,
            Outcome = outcome,
            ErrorCode = errorCode,
            DurationMilliseconds = durationMilliseconds
        };
        try
        {
            await repository.SaveAsync(terminal, cancellationToken);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException ||
            !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                exception,
                "Agent operation audit completion persistence failed. CorrelationId: {CorrelationId}",
                record.CorrelationId);
        }

        return terminal;
    }

    private static string SelectPolicy(HttpContext context)
    {
        string[] policies = context.GetEndpoint()?.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(metadata => metadata.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(policy => policy, StringComparer.Ordinal)
            .Cast<string>()
            .ToArray() ?? [];
        return policies.Length == 0 ? "Authenticated" : string.Join(',', policies);
    }

    private static string SelectRoute(HttpContext context)
    {
        string? pattern = (context.GetEndpoint() as RouteEndpoint)?
            .RoutePattern.RawText;
        return string.IsNullOrWhiteSpace(pattern)
            ? "/api/{unmatched}"
            : $"/{pattern.TrimStart('/')}";
    }

    private static string SelectMethod(string method) => method switch
    {
        "GET" => "GET",
        "POST" => "POST",
        "PUT" => "PUT",
        "PATCH" => "PATCH",
        "DELETE" => "DELETE",
        "HEAD" => "HEAD",
        "OPTIONS" => "OPTIONS",
        _ => "OTHER"
    };

    private static long ElapsedMilliseconds(long started) =>
        (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}

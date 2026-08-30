using System.Security.Cryptography;
using System.Text;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Observability;
using EU.Core.IServices.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Security;

public sealed class HttpIdempotencyMiddleware(
    RequestDelegate next,
    IOptions<AgentIdempotencyOptions> idempotencyOptions,
    TimeProvider timeProvider,
    AgentMetrics metrics)
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    public async Task InvokeAsync(
        HttpContext context,
        IHttpIdempotencyRepository repository)
    {
        AgentIdempotencyOptions options = idempotencyOptions.Value;
        if (!options.Enabled || !context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            await next(context);
            return;
        }

        string key = values.Count == 1 ? values[0] ?? string.Empty : string.Empty;
        if (!IsValidKey(key))
        {
            metrics.RecordResilience(AgentResilienceEvent.IdempotencyRejected);
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                "IDEMPOTENCY_KEY_INVALID",
                "Idempotency-Key must contain 8 through 128 URL-safe characters.");
            return;
        }

        if (!AgentWorkloadClassifier.SupportsIdempotency(context.Request))
        {
            metrics.RecordResilience(AgentResilienceEvent.IdempotencyRejected);
            string code = context.Request.Path.Equals(
                "/api/chat/runs",
                StringComparison.OrdinalIgnoreCase)
                ? "IDEMPOTENCY_STREAMING_UNSUPPORTED"
                : "IDEMPOTENCY_ENDPOINT_UNSUPPORTED";
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                code,
                "This endpoint does not support Idempotency-Key.");
            return;
        }

        string requestSha256 = await HashRequestAsync(context.Request, context.RequestAborted);
        string userId = context.User.FindFirst(AgentIdentityClaims.UserId)?.Value?.Trim()
            ?? throw new InvalidOperationException("An authorized caller identity is required.");
        string tenantId = AgentIdentityClaims.GetTenantId(context.User)
            ?? throw new InvalidOperationException("An authorized caller tenant is required.");
        string scopeSha256 = Hash(
            $"{tenantId}\0{userId}\0{context.Request.Method.ToUpperInvariant()}\0" +
            $"{context.Request.Path.Value?.ToLowerInvariant()}\0{key}");
        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        var pending = new HttpIdempotencyRecord(
            scopeSha256,
            requestSha256,
            HttpIdempotencyStatus.InProgress,
            0,
            string.Empty,
            string.Empty,
            [],
            now,
            now.AddHours(options.RetentionHours));
        HttpIdempotencyBeginResult begin = await repository.BeginAsync(
            pending,
            now,
            context.RequestAborted);
        if (!begin.Acquired)
        {
            await ReplayOrRejectAsync(context, requestSha256, begin.Record);
            return;
        }
        metrics.RecordResilience(AgentResilienceEvent.IdempotencyReserved);

        Stream originalBody = context.Response.Body;
        await using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;
        bool settled = false;
        try
        {
            await next(context);
            byte[] body = capturedBody.ToArray();
            if (context.Items.ContainsKey(
                ExpensiveRequestAdmissionMiddleware.RejectedItemKey))
            {
                await repository.AbandonAsync(
                    scopeSha256,
                    requestSha256,
                    CancellationToken.None);
                metrics.RecordResilience(AgentResilienceEvent.IdempotencyAbandoned);
                settled = true;
            }
            else if (body.Length <= options.MaximumCachedResponseBytes)
            {
                bool completed = await repository.CompleteAsync(
                    scopeSha256,
                    requestSha256,
                    context.Response.StatusCode,
                    context.Response.ContentType ?? string.Empty,
                    context.Response.Headers.Location.ToString(),
                    body,
                    CancellationToken.None);
                if (!completed)
                    throw new InvalidOperationException("The idempotency reservation could not be completed.");
                metrics.RecordResilience(AgentResilienceEvent.IdempotencyCompleted);
                settled = true;
            }
            else
            {
                await repository.MarkIndeterminateAsync(
                    scopeSha256,
                    requestSha256,
                    CancellationToken.None);
                metrics.RecordResilience(AgentResilienceEvent.IdempotencyIndeterminate);
                settled = true;
            }

            capturedBody.Position = 0;
            await capturedBody.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            if (!settled)
            {
                try
                {
                    await repository.MarkIndeterminateAsync(
                        scopeSha256,
                        requestSha256,
                        CancellationToken.None);
                    metrics.RecordResilience(AgentResilienceEvent.IdempotencyIndeterminate);
                }
                catch
                {
                    // Preserve the authoritative downstream exception.
                }
            }

            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private async Task ReplayOrRejectAsync(
        HttpContext context,
        string requestSha256,
        HttpIdempotencyRecord record)
    {
        if (!string.Equals(record.RequestSha256, requestSha256, StringComparison.Ordinal))
        {
            metrics.RecordResilience(AgentResilienceEvent.IdempotencyKeyReused);
            await WriteErrorAsync(
                context,
                StatusCodes.Status409Conflict,
                "IDEMPOTENCY_KEY_REUSED",
                "The Idempotency-Key was already used with a different request.");
            return;
        }

        if (record.Status == HttpIdempotencyStatus.Completed)
        {
            metrics.RecordResilience(AgentResilienceEvent.IdempotencyReplayed);
            context.Response.StatusCode = record.ResponseStatusCode;
            if (record.ResponseContentType.Length > 0)
                context.Response.ContentType = record.ResponseContentType;
            if (record.ResponseLocation.Length > 0)
                context.Response.Headers.Location = record.ResponseLocation;
            context.Response.Headers[ReplayedHeaderName] = "true";
            context.Response.ContentLength = record.ResponseBody.Length;
            await context.Response.Body.WriteAsync(record.ResponseBody, context.RequestAborted);
            return;
        }

        context.Response.Headers.RetryAfter = "1";
        metrics.RecordResilience(record.Status == HttpIdempotencyStatus.InProgress
            ? AgentResilienceEvent.IdempotencyInProgress
            : AgentResilienceEvent.IdempotencyOutcomeUnknown);
        await WriteErrorAsync(
            context,
            StatusCodes.Status409Conflict,
            record.Status == HttpIdempotencyStatus.InProgress
                ? "IDEMPOTENCY_IN_PROGRESS"
                : "IDEMPOTENCY_OUTCOME_UNKNOWN",
            record.Status == HttpIdempotencyStatus.InProgress
                ? "A request with this Idempotency-Key is still running."
                : "The earlier request outcome is unavailable and must be reconciled before retrying.");
    }

    private static async Task<string> HashRequestAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var body = new MemoryStream();
        await request.Body.CopyToAsync(body, cancellationToken);
        request.Body.Position = 0;
        return Convert.ToHexStringLower(SHA256.HashData(body.GetBuffer().AsSpan(0, (int)body.Length)));
    }

    private static bool IsValidKey(string value)
    {
        if (value.Length is < 8 or > 128) return false;
        return value.All(character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or '.' or ':');
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static async Task WriteErrorAsync(
        HttpContext context,
        int status,
        string code,
        string detail)
    {
        await AgentApiErrorResponseWriter.WriteAsync(
            context,
            code,
            detail,
            httpStatus: status,
            cancellationToken: context.RequestAborted);
    }
}

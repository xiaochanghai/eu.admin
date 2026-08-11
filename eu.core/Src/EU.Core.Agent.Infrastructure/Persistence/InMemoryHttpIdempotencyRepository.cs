using EU.Core.Agent.Application.Abstractions.Security;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryHttpIdempotencyRepository : IHttpIdempotencyRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, HttpIdempotencyRecord> _records = [];

    public Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            foreach (string expired in _records
                .Where(value => value.Value.ExpiresAtUtc <= nowUtc)
                .Select(value => value.Key)
                .ToArray())
            {
                _records.Remove(expired);
            }

            if (_records.TryGetValue(pending.ScopeSha256, out HttpIdempotencyRecord? existing)
                && existing.ExpiresAtUtc > nowUtc)
            {
                return Task.FromResult(new HttpIdempotencyBeginResult(
                    false,
                    Clone(existing)));
            }

            _records[pending.ScopeSha256] = Clone(pending);
            return Task.FromResult(new HttpIdempotencyBeginResult(true, Clone(pending)));
        }
    }

    public Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_records.TryGetValue(scopeSha256, out HttpIdempotencyRecord? existing)
                || existing.Status != HttpIdempotencyStatus.InProgress
                || !string.Equals(existing.RequestSha256, requestSha256, StringComparison.Ordinal))
                return Task.FromResult(false);

            _records[scopeSha256] = existing with
            {
                Status = HttpIdempotencyStatus.Completed,
                ResponseStatusCode = responseStatusCode,
                ResponseContentType = responseContentType,
                ResponseLocation = responseLocation,
                ResponseBody = responseBody.ToArray()
            };
            return Task.FromResult(true);
        }
    }

    public Task MarkIndeterminateAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_records.TryGetValue(scopeSha256, out HttpIdempotencyRecord? existing)
                && existing.Status == HttpIdempotencyStatus.InProgress
                && string.Equals(existing.RequestSha256, requestSha256, StringComparison.Ordinal))
            {
                _records[scopeSha256] = existing with
                {
                    Status = HttpIdempotencyStatus.Indeterminate
                };
            }
        }

        return Task.CompletedTask;
    }

    public Task AbandonAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_records.TryGetValue(scopeSha256, out HttpIdempotencyRecord? existing)
                && existing.Status == HttpIdempotencyStatus.InProgress
                && string.Equals(existing.RequestSha256, requestSha256, StringComparison.Ordinal))
            {
                _records.Remove(scopeSha256);
            }
        }

        return Task.CompletedTask;
    }

    private static HttpIdempotencyRecord Clone(HttpIdempotencyRecord value) =>
        value with { ResponseBody = value.ResponseBody.ToArray() };
}

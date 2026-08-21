#nullable enable

namespace EU.Core.IServices.Abstractions.Security;

public enum HttpIdempotencyStatus
{
    InProgress,
    Completed,
    Indeterminate
}

public sealed record HttpIdempotencyRecord(
    string ScopeSha256,
    string RequestSha256,
    HttpIdempotencyStatus Status,
    int ResponseStatusCode,
    string ResponseContentType,
    string ResponseLocation,
    byte[] ResponseBody,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record HttpIdempotencyBeginResult(
    bool Acquired,
    HttpIdempotencyRecord Record);

public interface IHttpIdempotencyRepository
{
    Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default);

    Task MarkIndeterminateAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default);
}

#nullable enable

namespace EU.Core.IServices.Abstractions.Security;

/// <summary>
/// HTTP 幂等请求的处理状态。
/// </summary>
public enum HttpIdempotencyStatus
{
    /// <summary>请求正在处理中。</summary>
    InProgress,
    /// <summary>请求已完成并可复用响应。</summary>
    Completed,
    /// <summary>请求结果无法确定，需要人工或补偿流程确认。</summary>
    Indeterminate
}

/// <summary>
/// HTTP 幂等请求及其响应快照。
/// </summary>
/// <param name="ScopeSha256">幂等作用域的 SHA-256 摘要。</param>
/// <param name="RequestSha256">请求内容的 SHA-256 摘要。</param>
/// <param name="Status">当前状态。</param>
/// <param name="ResponseStatusCode">缓存的 HTTP 响应状态码。</param>
/// <param name="ResponseContentType">缓存响应的内容类型。</param>
/// <param name="ResponseLocation">缓存响应的 Location 标头值。</param>
/// <param name="ResponseBody">缓存的响应正文。</param>
/// <param name="CreatedAtUtc">记录创建的 UTC 时间。</param>
/// <param name="ExpiresAtUtc">记录或审批过期的 UTC 时间。</param>
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

/// <summary>
/// 开始处理幂等请求的结果。
/// </summary>
/// <param name="Acquired">当前调用是否取得请求处理权。</param>
/// <param name="Record">幂等请求记录。</param>
public sealed record HttpIdempotencyBeginResult(
    bool Acquired,
    HttpIdempotencyRecord Record);

/// <summary>
/// 定义 HTTP 幂等请求记录的存储边界。
/// </summary>
public interface IHttpIdempotencyRepository
{
    /// <summary>开始处理幂等请求或复用已有响应。</summary>
    Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>完成HTTP 幂等记录。</summary>
    Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default);

    /// <summary>将幂等请求标记为结果不确定。</summary>
    Task MarkIndeterminateAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default);

    /// <summary>放弃尚未完成的幂等请求占用。</summary>
    Task AbandonAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default);
}

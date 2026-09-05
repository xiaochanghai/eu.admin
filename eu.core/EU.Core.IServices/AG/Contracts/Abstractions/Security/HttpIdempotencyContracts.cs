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
    #region 开始处理幂等请求或复用已有响应。
    /// <summary>开始处理幂等请求或复用已有响应。</summary>
    /// <param name="pending">本次请求拟占用作用域的待执行幂等记录，包含请求摘要及有效期。</param>
    /// <param name="nowUtc">当前时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>幂等作用域是否成功占用及对应记录；重复请求携带已有记录。</returns>
    Task<HttpIdempotencyBeginResult> BeginAsync(HttpIdempotencyRecord pending, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
    #endregion

    #region 保存 HTTP 响应并完成幂等记录（CompleteAsync）
    /// <summary>
    /// 保存 HTTP 响应并完成幂等记录（CompleteAsync）。
    /// </summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <param name="requestSha256">请求内容的 SHA-256 摘要。</param>
    /// <param name="responseStatusCode">HTTP 响应状态码。</param>
    /// <param name="responseContentType">HTTP 响应的内容类型。</param>
    /// <param name="responseLocation">HTTP 响应的 Location 地址。</param>
    /// <param name="responseBody">HTTP 响应正文。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步更新结果：成功将匹配范围和请求摘要的进行中记录更新为已完成时返回 true，否则返回 false。</returns>
    Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default);
    #endregion

    #region 将幂等请求标记为结果不确定。
    /// <summary>将幂等请求标记为结果不确定。</summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <param name="requestSha256">请求内容的 SHA-256 摘要。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task MarkIndeterminateAsync(string scopeSha256, string requestSha256, CancellationToken cancellationToken = default);
    #endregion

    #region 放弃尚未完成的幂等请求占用。
    /// <summary>放弃尚未完成的幂等请求占用。</summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <param name="requestSha256">请求内容的 SHA-256 摘要。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task AbandonAsync(string scopeSha256, string requestSha256, CancellationToken cancellationToken = default);
    #endregion
}
